using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Utilities.Splines;

namespace Procedural
{
    public class Road : ProceduralMesh
    {
        public Spline Spline;

        [SerializeField] private RoadSettings m_Settings;

        public RoadSettings Settings => m_Settings;

        [SerializeField, HideInInspector] private List<SplinePosition> m_CachedVerticesLeft = new List<SplinePosition>();
        [SerializeField, HideInInspector] private List<SplinePosition> m_CachedVerticesRight = new List<SplinePosition>();

        public void Construct()
        {
            m_Guid = GUID.Create();
            Spline = new Spline();
        }

        private void OnDestroy()
        {
            ProceduralManager?.RoadRemoved(m_Guid);
        }

        public override void Selected()
        {
           base.Selected();

            Spline.OnChange += Generate;
        }

        public override void Deselected()
        {
            base.Deselected();

            Spline.OnChange -= Generate;
        }

        public override void Generate(bool generateAttachedMeshes = true)
        {
            Mesh = RoadGeneration.GenerateRoadMesh(Spline, m_Settings, out m_CachedVerticesLeft, out m_CachedVerticesRight);
            
            if (generateAttachedMeshes)
            {
                ProceduralManager?.RegenerateRoadAttachments(m_Guid);
            }
        }

        /// <summary>
        /// Get cached vertices. False = Right, True = Left
        /// </summary>
        public List<SplinePosition> GetCachedVertices(bool side)
        {
            return side ? m_CachedVerticesLeft : m_CachedVerticesRight;
        }
    }
}