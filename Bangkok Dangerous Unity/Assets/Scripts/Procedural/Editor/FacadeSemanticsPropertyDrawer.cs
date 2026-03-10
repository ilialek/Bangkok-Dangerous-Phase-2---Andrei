using UnityEditor;
using UnityEngine;
using Procedural;

namespace ProceduralEditor
{
    [CustomPropertyDrawer(typeof(FacadeSemantic))]
    public class FacadeSemanticsPropertyDrawer : PropertyDrawer
    {
        private bool foldout = false;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new Rect(position.x, position.y, 15f, EditorGUIUtility.singleLineHeight);
            Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            foldout = EditorGUI.Foldout(foldoutRect, foldout, GUIContent.none, true);
            EditorGUI.PropertyField(fieldRect, property, label);

            if (foldout && property.objectReferenceValue != null)
            {
                FacadeSemantic target = property.objectReferenceValue as FacadeSemantic;
                if (target != null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginChangeCheck();

                    SerializedObject serializedObject = new SerializedObject(target);
                    SerializedProperty iterator = serializedObject.GetIterator();
                    iterator.NextVisible(true);

                    float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    while (iterator.NextVisible(false))
                    {
                        float height = EditorGUI.GetPropertyHeight(iterator, true);
                        Rect field = new Rect(position.x, y, position.width, height);
                        EditorGUI.PropertyField(field, iterator, true);
                        y += height + EditorGUIUtility.standardVerticalSpacing;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!foldout || property.objectReferenceValue == null) return EditorGUIUtility.singleLineHeight;

            FacadeSemantic target = property.objectReferenceValue as FacadeSemantic;
            if (target == null) return EditorGUIUtility.singleLineHeight;

            SerializedObject so = new SerializedObject(target);
            SerializedProperty iterator = so.GetIterator();
            float totalHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            iterator.NextVisible(true);
            while (iterator.NextVisible(false))
            {
                totalHeight += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            return totalHeight;
        }
    }
}