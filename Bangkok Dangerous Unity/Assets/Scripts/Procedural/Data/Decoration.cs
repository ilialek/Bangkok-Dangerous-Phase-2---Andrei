using UnityEngine;
using Utilities;

namespace Procedural
{
    public class Decoration : MonoBehaviour
    {
        public bool Procedural = true;
        private GUID m_Guid;
        
        public GUID Guid
        {
            get => m_Guid;
            set
            { 
                m_Guid = value;
                gameObject.name = $"{gameObject.name} {value}";
            }
        }
        public GUID ParentGuid{ get; set; }
    }
}