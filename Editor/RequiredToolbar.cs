#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Control.Tools.Required.Editor
{
    [InitializeOnLoad]
    internal static class RequiredToolbar
    {
        private const string MainToolbarPath = "Control/GlobalMissingRequired";

        static RequiredToolbar()
        {
            RequiredScanner.IssuesChanged -= RefreshToolbar;
            RequiredScanner.IssuesChanged += RefreshToolbar;
            EditorApplication.delayCall += RefreshToolbar;
        }

        [MainToolbarElement(
            MainToolbarPath,
            defaultDockPosition = MainToolbarDockPosition.Middle,
            defaultDockIndex = 3)]
        private static MainToolbarDropdown CreateToolbarElement()
        {
            return new MainToolbarDropdown(CreateContent(), ShowPopup);
        }

        private static MainToolbarContent CreateContent()
        {
            bool hasIssues = RequiredScanner.HasIssues;
            int issueCount = RequiredScanner.IssueCount;
            string tooltip = hasIssues
                ? issueCount + " missing Required field" + (issueCount == 1 ? string.Empty : "s")
                : "No missing Required fields";

            return new MainToolbarContent(
                hasIssues ? issueCount.ToString() : string.Empty,
                hasIssues ? RequiredIconUtility.RedIcon : RequiredIconUtility.GreenIcon,
                tooltip);
        }

        private static void RefreshToolbar()
        {
            MainToolbar.Refresh(MainToolbarPath);
        }

        private static void ShowPopup(Rect activatorRect)
        {
            RequiredIssuesPopup.Show(activatorRect);
        }
    }
}
#elif UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Control.Tools.Required.Editor
{
    [Overlay(typeof(SceneView), "Required", true)]
    [Icon("Packages/com.control-tools.required-attribute/Editor/Icons/Warning.svg")]
    public sealed class RequiredToolbarOverlay : ToolbarOverlay
    {
        public RequiredToolbarOverlay()
            : base(RequiredToolbarButton.Id)
        {
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    public sealed class RequiredToolbarButton : EditorToolbarButton
    {
        public const string Id = "Control/GlobalMissingRequired";

        public RequiredToolbarButton()
        {
            name = "GlobalMissingRequired";
            clicked += ShowPopup;
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                RequiredScanner.IssuesChanged -= UpdateState;
                RequiredScanner.IssuesChanged += UpdateState;
                UpdateState();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                RequiredScanner.IssuesChanged -= UpdateState;
            });

            UpdateState();
        }

        private void UpdateState()
        {
            bool hasIssues = RequiredScanner.HasIssues;
            icon = hasIssues ? RequiredIconUtility.RedIcon : RequiredIconUtility.GreenIcon;
            text = hasIssues ? RequiredScanner.IssueCount.ToString() : string.Empty;
            tooltip = hasIssues
                ? RequiredScanner.IssueCount + " missing Required field" + (RequiredScanner.IssueCount == 1 ? string.Empty : "s")
                : "No missing Required fields";
        }

        private void ShowPopup()
        {
            EditorWindow host = EditorWindow.mouseOverWindow;
            Rect bounds = worldBound;
            Vector2 screenPosition = host != null
                ? new Vector2(host.position.x + bounds.x, host.position.y + bounds.yMax)
                : GUIUtility.GUIToScreenPoint(Event.current != null ? Event.current.mousePosition : Vector2.zero);

            RequiredIssuesPopup.Show(new Rect(screenPosition.x, screenPosition.y, resolvedStyle.width, resolvedStyle.height));
        }
    }
}
#elif UNITY_EDITOR
namespace Control.Tools.Required.Editor
{
    internal static class RequiredToolbar
    {
        // The Control Required editor toolbar integration is intentionally Unity 6+ only.
    }
}
#endif



