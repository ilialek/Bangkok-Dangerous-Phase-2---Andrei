using System.IO;

public interface IProceduralData
{
    public void Serialize(StreamWriter writer);
    
    public void Deserialize(StreamReader reader);
}