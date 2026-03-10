using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RuneUnlock))]
public class SkillUnlockDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        SerializedProperty typeProp = property.FindPropertyRelative("type");
        SerializedProperty skillProp = property.FindPropertyRelative("unlockedRune");
        SerializedProperty statTypeProp = property.FindPropertyRelative("statType");
        SerializedProperty statValueProp = property.FindPropertyRelative("statValue");
        
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        Rect typeRect = new Rect(position.x, position.y, position.width, lineHeight);
        EditorGUI.PropertyField(typeRect, typeProp);
        
        RuneType type = (RuneType)typeProp.enumValueIndex;
        
        float yOffset = position.y + lineHeight + spacing;
        
        EditorGUI.indentLevel++;
        
        switch (type)
        {
            case RuneType.Skill:
                Rect skillRect = new Rect(position.x, yOffset, position.width, lineHeight);
                EditorGUI.PropertyField(skillRect, skillProp, new GUIContent("Rune"));
                break;
                
            case RuneType.Stat:
                Rect statTypeRect = new Rect(position.x, yOffset, position.width, lineHeight);
                EditorGUI.PropertyField(statTypeRect, statTypeProp, new GUIContent("Stat Type"));
                
                Rect statValueRect = new Rect(position.x, yOffset + lineHeight + spacing, position.width, lineHeight);
                EditorGUI.PropertyField(statValueRect, statValueProp, new GUIContent("Stat Value"));
                break;
        }
        
        EditorGUI.indentLevel--;
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty typeProp = property.FindPropertyRelative("type");
        RuneType type = (RuneType)typeProp.enumValueIndex;
        
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        float height = lineHeight + spacing;
        
        switch (type)
        {
            case RuneType.Skill:
                height += lineHeight + spacing;
                break;
                
            case RuneType.Stat:
                height += (lineHeight + spacing) * 2;
                break;
        }
        
        return height;
    }
}
