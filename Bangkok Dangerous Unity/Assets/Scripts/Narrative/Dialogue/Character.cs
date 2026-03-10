using UnityEngine;

namespace Dialogue
{
    [CreateAssetMenu(fileName = "Character", menuName = "Narrative/Character")]
    public class Character : ScriptableObject
    {
        public string Name;
        public Color NameColor;
    }
}