#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Control.Tools.Required.Editor
{
    public static class RequiredValidator
    {
        public static bool IsMissingValue(object value)
        {
            if (value == null)
            {
                return true;
            }

            if (value is UnityEngine.Object unityObject)
            {
                return unityObject == null;
            }

            if (value is string text)
            {
                return string.IsNullOrEmpty(text);
            }

            Type valueType = value.GetType();
            Type nullableType = Nullable.GetUnderlyingType(valueType);
            if (nullableType != null)
            {
                return false;
            }

            if (value is ICollection collection)
            {
                return collection.Count == 0;
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                IEnumerator enumerator = enumerable.GetEnumerator();
                try
                {
                    return !enumerator.MoveNext();
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            }

            return false;
        }

        public static bool IsMissingProperty(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue == null;
                case SerializedPropertyType.String:
                    return string.IsNullOrEmpty(property.stringValue);
                case SerializedPropertyType.ManagedReference:
                    return property.managedReferenceValue == null;
                case SerializedPropertyType.ExposedReference:
                    return property.exposedReferenceValue == null;
            }

            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                return property.arraySize == 0;
            }

            return false;
        }
    }
}
#endif



