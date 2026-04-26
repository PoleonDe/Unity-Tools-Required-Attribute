#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Control.Tools.Required.Editor
{
    internal sealed class RequiredIssuesPopup : EditorWindow
    {
        private const float RowHeight = 44f;
        private const float HeaderHeight = 34f;
        private const float FooterHeight = 30f;
        private const int VisibleRows = 10;

        private Vector2 scroll;

        public static void Show(Rect activatorRect)
        {
            RequiredIssuesPopup window = CreateInstance<RequiredIssuesPopup>();
            Vector2 size = new Vector2(430f, HeaderHeight + FooterHeight + RowHeight * VisibleRows);
            window.ShowAsDropDown(activatorRect, size);
            window.minSize = size;
            window.maxSize = size;
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            RequiredScanner.IssuesChanged += Repaint;
        }

        private void OnDisable()
        {
            RequiredScanner.IssuesChanged -= Repaint;
        }

        private void OnGUI()
        {
            if (Event.current != null && Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }

            IReadOnlyList<RequiredIssue> issues = RequiredScanner.Issues;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(HeaderHeight)))
            {
                GUILayout.Label(issues.Count == 0 ? "Required fields complete" : "Missing required fields", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    RequiredScanner.RefreshNow();
                }
            }

            Rect listRect = GUILayoutUtility.GetRect(position.width, RowHeight * VisibleRows);
            if (issues.Count == 0)
            {
                GUI.Label(listRect, "No missing Required fields.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                DrawIssues(listRect, issues);
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(FooterHeight)))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(issues.Count + " issue" + (issues.Count == 1 ? string.Empty : "s"), EditorStyles.miniLabel);
            }
        }

        private void DrawIssues(Rect listRect, IReadOnlyList<RequiredIssue> issues)
        {
            Rect contentRect = new Rect(0f, 0f, listRect.width - 16f, issues.Count * RowHeight);
            scroll = GUI.BeginScrollView(listRect, scroll, contentRect);

            for (int i = 0; i < issues.Count; i++)
            {
                Rect rowRect = new Rect(0f, i * RowHeight, contentRect.width, RowHeight);
                DrawIssueRow(rowRect, issues[i], i);
            }

            GUI.EndScrollView();
        }

        private void DrawIssueRow(Rect rowRect, RequiredIssue issue, int index)
        {
            Event current = Event.current;
            bool isHover = rowRect.Contains(current.mousePosition);
            if (current.type == EventType.Repaint && index % 2 == 1)
            {
                EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, EditorGUIUtility.isProSkin ? 0.035f : 0.12f));
            }

            if (current.type == EventType.Repaint && isHover)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.25f, 0.48f, 0.90f, EditorGUIUtility.isProSkin ? 0.22f : 0.16f));
            }

            Rect iconRect = new Rect(rowRect.x + 8f, rowRect.y + 14f, 16f, 16f);
            GUI.DrawTexture(iconRect, RequiredIconUtility.RedIcon, ScaleMode.ScaleToFit, true);

            Rect textRect = new Rect(rowRect.x + 32f, rowRect.y + 4f, rowRect.width - 40f, RowHeight - 8f);
            GUI.Label(
                new Rect(textRect.x, textRect.y, textRect.width, 18f),
                issue.OwnerName + "  -  " + issue.OwnerTypeName,
                EditorStyles.boldLabel);
            GUI.Label(
                new Rect(textRect.x, textRect.y + 18f, textRect.width, 16f),
                issue.FieldName + "  -  " + issue.Location,
                EditorStyles.miniLabel);

            EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);
            if (current.type == EventType.MouseDown && current.button == 0 && isHover)
            {
                RequiredEditorNavigation.OpenIssue(issue);
                current.Use();
                Close();
            }
        }
    }
}
#endif



