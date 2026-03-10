using UnityEngine;
namespace MeshGeneration.Interface{
    public interface IMeshGenerator{
        public int VertexCount{ get; }
        public int IndexCount{ get; }
        public int JobLength{ get; }
        public Bounds Bounds{ get; }
        public int Resolution{ get; set; }
        
        public void Execute<TStream>(int index, TStream stream) where TStream : struct, IMeshStream;
    }
}