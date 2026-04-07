using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NPCStateDebugIndicator : MonoBehaviour
{
    [SerializeField] private NPCStateMachine stateMachine;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private Color labelColor = Color.white;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        stateMachine = GetComponent<NPCStateMachine>();

        GUIStyle style = new GUIStyle();
        style.normal.textColor = labelColor;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;

        Vector3 labelPosition = transform.position + worldOffset;
        Handles.Label(labelPosition, stateMachine.CurrentStateName, style);
    }
#endif
}