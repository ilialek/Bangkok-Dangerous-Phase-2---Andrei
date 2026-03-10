using Utilities;
namespace Procedural
{
    public enum ProceduralMeshType
    {
        Road,
        Intersection,
        RoadSidewalk,
        IntersectionSidewalk,
        BlockArea
    }

    [System.Serializable]
    public struct ProceduralElement
    {
        public GUID Guid;
        public ProceduralMeshType MeshType;
        public bool Reverse;
    }
}