using UnityEngine;

[System.Serializable]
public class MeshData
{
    public Mesh Mesh;
    public Vector3 Position;
    public MeshType MeshType;

    public MeshData(Mesh mesh, Vector3 position, MeshType type)
    {
        Mesh = mesh;
        Position = position;
        MeshType = type;
    }
}

public enum MeshType
{
    Terrain,
    Road,
    Building
}