using UnityEngine;

[System.Serializable]
public class NoiseSettings
{
    [Range(0.1f, 100.0f)] public float Scale = 20.0f;
    [Range(1, 8)] public int Octaves = 4;
    [Range(0, 10)] public float Persistance = 2.0f;
    [Range(0, 1)] public float Lacunarity = 0.5f;
}