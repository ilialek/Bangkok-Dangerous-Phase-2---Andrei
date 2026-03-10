using UnityEngine;
using Random = System.Random;

public static class Noise
{
    public static float[,] GeneratePerlinNoise(Random random, int width, int height, NoiseSettings noiseSettings)
    {
        float[,] map = new float[width, height];

        float offsetX = (float)(random.NextDouble() * 200000 - 100000);
        float offsetY = (float)(random.NextDouble() * 200000 - 100000);

        if (noiseSettings.Scale <= 0)
        {
            noiseSettings.Scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplutide = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < noiseSettings.Octaves; i++)
                {
                    float value = Mathf.PerlinNoise(offsetX + x / noiseSettings.Scale * frequency, offsetY + y / noiseSettings.Scale * frequency) * 2 - 1;

                    noiseHeight += value * amplutide;
                    amplutide *= noiseSettings.Persistance;
                    frequency *= noiseSettings.Lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                map[x, y] = noiseHeight;
            }
        }

        //Normalize values
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                map[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, map[x, y]);
            } 
        }

        return map;
    }

    public static float[,] MultiplyNoise(float[,] data1, float[,] data2)
    {
        int width = data1.GetLength(0);
        int height = data1.GetLength(1);

        if (width != data2.GetLength(0) || height != data2.GetLength(1))
        {
            Debug.Log("Can not multiply maps with different sizes");

            return data1;
        }

        float[,] result = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = data1[x, y] * data2[x, y];
            }
        }

        return result;
    }
}