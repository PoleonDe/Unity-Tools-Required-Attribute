#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Control.Tools.Required.Editor
{
    internal static class RequiredEditorNavigation
    {
        private const double HighlightDuration = 1.6d;

        private static string highlightedKey;
        private static double highlightUntil;

        public static void OpenIssue(RequiredIssue issue)
        {
            if (issue == null || issue.SelectionObject == null)
            {
                return;
            }

            Selection.activeObject = issue.SelectionObject;
            EditorGUIUtility.PingObject(issue.SelectionObject);
            OpenInspector();

            highlightedKey = issue.StableKey;
            highlightUntil = EditorApplication.timeSinceStartup + HighlightDuration;
            ActiveEditorTracker.sharedTracker.ForceRebuild();
            InternalEditorUtility.RepaintAllViews();
        }

        public static bool IsHighlightTarget(SerializedProperty property)
        {
            if (string.IsNullOrEmpty(highlightedKey) || property == null || property.serializedObject == null)
            {
                return false;
            }

            if (EditorApplication.timeSinceStartup > highlightUntil)
            {
                highlightedKey = null;
                return false;
            }

            Object owner = property.serializedObject.targetObject;
            if (owner == null)
            {
                return false;
            }

            string currentKey = RequiredGlobalObjectIdUtility.CreateStableKey(owner, property.propertyPath);

            return string.Equals(currentKey, highlightedKey, StringComparison.Ordinal);
        }

        private static void OpenInspector()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
    }
}
#endif



