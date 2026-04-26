#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using UnityEditor;
using UnityEngine;

namespace Control.Tools.Required.Editor
{
    internal sealed class RequiredIssue
    {
        public RequiredIssue(
            Object ownerObject,
            Object selectionObject,
            string ownerName,
            string ownerTypeName,
            string propertyPath,
            string fieldName,
            string sceneName,
            string assetPath)
        {
            OwnerObject = ownerObject;
            SelectionObject = selectionObject;
            OwnerName = ownerName;
            OwnerTypeName = ownerTypeName;
            PropertyPath = propertyPath;
            FieldName = fieldName;
            SceneName = sceneName;
            AssetPath = assetPath;
            StableKey = CreateStableKey(ownerObject, propertyPath);
        }

        public Object OwnerObject { get; }
        public Object SelectionObject { get; }
        public string OwnerName { get; }
        public string OwnerTypeName { get; }
        public string PropertyPath { get; }
        public string FieldName { get; }
        public string SceneName { get; }
        public string AssetPath { get; }
        public string StableKey { get; }

        public string Location
        {
            get
            {
                if (!string.IsNullOrEmpty(SceneName))
                {
                    return SceneName;
                }

                return string.IsNullOrEmpty(AssetPath) ? "Unsaved" : AssetPath;
            }
        }

        private static string CreateStableKey(Object ownerObject, string propertyPath)
        {
            return RequiredGlobalObjectIdUtility.CreateStableKey(ownerObject, propertyPath);
        }
    }
}
#endif



