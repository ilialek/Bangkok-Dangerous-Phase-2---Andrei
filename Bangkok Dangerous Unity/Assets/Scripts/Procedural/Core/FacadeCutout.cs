using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    [System.Serializable]
    public class FacadeCutout
    {
        public FacadeHandle FacadeHandle;
        public List<Quad3D> Quads;

        public FacadeCutout(FacadeHandle facadeHandle)
        {
            FacadeHandle = facadeHandle;
            Quads = new List<Quad3D>();
        }
    }

    [System.Serializable]
    public struct Facade
    {
        public Quad3D Face;
        public FacadeHandle Handle;

        public Facade(Quad3D face, FacadeHandle handle)
        {
            Face = face;
            Handle = handle;
        }
    }

    [System.Serializable]
    public class FacadeList
    {
        public List<Facade> Facades;
        public List<Vector3> Roof;
        public bool HasRoof;
        public float Height;
        public FacadeSemantic Semantics;

        public FacadeList(float height, FacadeSemantic semantics)
        {
            Facades = new List<Facade>();
            Roof = new List<Vector3>();
            HasRoof = false;
            Height = height;
            Semantics = semantics;
        }
    }

    [System.Serializable]
    public struct FacadeHandle
    {
        public int Level;
        public int Side;

        public FacadeHandle(int level, int side)
        {
            Level = level;
            Side = side;
        }
    }
}