using UnityEngine;
using UnityEditor;
using System.Collections;
using System;

public class NoiseGeneration : MonoBehaviour 
{
    //public int test;
    public int Number_Of_Octaves;
    public float persistence;
    private float[,] heightMap = new float[terrainSize,terrainSize];
    
    private Texture2D texture;
    private int textureRes = 256;
    
    public const int terrainSize = 512;
    public Terrain terrain;

    //void Awake()
    //{
    //    texture = new Texture2D(textureRes, textureRes, TextureFormat.RGB24,true);
    //    texture.name = "TextureTest";
    //    texture.wrapMode = TextureWrapMode.Clamp;
    //    GetComponent<Renderer>().material.mainTexture = texture;
    //    //FillTexture();
    //}

    void Start()
    {
        //float stepSize = 1f / textureRes;
        //for (int i = 0; i < terrainSize; i += 2)
        //{
        //    for (int j = 0; j < terrainSize; j+=2)
        //    {
        //        float h,s,v;
        //        float temp = PerlinNoise_2D(i,j);

        //        heightMap[j, i] = temp;
        //        Color heightMapColor = new Color(temp, temp, temp);
        //        EditorGUIUtility.RGBToHSV(heightMapColor, out h, out s, out v);
        //        texture.SetPixel(i, j, new Color(v,v,v,1));

        //    }
        //}
        //texture.Apply();
        //terrain.terrainData.SetHeights(0, 0, heightMap);

        //second attempt

        float[,] fbm = genNoise(terrainSize);       
        terrain.terrainData.SetHeights(0, 0, GeneratePerlinNoise(fbm, Number_Of_Octaves,terrainSize));

    }

    void FillTexture()
    {
        for (int i = 0; i < textureRes; i++)
        {
            for (int j = 0; j < textureRes; j++)
            {
                texture.SetPixel(i, j, Color.red);
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

    float [,] genSmoothNoise(float [,] noise, int octave, int size)
    {       
        float[,] smoothNoise = new float [size,size];
 
        int samplePeriod = 1 << octave; // calculates 2 ^ k
        float sampleFrequency = 1.0f / samplePeriod;
 
        for (int i = 0; i < size; i++)
        {
          //calculate the horizontal sampling indices
          int sample_i0 = (i / samplePeriod) * samplePeriod;
          int sample_i1 = (sample_i0 + samplePeriod) % size; //wrap around
          float horizontal_blend = (i - sample_i0) * sampleFrequency;
 
          for (int j = 0; j < size; j++)
          {
             //calculate the vertical sampling indices
             int sample_j0 = (j / samplePeriod) * samplePeriod;
             int sample_j1 = (sample_j0 + samplePeriod) % size; //wrap around
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
        return x0 * (1 - alpha) + alpha * x1;
    }

    float[,] GeneratePerlinNoise(float[,] noise, int octaveCount, int size)
    {
       float[][,] smoothNoise = new float[octaveCount][,];
 
       float persistance = 0.5f;
 
       //generate smooth noise
       for (int i = 0; i < octaveCount; i++)
       {
           smoothNoise[i] = genSmoothNoise(noise, i,size);
       }
 
       float[,] perlinNoise = new float [size,size];
       float amplitude = 1.0f;
       float totalAmplitude = 0.0f;
 
       //blend noise together
       for (int octave = octaveCount - 1; octave >= 0; octave--)
       {
           amplitude *= persistance;
           totalAmplitude += amplitude;
 
           for (int i = 0; i < size; i++)
           {
              for (int j = 0; j < size; j++)
              {
                 perlinNoise[i,j] += smoothNoise[octave][i,j] * amplitude;
              }
           }
        }
 
       //normalisation
       for (int i = 0; i < size; i++)
       {
          for (int j = 0; j < size; j++)
          {
             perlinNoise[i,j] /= totalAmplitude;
          }
       }
 
       return perlinNoise;
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