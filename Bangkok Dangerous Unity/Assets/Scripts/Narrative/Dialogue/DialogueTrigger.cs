using UnityEngine;
using GameArchitecture;

namespace Dialogue
{
    public class DialogueTrigger : MonoBehaviour, IReference
    {
        public string TargetTag = "Player";
        public ScriptableDialogue Dialogue;

        private DialogueManager m_DialogueManager;

        public void OnTriggerEnter(Collider other)
        {
            if (Dialogue != null && other.gameObject.CompareTag(TargetTag))
            {
                m_DialogueManager?.StartDialogue(Dialogue);
            }
        }

        public void Register() { }

        public void Setup()
        {
            ReferenceManager.TryGetReference_Scene(gameObject, out m_DialogueManager);
        }
    }
}