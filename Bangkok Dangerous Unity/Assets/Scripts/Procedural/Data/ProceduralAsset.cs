using UnityEngine;

namespace Procedural
{
    [CreateAssetMenu(fileName = "ProceduralAsset", menuName = "Procedural/Procedural Asset")]
    public class ProceduralAsset : ScriptableObject
    {
        public GameObject Prefab;
        ProceduralAssetType Type;
        public Bounds BoundingBox;

        public Vector3 Direction = Vector3.forward;
        public AttachmentSettings Attachment;

        [Tooltip("Offset based on the direction")]
        public Vector3 Offset;

        [Tooltip("Whether the buildings needs to be cut into (for objects that require holes)")]
        public bool RequiresCut;

        public Vector2 CutScale = Vector2.one;

        [Tooltip("0 = Object cannot be stretched. The aspect ratio always stays the same. 1 = Object can be stretched to fit target area"), Range(0, 1)]
        public float Stretch;

        [SerializeField, HideInInspector] public OrientedBounds OrientedBounds;
    }

    public enum ProceduralAssetType
    {
        Window,
        Door,
        Front,
        Other
    }
}