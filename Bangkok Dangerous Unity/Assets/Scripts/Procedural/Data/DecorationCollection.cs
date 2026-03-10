using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    [CreateAssetMenu(fileName = "DecorationCollection", menuName = "Procedural/Decoration Collection")]
    [System.Serializable]
    public class DecorationCollection : ScriptableObject
    {
        [SerializeField]
        public List<ProceduralAsset> DecorationPrefabs;
        [SerializeField]
        public List<ProceduralAsset> WirePoles;
    }
}