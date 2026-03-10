using System.Collections;
using TMPro;
using UnityEngine;
using FlowGraph;
using System.Collections.Generic;
using GameArchitecture;

#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace Dialogue 
{
    public class DialogueManager : MonoBehaviour, IReference
#if UNITY_EDITOR
        , IPreprocessBuildWithReport
#endif
    {
        public GameObject DialogueContainer;
        public TMP_Text CharacterName;
        public TMP_Text DialogueText;

        private bool m_Active;

        [SerializeField, HideInInspector] private List<(string, Character)> m_Characters;
        private Dictionary<string, Character> m_CharacterDictionary;

        public ScriptableDialogue TestDialogue;

        [ContextMenu("Test")]
        public void Test()
        {
            StartDialogue(TestDialogue);
        }

        public void Register()
        {
            ReferenceManager.AddReference_Scene(gameObject, this);
        }

        public void Setup() { }

        public void Awake()
        {
            SetState(false);

#if UNITY_EDITOR
            ResolveCharacters();
#endif
            m_CharacterDictionary = new Dictionary<string, Character>();
            if (m_Characters != null && m_Characters.Count > 0)
            {
                // Populate character dictionary for instant access
                foreach (var character in m_Characters)
                {
                    m_CharacterDictionary.Add(character.Item1, character.Item2);
                }
            }
        }

#if UNITY_EDITOR
        public void ResolveCharacters()
        {
            // Gather all characters that are present in the Assets
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Character");

            m_Characters = new List<(string, Character)>();
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Character character = UnityEditor.AssetDatabase.LoadAssetAtPath<Character>(path);
                if (character != null)
                {
                    m_Characters.Add((guid, character));
                }
            }
        }
#endif

        public void StartDialogue(ScriptableDialogue dialogue)
        {
            if (m_Active) return;

            SetState(true);
            StartCoroutine(ManageDialogue(dialogue));
        }

        private IEnumerator ManageDialogue(ScriptableDialogue dialogue)
        {
            (FlowGraphNodeData, NodeData) entry = dialogue.GetNodeEntry();
            NodeData currentNode = entry.Item2;

            while (true)
            {
                if (currentNode is TextNode textNode)
                {
                    SetNode(textNode);

                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

                    if (!textNode.OutPort.Port.IsValid()) break;

                    currentNode = dialogue.GetNodeData(textNode.OutPort.Port.TargetNode);

                    yield return null;
                }
                else if (currentNode is BranchNode branchNode)
                {
                    // Gather valid paths
                    break;
                }
                else break;
            }

            SetState(false);
        }

        private void SetState(bool state)
        {
            m_Active = state;
            DialogueContainer.SetActive(m_Active);
        }

        private void SetNode(TextNode textNode)
        {
            if (m_CharacterDictionary.TryGetValue(textNode.CharacterGuid.Value, out Character character) && character != null)
            {
                CharacterName.color = character.NameColor;
                CharacterName.text = character.Name;
            }
            else
            {
                CharacterName.color = Color.gray;
                CharacterName.text = "(...)";
            }
            
            DialogueText.text = textNode.Text.Value;
        }


#if UNITY_EDITOR
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            ResolveCharacters();
        }
#endif
    }
}