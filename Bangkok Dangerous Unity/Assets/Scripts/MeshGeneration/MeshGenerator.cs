using UnityEngine;
namespace MeshGeneration{
    public class MeshGenerator : MeshContainer{
        
        [Range(1, 10)]
        public int Resolution = 1;
        
        protected virtual void GenerateMesh(){}
    }
}