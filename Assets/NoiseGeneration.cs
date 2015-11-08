using UnityEngine;
using UnityEditor;
using System.Collections;
using System;

public class NoiseGeneration : MonoBehaviour 
{
    public int Number_Of_Octaves;
    public float persistence;    
    public int terrainSize, terrainHeight;
    public Terrain terrain;
    
    private Texture2D texture;
    private int textureRes = 512;

    public bool recalculate = false;

    public float roughness_factor = 1;

    void Awake()
    {
        texture = new Texture2D(textureRes, textureRes, TextureFormat.RGB24, true);
        texture.name = "TextureTest";
        texture.wrapMode = TextureWrapMode.Clamp;
        GetComponent<Renderer>().material.mainTexture = texture;
        //FillTexture();
    }

    void Start()
    {
        //second attempt
        float[,] fbm = genNoise(terrainSize);
        
        terrain.terrainData.heightmapResolution = terrainSize;
        terrain.terrainData.alphamapResolution = 128;
        terrain.terrainData.SetDetailResolution(terrainSize - 1, 16);
        terrain.terrainData.baseMapResolution = terrainSize - 1 + 1;
        terrain.terrainData.SetHeights(0, 0, GenerateFBMNoise(fbm, Number_Of_Octaves));
        terrain.terrainData.size = new Vector3(terrainSize - 1, terrainHeight, terrainSize - 1);
        //terrain.terrainData[col * CHUNKS + row].splatPrototypes = test;
    }

    void Update()
    {
        if(recalculate == true)
        {
            float[,] fbm = genNoise(terrainSize);
            terrain.terrainData.heightmapResolution = terrainSize;
            terrain.terrainData.alphamapResolution = 128;
            terrain.terrainData.SetDetailResolution(terrainSize - 1, 16);
            terrain.terrainData.baseMapResolution = terrainSize - 1 + 1;
            terrain.terrainData.SetHeights(0, 0, GenerateFBMNoise(fbm, Number_Of_Octaves));
            terrain.terrainData.size = new Vector3(terrainSize - 1, terrainHeight, terrainSize - 1);
            recalculate = false;
        }
    }

    void FillTexture(float [,] noiseData)
    {
        float stepSize = 1f / textureRes;
        for (int i = 0; i < textureRes; i++)
        {
            for (int j = 0; j < textureRes; j++)
            {
                float h, s, v;
                float temp = noiseData[i, j];
                Color heightMapColor = new Color(temp, temp, temp);
                EditorGUIUtility.RGBToHSV(heightMapColor, out h, out s, out v);
                texture.SetPixel(i, j, new Color(v, v, v, 1));

            }
        }
        texture.Apply();
    }

    float [,] genNoise(int size)
    {
        System.Random random = new System.Random(0);
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
            
            //int sample_i0 = (i - 1 / samplePeriod) * samplePeriod;

            //int sample_i1 = (i / samplePeriod) * samplePeriod;
            //int sample_i2 = (sample_i1 + samplePeriod) % terrainSize; //wrap around

            //int sample_i3 = 0;
            //if(i < terrainSize - 1)
            //    sample_i3= (sample_i2 + samplePeriod) % terrainSize;             
            
            //float horizontal_blend = (i - sample_i1) * sampleFrequency;
 
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
                

                ////Testing Cubic Interpolation
                ////int sample_j0 = (j-1 / samplePeriod) * samplePeriod;
                //int sample_j1 = (j / samplePeriod) * samplePeriod;
                //int sample_j2 = (sample_j1 + samplePeriod) % terrainSize; //wrap around
                ////int sample_j3 = (sample_j2 + samplePeriod) % terrainSize;             
           
                ////float vertical_blend = (i - sample_j1) * sampleFrequency;

                ////float top = Cubic_Interpolate(noise[sample_i0, sample_j0], noise[sample_i1, sample_j1], noise[sample_i2, sample_j2], noise[sample_i3, sample_j0], horizontal_blend);
                ////float bottom = Cubic_Interpolate(noise[sample_i0, sample_j1], noise[sample_i1, sample_j1], noise[sample_i2, sample_j1], noise[sample_i3, sample_j1], horizontal_blend);

                //float x = noise[sample_i2,sample_j1] - noise[sample_i1,sample_j1];
                ////float temp = Cubic_Interpolate(pointbefore,top,bottom, pointafter,vertical_blend);
                ////if (noise[sample_i0, sample_j1] == null || noise[sample_i1, sample_j1] == null || noise[sample_i2, sample_j1] == null || noise[sample_i3, sample_j1] == null)
                ////    Debug.Log("i " + i + " j " + j + "  " + sample_i0 + "  " + sample_i1 + "  " + sample_i2 + "  " + sample_i3 + "  " + sample_j1);
                //smoothNoise[i, j] = Cubic_Interpolate(noise[sample_i0, sample_j1], noise[sample_i1, sample_j1], noise[sample_i2, sample_j1], noise[sample_i1, sample_j1], horizontal_blend);

            }
         }
        return smoothNoise;
    }

    float Interpolate(float x0, float x1, float alpha)
    {
        float ft = alpha * 3.1415927f;
        float f = (1 - Mathf.Cos(ft)) * 0.5f;

        return x0 * (1 - f) + x1 * f;

        //return x0 * (1 - alpha) + alpha * x1;
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
       float[,] fbm = new float [terrainSize,terrainSize];
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
             fbm[i,j] /= totalAmplitude;
          }
       }

       return fbm;
    }

    //diamond-square algorithm
    float avgOfEndPoints(int i, int j, int distFromEndPoints, float [,] noise)
    {
        return ((float)(noise[i - distFromEndPoints, j - distFromEndPoints] 
                      + noise[i + distFromEndPoints, j + distFromEndPoints]) * 0.5f);
    }

    float avgOfDiamondValues(int i, int j, int distFromDiamondCenter, int size, float [,] noise)
    {
        if(i == 0)  //bottom left 
        {
            return ((float)(noise[(i*size) + j-distFromDiamondCenter, 2]);
        }
        else if(j == 0) //bottom left
        {

        }
        else if (i == size - 1)  //top right
        {

        }
        else if (j == size - 1)  //top right
        {

        }
        else    //all points within size. Every other point
        {

        }
    }

    float avgOfSquareValues(int i, int j, int distFromEndPoints, float [,] noise)
    {
        return ((float)((noise[i-distFromEndPoints,j-distFromEndPoints] + 
                        noise[i+distFromEndPoints,j-distFromEndPoints] + 
                        noise[i-distFromEndPoints,j+distFromEndPoints] + 
                        noise[i+distFromEndPoints,j+distFromEndPoints]) * 0.25f));
    }

    void diamondSquareNoise(float [,] fbm)
    {
        int stride, subSize;
        float ratio, scale, oddline;
        float[,] noise = new float[terrainSize,terrainSize];

        subSize = terrainSize;
        System.Random random = new System.Random(5);
        ratio = (float)Mathf.Pow(2.0f, -roughness_factor);
        scale = terrainHeight * ratio;
        stride = terrainSize / 2;
        //TODO: change the = 0 to values from FBM noise
        noise[0,0] = noise[terrainSize-1,terrainSize-1] = noise[0,terrainSize] = noise[terrainSize,0] = fbm[0,0];

        while(stride != 0)
        {
            for (int i = stride; i < terrainSize; i+=stride)
            {
                for (int j = stride; j < terrainSize; j+=stride)
                {
                    noise[i, j] = scale * randomValBetweenRange(0.5f) + avgOfSquareValues(i,j,stride,noise);
                    j += stride;
                }
                i += stride;
            }
            oddline = 0;
            for (int i = 0; i < terrainSize; i+=stride)
            {
                for (int j = 0; j < terrainSize; j+=stride)
                {
                    if (oddline != 0 && j==0)
                        j += stride;

                    noise[i,j] = 
                }
            }
        }

        

    }

    float randomValBetweenRange(float value)
    {
        return (UnityEngine.Random.Range(-value, value));
        //int r;
        //float x;
        //r = System.Random();
        //x = (float)(r & 0x7ffff)/(float)0x7ffff;
        //return (x * (max - min) + min);
    }



    /*
    float PerlinNoise_2D(float x, float y)
    {
        float total = 0;
        float p = persistence;
        float n = Number_Of_Octaves - 1;
        float frequency = 0;
        float amplitude = 0;

        for (int i = 0; i < n; i++)
		{
		    frequency = Mathf.Pow(2,i);
	        amplitude = Mathf.Pow(p,i);

            total = total + InterpolatedNoise(x * frequency, y * frequency) * amplitude;
		}

        return (total+1)/2;
    }   

    
	float Noise(System.Int32 x, System.Int32 y)	
    {
        int n = x + y * 57;
        n = (n<<13) ^ n;
        return (float)( 1.0 - ( (n * (n * n * 15731 + 789221) + 1376312589) & 2147483647) / 1073741824.0); 
    }

    float SmoothNoise(float x, float y)
    {
        int xInt = (int)x;
        int yInt = (int)y;
        float corners = (Noise(xInt - 1, yInt - 1) + Noise(xInt + 1, yInt - 1) + Noise(xInt - 1, yInt + 1) + Noise(xInt + 1, yInt + 1)) / 16;
        float sides = (Noise(xInt - 1, yInt) + Noise(xInt + 1, yInt) + Noise(xInt, yInt - 1) + Noise(xInt, yInt + 1)) / 8;
        float center = Noise(xInt, yInt) / 4;
        return corners + sides + center;
    }

    float InterpolatedNoise(float x, float y)
    {
      int xInt = (int)x;
      float fractional_X = x - xInt;

      int yInt = (int)y;
      float fractional_Y = y - yInt;

      float v1 = SmoothNoise(xInt, yInt);
      float v2 = SmoothNoise(xInt + 1, yInt);
      float v3 = SmoothNoise(xInt, yInt + 1);
      float v4 = SmoothNoise(xInt + 1, yInt + 1);

      //float i1 = Interpolate(v1 , v2 , fractional_X);
      //float i2 = Interpolate(v3 , v4 , fractional_X);

      float i1 = Cubic_Interpolate(v1, v2, v3 ,v4, x);
//      float i2 = Cubic_Interpolate(v1, v2, v3, v4, x);

      return i1;// Interpolate(i1, i2, fractional_Y);
    }

     float Cubic_Interpolate(float v0, float  v1, float v2, float v3, float x)
     {
         float P = (v3 - v2) - (v0 - v1);
	    float Q = (v0 - v1) - P;
	    float R = v2 - v0;
	    float S = v1;

        return (P*Mathf.Pow(x,3)) + (Q*Mathf.Pow(x,2)) + (R*x) + S;
     }

    float Interpolate(float a, float  b, float x)
    {
	    float ft = x * 3.1415927f;
	    float f = (1 - Mathf.Cos(ft)) * 0.5f;

        return (a * (1 - f) + b * f);
    }
    */

}