using System.Collections.Generic;
using UnityEngine;
using Utilities;
namespace Procedural
{
    public class Building : ProceduralMesh
    {
        public BuildingSemantic Semantics;

        [HideInInspector] public List<Vector3> Lot = new List<Vector3>();
        [HideInInspector] public List<FacadeList> Facades = new List<FacadeList>();

        [SerializeField] private int m_Seed;
        public int Seed => m_Seed;

        [SerializeField] private BuildingSettings m_Settings;

        public void Construct()
        {
            m_Guid = GUID.Create();
            Randomize();
            Generate();
        }
        
        public override void Generate(bool generateAttachedMeshes = true)
        {
            if (!Semantics)
            {
                Debug.LogWarning($"{name} does not have the semantic settings set");
                return;
            }

            Semantics.Validate();

            // Generate building mesh
            BuildingGeneration buildingGeneration = new BuildingGeneration(m_Seed, m_Settings, Semantics);
            Mesh = buildingGeneration.Generate(Lot, null, out Facades);

            // Generate decoratives
            RemoveDecoratives();

            if (!Semantics)
            {
                Debug.LogWarning($"{name} does not have the building semantic settings set");
                return;
            }

            DecorativeGeneration decorativeGeneration = new DecorativeGeneration(m_Seed ^ 1, Semantics, m_Settings);
            decorativeGeneration.Generate(this, Facades, out List<FacadeCutout> facadeCutouts);

            // Regenerate building mesh if any cutouts were registered
            if (facadeCutouts != null && facadeCutouts.Count > 0)
            {
                buildingGeneration = new BuildingGeneration(m_Seed, m_Settings, Semantics);
                Mesh = buildingGeneration.Generate(Lot, facadeCutouts, out Facades);
            }

            bool addCollider = Semantics.Collider != BuildingColliderType.None && Facades.Count > 0 && Semantics.Collider.HasFlag(BuildingColliderType.OuterFirstFloor) && Facades[0].Facades.Count > 2;

            // Generate building collider
            if (!addCollider)
            {
                RemoveCollider();
            }
            else
            {
                Dictionary<FacadeHandle, FacadeCutout> cutoutsFromHandle = new Dictionary<FacadeHandle, FacadeCutout>();
                foreach (FacadeCutout cutout in facadeCutouts)
                {
                    if (cutout.FacadeHandle.Level > 0) continue;

                    cutoutsFromHandle.Add(cutout.FacadeHandle, cutout);
                }

                MeshCreator colliderMeshCreator = new MeshCreator();

                FacadeList baseFacade = Facades[0];
                bool interiorColliders = Semantics.Collider.HasFlag(BuildingColliderType.InteriorFirstFloor);

                float wallThickness = baseFacade.Semantics.WallThickness;

                if (!interiorColliders)
                {
                    wallThickness = 0.0f;
                }

                int facadeCount = baseFacade.Facades.Count;

                for (int i = 0; i < facadeCount; i++)
                {
                    Facade facade = baseFacade.Facades[i];
                    Facade previousFacade = baseFacade.Facades[(i - 1 + facadeCount) % facadeCount];
                    Facade nextFacade = baseFacade.Facades[(i + 1) % facadeCount];

                    if (interiorColliders && cutoutsFromHandle.TryGetValue(facade.Handle, out FacadeCutout cutout))
                    {
                        BuildingGeneration.AddCutoutFacade(colliderMeshCreator, facade.Face, previousFacade.Face.BottomRight, nextFacade.Face.BottomLeft, cutout, wallThickness);
                    }
                    else
                    {
                        BuildingGeneration.AddFacade(colliderMeshCreator, facade.Face, previousFacade.Face.BottomRight, nextFacade.Face.BottomLeft, wallThickness);
                        colliderMeshCreator.AddQuad(facade.Face);
                    }   
                }

                AddCollider(colliderMeshCreator.GetMesh(), !interiorColliders && MeshUtils.IsConvex(Lot));
            }
        }

        private void AddCollider(Mesh mesh, bool convex)
        {
            if (!TryGetComponent(out MeshCollider meshCollider))
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = MeshUtils.CreateMeshAsset(mesh, m_Guid, ProceduralManager.TargetMeshCollection, "Colliders");
            meshCollider.convex = convex;
        }

        private void RemoveCollider()
        {
            if (TryGetComponent(out MeshCollider meshCollider))
            {
                MeshUtils.DeleteMeshAsset(Guid, ProceduralManager.TargetMeshCollection, "Colliders");
                DestroyImmediate(meshCollider);
            }
        }

        public override void OnUndoRedo()
        {
            GetComponentInParent<BlockArea>()?.Initialize();
        }

        public void RemoveDecoratives()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;

                if (child.tag == "Procedural")
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        public void Randomize()
        {
            m_Seed = SeedManager.CreateRandomSeed();
        }
    }
}