using Utilities;
namespace Procedural
{
    [System.Serializable]
    public struct SidewalkHandle
    {
        public GUID Guid;
        public ProceduralMeshType MeshType;
        public bool Reverse;
        public int Index;

        public SidewalkHandle(GUID guid, ProceduralMeshType meshType, bool revese, int index)
        {
            Guid = guid;
            MeshType = meshType;
            Reverse = revese;
            Index = index;
        }
    }
}