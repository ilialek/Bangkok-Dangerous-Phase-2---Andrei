using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Procedural
{
    public class BlockArea : ProceduralMesh
    {
        public List<SidewalkHandle> Guids = new List<SidewalkHandle>();

        public Dictionary<GUID, Building> Buildings = new Dictionary<GUID, Building>();

        [SerializeField] private BlockAreaSettings m_Settings;

        [SerializeField, HideInInspector] private List<Vector3> m_CachedVertices = new List<Vector3>();
        public List<Vector3> CachedVertices => m_CachedVertices;

        public BlockAreaSettings Settings => m_Settings;

        public void Initialize()
        {
            Buildings.Clear();

            Building[] buildings = GetComponentsInChildren<Building>();
            foreach (Building building in buildings)
            {
                Buildings.Add(building.Guid, building);
            }
        }

        public void Construct(List<SidewalkHandle> elements)
        {
            m_Guid = GUID.Create();
            Guids = elements;
            Generate();
        }

        private void OnDestroy()
        {
            ProceduralManager?.BlockAreaRemoved(m_Guid);
        }

        public override void Generate(bool generateAttachedMeshes = true)
        {
			RoadGeneration.SortAreaElements(ProceduralManager, ref Guids);
			Mesh = RoadGeneration.GenerateBlockArea(ProceduralManager, m_Settings, Guids, out m_CachedVertices);
        }
    }
}