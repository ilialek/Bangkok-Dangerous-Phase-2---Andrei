using UnityEngine;
using Utilities;
namespace Procedural
{
    [RequireComponent(typeof(MeshFilter)), ExecuteInEditMode]
    public abstract class ProceduralMesh : MonoBehaviour
    {
        [SerializeField, HideInInspector] protected GUID m_Guid;

        private ProceduralManager m_ProceduralManager;

        public GUID Guid
        {
            get => m_Guid;
            set => m_Guid = value;
        }

        public Mesh Mesh
        {
            get => GetComponent<MeshFilter>().sharedMesh;
            set => GetComponent<MeshFilter>().sharedMesh = MeshUtils.CreateMeshAsset(value, m_Guid, ProceduralManager.TargetMeshCollection);
        }

        public ProceduralManager ProceduralManager
        {
            get
            {
                if (m_ProceduralManager == null)
                {
                    m_ProceduralManager = GetComponentInParent<ProceduralManager>();

                    if (m_ProceduralManager == null) return null;
                }

                if (!m_ProceduralManager.Initialized)
                {
                    m_ProceduralManager.Initialize();
                }

                return m_ProceduralManager;
            }
        }

        public void Load()
        {

        }

        public void Unload()
        {
            Destroy(gameObject);
        }

        [ContextMenu("Regenerate")]
        public void Generate() => Generate(true);

        public abstract void Generate(bool generateAttachedMeshes = true);

        public virtual void Selected() 
        {
            if (!ProceduralManager.Initialized)
            {
                ProceduralManager.Initialize();
            }
        }

        public virtual void Deselected()
        {
            MeshUtils.SaveMesh();
        }

        public virtual void OnUndoRedo() { }
    }
}