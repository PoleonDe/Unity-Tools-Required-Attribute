#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using UnityEditor;
using UnityEngine;

namespace Control.Tools.Required.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    internal sealed class RequiredMonoBehaviourEditor : UnityEditor.Editor
    {
        protected override void OnHeaderGUI()
        {
            RequiredHeaderIndicator.DrawHeaderWithWarning(this, base.OnHeaderGUI);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
    internal sealed class RequiredScriptableObjectEditor : UnityEditor.Editor
    {
        protected override void OnHeaderGUI()
        {
            RequiredHeaderIndicator.DrawHeaderWithWarning(this, base.OnHeaderGUI);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}
#endif



