#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using UnityEditor;
using UnityEngine;

namespace Control.Tools.Required.Editor
{
    [InitializeOnLoad]
    internal static class RequiredHeaderIndicator
    {
        private const float IconSize = 16f;

        private static readonly Color TitleColor = new Color(0.95f, 0.18f, 0.14f, 1f);
        private static bool suppressFinishedHeaderCallback;

        static RequiredHeaderIndicator()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= OnFinishedDefaultHeaderGUI;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
        }

        private static void OnFinishedDefaultHeaderGUI(UnityEditor.Editor editor)
        {
            if (suppressFinishedHeaderCallback)
            {
                return;
            }

            if (editor == null || editor.targets == null || editor.targets.Length == 0)
            {
                return;
            }

            DrawHeaderWarning(editor);
        }

        public static void DrawHeaderWithWarning(UnityEditor.Editor editor, System.Action drawDefaultHeader)
        {
            suppressFinishedHeaderCallback = true;
            try
            {
                drawDefaultHeader?.Invoke();
            }
            finally
            {
                suppressFinishedHeaderCallback = false;
            }

            DrawHeaderWarning(editor);
        }

        private static void DrawHeaderWarning(UnityEditor.Editor editor)
        {
            if (editor == null || editor.targets == null || editor.targets.Length == 0)
            {
                return;
            }

            int issueCount = CountHeaderIssues(editor.targets);
            if (issueCount == 0)
            {
                return;
            }

            Rect headerRect = GUILayoutUtility.GetLastRect();
            if (headerRect.width <= 1f || headerRect.height <= 1f)
            {
                DrawFallbackHeaderIcon(issueCount);
                return;
            }

            DrawHeaderIcon(headerRect, issueCount);
            TryDrawRedHeaderTitle(headerRect, editor.target);
        }

        private static int CountHeaderIssues(Object[] targets)
        {
            int count = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is Component || targets[i] is ScriptableObject)
                {
                    count += RequiredScanner.CountMissingRequiredFields(targets[i]);
                }
            }

            return count;
        }

        private static void DrawHeaderIcon(Rect headerRect, int issueCount)
        {
            Rect iconRect = new Rect(
                headerRect.xMax - 54f,
                headerRect.y + Mathf.Max(2f, (headerRect.height - IconSize) * 0.5f),
                IconSize,
                IconSize);

            GUI.DrawTexture(iconRect, RequiredIconUtility.RedIcon, ScaleMode.ScaleToFit, true);
            GUI.Label(
                new Rect(iconRect.xMax + 3f, iconRect.y - 1f, 28f, IconSize + 2f),
                issueCount.ToString(),
                EditorStyles.miniBoldLabel);
        }

        private static void DrawFallbackHeaderIcon(int issueCount)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(RequiredIconUtility.RedIcon, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
                GUILayout.Label(issueCount.ToString(), EditorStyles.miniBoldLabel, GUILayout.Width(28f));
            }
        }

        private static void TryDrawRedHeaderTitle(Rect headerRect, Object target)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || target == null)
            {
                return;
            }

            string title = ObjectNames.NicifyVariableName(target.GetType().Name);
            Rect titleRect = new Rect(
                headerRect.x + 44f,
                headerRect.y + Mathf.Max(2f, (headerRect.height - EditorGUIUtility.singleLineHeight) * 0.5f),
                Mathf.Max(1f, headerRect.width - 128f),
                EditorGUIUtility.singleLineHeight);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = TitleColor },
                hover = { textColor = TitleColor },
                active = { textColor = TitleColor },
                focused = { textColor = TitleColor }
            };

            GUI.Label(titleRect, title, style);
        }
    }
}
#endif



