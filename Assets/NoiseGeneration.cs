using UnityEngine;
using UnityEditor;
using System.Collections;
using System;

public class NoiseGeneration : MonoBehaviour 
{
    public int Number_Of_Octaves;
    public float persistence;
    public int terrainSize, terrainHeight, textureBlendOffset, seedValue;
    public float smooth_factor, textureHeight;
    public Terrain terrain;
    public Texture terrainTexture;

    private int textureRes;
    public float contrastScale;
    private Texture2D texture;

    public bool RECALCULATE = false;
    public enum NoiseType { FBM,DSN,BOTH};
    public NoiseType noiseType = NoiseType.BOTH;
    SplatPrototype[] splatProto = new SplatPrototype[2];

    void Awake()
    {
        textureRes = terrainSize - 1;
        texture = new Texture2D(textureRes, textureRes, TextureFormat.RGB24, true);
        texture.name = "TextureTest";
        texture.wrapMode = TextureWrapMode.Clamp;
        GetComponent<Renderer>().material.mainTexture = texture;
        
        splatProto[0] = new SplatPrototype();
        splatProto[0].texture = (Texture2D)Resources.Load("GoodDirt", typeof(Texture2D));
        splatProto[0].tileOffset = new Vector2(0, 0);
        splatProto[0].tileSize = new Vector2(textureRes, textureRes);

        splatProto[1] = new SplatPrototype();
        splatProto[1].texture = (Texture2D)Resources.Load("snow", typeof(Texture2D));
        splatProto[1].tileOffset = new Vector2(0, 0);
        splatProto[1].tileSize = new Vector2(textureRes, textureRes);        
    }

    void Update()
    {
        if(RECALCULATE == true)
        {
            textureRes = terrainSize - 1;
            RedrawTerrain();
        }
    }

    void RedrawTerrain()
    {
        float[,] fbm, dsn;
        if (noiseType == NoiseType.FBM)
        {
            fbm = genNoise(terrainSize);
            InitializeTerrain();
            float[,] test = new float[terrainSize, terrainSize];
            test = GenerateFBMNoise(fbm, Number_Of_Octaves);
            terrain.terrainData.SetHeights(0, 0, test);
            FillTexture(test, 0);
        }

        else if (noiseType == NoiseType.DSN)
        {
            dsn = GenerateDSNNoise();
            float[,] dsnInterp = new float[terrainSize, terrainSize];
            dsnInterp = genSmoothNoise(dsn, Number_Of_Octaves);
            InitializeTerrain();
            terrain.terrainData.SetHeights(0, 0, dsnInterp);
            FillTexture(dsnInterp, 0);
        }

        else if (noiseType == NoiseType.BOTH)
        {
            fbm = genNoise(terrainSize);
            dsn = GenerateDSNNoise();
            float[,] newNoise = mergeNoise(fbm, dsn);
            newNoise = genSmoothNoise(newNoise, Number_Of_Octaves);
            InitializeTerrain();
            terrain.terrainData.SetHeights(0, 0, newNoise);
            FillTexture(newNoise, 0);
        }
        
        RECALCULATE = false;
    }
    void InitializeTerrain()
    {
        terrain.terrainData.heightmapResolution = terrainSize-1;
        terrain.terrainData.alphamapResolution = textureRes;

        terrain.terrainData.SetDetailResolution(textureRes, 16);
        terrain.terrainData.baseMapResolution = textureRes;
        terrain.terrainData.size = new Vector3(terrainSize - 1, terrainHeight, terrainSize - 1);
        terrain.terrainData.splatPrototypes = splatProto;
    }

    void FillTexture(float [,] noiseData, int quadNum)
    {
        //GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        //quad.name += quadNum.ToString();
        //quad.transform.position = new Vector3(quadNum, 0, 0);
        //Material mat = new Material(quad.GetComponent<Renderer>().material);
        //quad.GetComponent<Renderer>().material = mat;

        float[,,] singlePoint = new float[textureRes, textureRes, splatProto.Length];
        float stepSize = 1f / textureRes;
        for (int i = 0; i < textureRes; i++)
        {
            for (int j = 0; j < textureRes; j++)
            {
                float h, s, v;
                float rawV;
                float temp = noiseData[i, j];
                Color heightMapColor = new Color(temp, temp, temp);
                EditorGUIUtility.RGBToHSV(heightMapColor, out h, out s, out rawV);

                float vNorm11 = (rawV * 2) - 1f;
                float sigmoidv1 = (float)System.Math.Tanh(vNorm11 * contrastScale);
                v = (sigmoidv1 + 1) * 0.5f;

                texture.SetPixel(i, j, new Color(v, v, v, 1));
                singlePoint[i, j, 0] = 1.0f - v;
                singlePoint[i, j, 1] = v;
            }
        }
        terrain.terrainData.SetAlphamaps(0, 0, singlePoint);

        texture.Apply(false);
        //quad.GetComponent<Renderer>().material.mainTexture = texture;
        
        //Save image
        //byte[] data = texture.EncodeToPNG();
        //System.IO.File.WriteAllBytes(Application.dataPath + "/../SavedScreenNewSigmoid.png", data);
    }
    float[,] mergeNoise(float[,] noise1, float[,] noise2)
    {
        float[,] mergedNoise = new float[terrainSize, terrainSize];
        for (int i = 0; i < terrainSize; i++)
        {
            for (int j = 0; j < terrainSize; j++)
            {
                mergedNoise[i, j] = (noise1[i, j] + noise2[i, j])/2;
            }
        }

        return mergedNoise;
    }

    //FRACTAL-BROWNIAN MOTION

    float [,] genNoise(int size)
    {
        System.Random random = new System.Random(seedValue);
        float[,] values = new float[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                values[i,j] = (float)random.NextDouble() % 1;
            }
        }

        return values;
    }

    float [,] genSmoothNoise(float [,] noise, int octave)
    {
        float[,] smoothNoise = new float[terrainSize, terrainSize];
 
        int samplePeriod = 1 << octave; // calculates 2 ^ octave (the kth octave)
        float sampleFrequency = 1.0f / samplePeriod;
 
        for (int i = 0; i < terrainSize; i++)
        {
            //calculate the horizontal sampling indices
            int sample_i0 = (i / samplePeriod) * samplePeriod;
            int sample_i1 = (sample_i0 + samplePeriod) % terrainSize; //wrap around
            float horizontal_blend = (i - sample_i0) * sampleFrequency;
            for (int j = 0; j < terrainSize; j++)
            {
                //calculate the vertical sampling indices
                int sample_j0 = (j / samplePeriod) * samplePeriod;
                int sample_j1 = (sample_j0 + samplePeriod) % terrainSize; //wrap around
                float vertical_blend = (j - sample_j0) * sampleFrequency;
  
                
                //blend the top two corners
                float top = Interpolate(noise[sample_i0,sample_j0],
                   noise[sample_i1,sample_j0], horizontal_blend);
  
                //blend the bottom two corners
                float bottom = Interpolate(noise[sample_i0,sample_j1],
                   noise[sample_i1,sample_j1], horizontal_blend);
  
                //final blend
                smoothNoise[i,j] = Interpolate(top, bottom, vertical_blend);
            }
        }
        return smoothNoise;
    }

    float Interpolate(float x0, float x1, float alpha)
    {
        float ft = alpha * 3.1415927f;
        float f = (1 - Mathf.Cos(ft)) * 0.5f;

        return x0 * (1 - f) + x1 * f;
    }

    float Cubic_Interpolate(float v0, float v1, float v2, float v3, float x)
    {
        float P = (v3 - v2) - (v0 - v1);
        float Q = (v0 - v1) - P;
        float R = v2 - v0;
        float S = v1;

        return (P * Mathf.Pow(x, 3)) + (Q * Mathf.Pow(x, 2)) + (R * x) + S;
    }

    float[,] GenerateFBMNoise(float[,] noise, int octaveCount)
    {
       float[][,] smoothNoise = new float[octaveCount][,];
 
       float _persistance = persistence;
 
       //generate smooth noise
       for (int i = 0; i < octaveCount; i++)
       {
           smoothNoise[i] = genSmoothNoise(noise, i);
       }
        float[,] fbm = new float[terrainSize, terrainSize];
        float amplitude = 1.0f;
        float totalAmplitude = 0.0f;


        //blend noise together
        for (int octave = octaveCount - 1; octave >= 0; octave--)
        {
            amplitude *= _persistance;
            totalAmplitude += amplitude;
            for (int i = 0; i < terrainSize; i++)
            {
                for (int j = 0; j < terrainSize; j++)
                {
                    fbm[i, j] += smoothNoise[octave][i, j] * amplitude;
                }
            }

        }

        //normalisation
        for (int i = 0; i < terrainSize; i++)
        {
            for (int j = 0; j < terrainSize; j++)
            {
                fbm[i, j] /= totalAmplitude;
            }
        }

        return fbm;
    }


    
    //DIAMOND-SQUARE ALGORITHM

    //get the points to make a new square 
    float avgOfDiamondValues(int posX, int posY, int distFromEndPoints, float[,] noise, char pos)
    {
        if(pos == 'h')
        {
            int x = posX - distFromEndPoints;
            int x2 = posX + distFromEndPoints;
            return ((float)(noise[posX - distFromEndPoints, posY] + 
                            noise[posX + distFromEndPoints,posY]) * 0.25f);
        }
        else if(pos == 'v')
        {
            int y = posY - distFromEndPoints;
            int y2 = posY + distFromEndPoints;
            return ((float)(noise[posX, posY - distFromEndPoints] +
                            noise[posX, posY + distFromEndPoints]) * 0.25f);
        }


        return 0;
    }

    //get the center point in a diamond
    float avgOfSquareValues(int i, int j, int distFromEndPoints, float [,] noise)
    {
        return ((float)((noise[i - distFromEndPoints, j - distFromEndPoints] + 
                         noise[i + distFromEndPoints, j - distFromEndPoints] + 
                         noise[i - distFromEndPoints, j + distFromEndPoints] +  
                         noise[i + distFromEndPoints, j + distFromEndPoints]) * 0.25f));
    }

    float [,] GenerateDSNNoise()
    {
        int stride, adjustedSize;
        float ratio, scale;
        float[,] noise = new float[terrainSize,terrainSize];

        adjustedSize = terrainSize - 1;
        System.Random random = new System.Random(seedValue);
        ratio = (float)Mathf.Pow(2.0f, -smooth_factor);
        scale = UnityEngine.Random.Range(0.0f,1.0f) * ratio;
        stride = adjustedSize / 2;
        //TODO: change the = 0 to values from FBM noise
        noise[0, 0] = noise[adjustedSize, adjustedSize] = noise[0, adjustedSize] = noise[adjustedSize, 0] = UnityEngine.Random.Range(0.0f, 0.3f);

        while(stride != 0)
        {
            for (int i = stride; i < adjustedSize; i += stride)
            {
                for (int j = stride; j < adjustedSize; j += stride)
                {
                    noise[i, j] = scale * randomValBetweenRange(0.5f) + avgOfSquareValues(i,j,stride,noise);
                }
            }

            int basePosX, basePosY;
            for (int i = 0; i < adjustedSize; i += (stride * 2))
            {
                for (int j = 0; j < adjustedSize; j += (stride * 2))
                {
                    basePosX = i;
                    basePosY = j;

                   //bottom
                    noise[basePosX + stride, basePosY] = scale * randomValBetweenRange(0.5f) + 
                                                         avgOfDiamondValues(basePosX + stride,basePosY,stride,noise,'h');
                    //left                    
                    noise[basePosX, basePosY + stride] = scale * randomValBetweenRange(0.5f) +
                                                         avgOfDiamondValues(basePosX, basePosY + stride, stride, noise, 'v');
                    //right
                    noise[basePosX + (stride * 2), basePosY + stride] = scale * randomValBetweenRange(0.5f) +
                                                                        avgOfDiamondValues(basePosX + (stride * 2) , basePosY + stride, stride, noise, 'v');
                    //top
                    noise[basePosX + stride, basePosY + (stride * 2)] = scale * randomValBetweenRange(0.5f) + 
                                                                        avgOfDiamondValues(basePosX + stride, basePosY + (stride * 2) , stride, noise, 'h');
                }
            }
            scale *= ratio;
            stride = stride / 2;
        }

        return noise;
    }

    float randomValBetweenRange(float value)
    {
        float temp = UnityEngine.Random.Range(-value, value);
        return temp;
    }
}