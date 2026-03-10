using UnityEngine;
using GUID = Utilities.GUID;

namespace WirePoleSystem{
    [ExecuteInEditMode]
    public class WirePole : WireConnector{
        public new GUID Guid{
            get => ConnectorGuid;
            set{
                ConnectorGuid = value;
                gameObject.name = $"WirePole {value}";
            }
        }
        
        private new void Update(){
            base.Update();

        }
    }
}