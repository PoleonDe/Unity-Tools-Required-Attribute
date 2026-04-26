#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Control.Tools.Required.Editor
{
    internal static class RequiredGlobalObjectIdUtility
    {
        public static string CreateStableKey(Object ownerObject, string propertyPath)
        {
            if (ownerObject == null)
            {
                return propertyPath ?? string.Empty;
            }

            try
            {
                GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(ownerObject);
                if (!string.IsNullOrEmpty(globalId.ToString()))
                {
                    return globalId + "|" + propertyPath;
                }
            }
            catch
            {
                // Unsaved or transient editor objects can fail global id lookup.
            }

            return ownerObject.GetInstanceID() + "|" + propertyPath;
        }
    }
}
#endif



