using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Utilities;

namespace Procedural
{
    public class Sidewalk : ProceduralMesh
    {
        public GUID TargetGuid;
        public RoadAttachment RoadAttachment1;
        public RoadAttachment RoadAttachment2;
        public SidewalkType Type;

        private List<SidewalkBreak> m_Breaks = new List<SidewalkBreak>();

        public IReadOnlyList<SidewalkBreak> Breaks => m_Breaks;

        [SerializeField] private SidewalkSettings m_Settings = new SidewalkSettings();
        [SerializeField, HideInInspector] private List<SidewalkCache> m_CachedData = new List<SidewalkCache>(); // Cache outer vertices
        
        public SidewalkSettings Settings => m_Settings;

        public List<SidewalkCache> CachedData => m_CachedData;

        public void ConstructForRoad(GUID targetGuid, SidewalkType type)
        {
            m_Guid = GUID.Create();
            TargetGuid = targetGuid;
            Type = type;
            Generate(false);
        }
        
        public void ConstructForIntersection (GUID intersectionGuid, RoadAttachment roadAttachment1, RoadAttachment roadAttachment2)
        {
            m_Guid = GUID.Create();
            TargetGuid = intersectionGuid;
            RoadAttachment1 = roadAttachment1;
            RoadAttachment2 = roadAttachment2;
            Type = SidewalkType.Connection;
            Generate(false);
        }

        private void OnDestroy()
        {
            ProceduralManager?.SidewalkRemoved(m_Guid);
        }

        /// <summary>
        /// Generate complete sidewalk mesh
        /// </summary>
        public override void Generate(bool generateAttachedMeshes = true)
        {
            if (!ProceduralManager) return;
            
            if (Type == SidewalkType.Connection)
            {
                Mesh = RoadGeneration.GenerateSidewalkIntersectionMesh(ProceduralManager, m_Settings, m_Guid, TargetGuid, RoadAttachment1, RoadAttachment2, out m_CachedData);
            }
            else
            {
                Mesh = RoadGeneration.GenerateSidewalkRoadMesh(ProceduralManager, m_Settings, m_Guid, TargetGuid, Type == SidewalkType.RoadLeft ? -1 : 1, m_Breaks, out m_CachedData);
            }
            
            if (generateAttachedMeshes)
            {
                ProceduralManager?.RegenerateSidewalksAttachments(m_Guid);
            }
        }

        /// <summary>
        /// Adds a break to interrupt the sidewalk. If a sidewalk with an existing rason GUID is already added, it is overwritten. Orders all breaks based on the start
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBreak(GUID reason, float start, float end)
        {
            // Check if already added
            for (int i = m_Breaks.Count - 1; i >= 0; i--)
            {
                if (m_Breaks[i].Reason == reason)
                {
                    bool order = m_Breaks[i].Start == start;
                    m_Breaks[i] = new SidewalkBreak(reason, start, end);

                    if (order)
                    {
                        OrderBreaks();
                    }

                    return;
                }
            }

            m_Breaks.Add(new SidewalkBreak(reason, start, end));
            OrderBreaks();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public void RemoveBreak(int index)
        {
            m_Breaks.RemoveAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OrderBreaks()
        {
            m_Breaks.Sort((x, y) => x.Start.CompareTo(y.Start));
        }

        public override void OnUndoRedo()
        {
            if (!ProceduralManager) return;

            if (!ProceduralManager.SidewalkAreas.TryGetValue(Guid, out List<GUID> areaGuids)) return;

            foreach (GUID areaGuid in areaGuids)
            {
                if (!ProceduralManager.BlockAreas.TryGetValue(areaGuid, out BlockArea area)) continue;

                area.Generate(false);
            }
        }
    }

    public enum SidewalkType
    {
        RoadRight,
        RoadLeft,
        Connection
    }
}