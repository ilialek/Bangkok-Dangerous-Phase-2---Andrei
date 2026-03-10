using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RoadDataCollection : IProceduralData
{
    private List<RoadData> m_Splines = new List<RoadData>();

    public void Serialize(StreamWriter writer)
    {
        
    }

    public void Deserialize(StreamReader reader)
    {
        
    }
}

public class RoadData
{
    public List<ControlPoint> Points = new List<ControlPoint>();
    
    public void Serialize(StreamWriter writer)
    {
        string json = JsonUtility.ToJson(Points);
        writer.Write(json);
    }

    public void Deserialize(StreamReader reader)
    {
        JsonUtility.FromJson<List<ControlPoint>>(reader.ReadToEnd());
    }
}

public struct ControlPoint
{
    public Vector3 Position;
    public Vector3 Forward;
    public Vector3 Up;
}