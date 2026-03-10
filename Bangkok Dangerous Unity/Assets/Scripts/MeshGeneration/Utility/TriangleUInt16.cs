using System.Runtime.InteropServices;
using Unity.Mathematics;
namespace MeshGeneration.Utility{
    [StructLayout(LayoutKind.Sequential)]
    public struct TriangleUInt16{
        public ushort a;
        public ushort b;
        public ushort c;
        
        public static implicit operator TriangleUInt16(int3 triple) => new(){
            a = (ushort) triple.x,
            b = (ushort) triple.y,
            c = (ushort) triple.z
        };
    }
}