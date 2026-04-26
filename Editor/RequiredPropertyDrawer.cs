#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using UnityEditor;
using UnityEngine;

namespace Control.Tools.Required.Editor
{
    [CustomPropertyDrawer(typeof(global::Control.Tools.RequiredAttribute))]
    internal sealed class RequiredPropertyDrawer : PropertyDrawer
    {
        private const float IconSize = 16f;
        private const float IconPadding = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool missing = IsMissing(property);
            bool highlighted = RequiredEditorNavigation.IsHighlightTarget(property);

            if (highlighted && Event.current.type == EventType.Repaint)
            {
                Color highlight = new Color(1f, 0.72f, 0.18f, 0.24f);
                EditorGUI.DrawRect(position, highlight);
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect fieldRect = position;
            if (missing)
            {
                Rect indented = EditorGUI.IndentedRect(position);
                Rect iconRect = new Rect(
                    indented.x,
                    position.y + Mathf.Max(0f, (EditorGUIUtility.singleLineHeight - IconSize) * 0.5f),
                    IconSize,
                    IconSize);

                GUI.DrawTexture(iconRect, RequiredIconUtility.RedIcon, ScaleMode.ScaleToFit, true);
                fieldRect.xMin += IconSize + IconPadding;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(fieldRect, property, label, true);
            if (EditorGUI.EndChangeCheck())
            {
                RequiredScanner.RequestRefresh(false);
            }

            EditorGUI.EndProperty();
        }

        private static bool IsMissing(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            if (property.hasMultipleDifferentValues)
            {
                Object[] targets = property.serializedObject.targetObjects;
                for (int i = 0; i < targets.Length; i++)
                {
                    object value = RequiredSerializedPropertyUtility.GetValue(targets[i], property.propertyPath);
                    if (RequiredValidator.IsMissingValue(value))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (RequiredValidator.IsMissingProperty(property))
            {
                return true;
            }

            object singleValue = RequiredSerializedPropertyUtility.GetValue(
                property.serializedObject.targetObject,
                property.propertyPath);
            return RequiredValidator.IsMissingValue(singleValue);
        }
    }
}
#endif



