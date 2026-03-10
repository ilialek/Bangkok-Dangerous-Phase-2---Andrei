using UnityEngine;
namespace MeshGeneration.Runtime{
    public class RuntimeMeshGenerator : MeshGenerator{
        private void OnValidate(){
            enabled = true;
        }

        private void Update(){
            GenerateMesh();
            enabled = false;
        }

        private void Awake(){
            Mesh = new Mesh{
                name = "Generated Mesh"
            };
            GetComponent<MeshFilter>().mesh = Mesh;
        }
    }
}