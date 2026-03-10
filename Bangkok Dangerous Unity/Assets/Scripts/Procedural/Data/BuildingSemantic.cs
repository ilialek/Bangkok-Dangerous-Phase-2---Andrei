using System;
using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    [CreateAssetMenu(fileName = "BuildingSemantic", menuName = "Procedural/Building Semantic")]
    public class BuildingSemantic : ScriptableObject
    {
        [Header("Base")]

        [Tooltip("Decorative attachnents that can generate on the building")]
        public List<ProceduralAsset> Attachments = new List<ProceduralAsset>();

        [Tooltip("Type of collider that is generated for the building")]
        public BuildingColliderType Collider;


        [Header("Facades")]

        public List<FacadeSemantic> FrontSemantics = new List<FacadeSemantic>();
        public List<FacadeSemantic> UpperFacades = new List<FacadeSemantic>();

        public void Validate()
        {
            foreach (FacadeSemantic facade in FrontSemantics) 
            {
                facade.Validate();
            }

            foreach (FacadeSemantic facade in UpperFacades)
            {
                facade.Validate();
            }
        }
    }

    [Flags]
    public enum BuildingColliderType
    {
        None = 0,
        OuterFirstFloor = 1 << 0,
        InteriorFirstFloor = 1 << 1,
        Ground = 1 << 2
    }
}