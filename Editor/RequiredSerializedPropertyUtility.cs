#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Control.Tools.Required.Editor
{
    internal static class RequiredSerializedPropertyUtility
    {
        public static bool TryGetRequiredField(SerializedProperty property, out FieldInfo fieldInfo)
        {
            fieldInfo = null;
            if (property == null || property.serializedObject == null || property.serializedObject.targetObject == null)
            {
                return false;
            }

            if (IsArrayElementItself(property.propertyPath) || property.propertyPath.EndsWith(".Array.size", StringComparison.Ordinal))
            {
                return false;
            }

            Type hostType = property.serializedObject.targetObject.GetType();
            fieldInfo = GetFieldInfo(hostType, property.propertyPath);
            return fieldInfo != null && Attribute.IsDefined(fieldInfo, typeof(global::Control.Tools.RequiredAttribute), true);
        }

        public static string GetDisplayName(SerializedProperty property, FieldInfo fieldInfo)
        {
            if (property == null)
            {
                return "Field";
            }

            string name = fieldInfo != null ? fieldInfo.Name : property.name;
            if (name.StartsWith("<", StringComparison.Ordinal) && name.Contains(">"))
            {
                int closing = name.IndexOf('>');
                name = closing > 1 ? name.Substring(1, closing - 1) : property.displayName;
            }

            return ObjectNames.NicifyVariableName(name);
        }

        public static object GetValue(UnityEngine.Object targetObject, string propertyPath)
        {
            if (targetObject == null || string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            object current = targetObject;
            string[] parts = NormalizeArrayPath(propertyPath).Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                if (current == null)
                {
                    return null;
                }

                string part = parts[i];
                int bracket = part.IndexOf('[');
                string fieldName = bracket >= 0 ? part.Substring(0, bracket) : part;
                FieldInfo field = GetFieldInHierarchy(current.GetType(), fieldName);
                if (field == null)
                {
                    return null;
                }

                current = field.GetValue(current);
                if (bracket >= 0)
                {
                    int endBracket = part.IndexOf(']', bracket + 1);
                    if (endBracket <= bracket + 1 || !int.TryParse(part.Substring(bracket + 1, endBracket - bracket - 1), out int index))
                    {
                        return null;
                    }

                    current = GetIndexedValue(current, index);
                }
            }

            return current;
        }

        private static FieldInfo GetFieldInfo(Type hostType, string propertyPath)
        {
            if (hostType == null || string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            Type currentType = hostType;
            FieldInfo field = null;
            string[] parts = NormalizeArrayPath(propertyPath).Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                int bracket = part.IndexOf('[');
                string fieldName = bracket >= 0 ? part.Substring(0, bracket) : part;
                if (string.IsNullOrEmpty(fieldName))
                {
                    continue;
                }

                field = GetFieldInHierarchy(currentType, fieldName);
                if (field == null)
                {
                    return null;
                }

                currentType = field.FieldType;
                if (bracket >= 0)
                {
                    currentType = GetElementType(currentType) ?? currentType;
                }
            }

            return field;
        }

        private static FieldInfo GetFieldInHierarchy(Type type, string fieldName)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private static Type GetElementType(Type type)
        {
            if (type == null || type == typeof(string))
            {
                return null;
            }

            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType && typeof(IList).IsAssignableFrom(type))
            {
                Type[] arguments = type.GetGenericArguments();
                return arguments.Length == 1 ? arguments[0] : null;
            }

            return null;
        }

        private static object GetIndexedValue(object collection, int index)
        {
            if (collection == null || index < 0)
            {
                return null;
            }

            if (collection is IList list)
            {
                return index < list.Count ? list[index] : null;
            }

            if (collection is IEnumerable enumerable)
            {
                int currentIndex = 0;
                foreach (object value in enumerable)
                {
                    if (currentIndex == index)
                    {
                        return value;
                    }

                    currentIndex++;
                }
            }

            return null;
        }

        private static string NormalizeArrayPath(string propertyPath)
        {
            return propertyPath.Replace(".Array.data[", "[");
        }

        private static bool IsArrayElementItself(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                return false;
            }

            int dataIndex = propertyPath.LastIndexOf(".Array.data[", StringComparison.Ordinal);
            return dataIndex >= 0 && propertyPath.EndsWith("]", StringComparison.Ordinal);
        }
    }
}
#endif



