#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Control.Tools.Required.Editor
{
    [InitializeOnLoad]
    internal static class RequiredScanner
    {
        private static readonly List<RequiredIssue> issues = new List<RequiredIssue>();
        private static bool refreshQueued;
        private static bool queuedRefreshIncludesAssets;
        private static double nextRefreshTime;

        public static event Action IssuesChanged;

        static RequiredScanner()
        {
            EditorApplication.delayCall += () => RequestRefresh(true);
            EditorApplication.hierarchyChanged += () => RequestRefresh(false);
            EditorSceneManager.sceneOpened += (_, __) => RequestRefresh(false);
            EditorSceneManager.sceneClosed += _ => RequestRefresh(false);
            EditorSceneManager.sceneSaved += _ => RequestRefresh(false);
            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => RequestRefresh(false);
            Undo.undoRedoPerformed += () => RequestRefresh(false);
            PrefabStage.prefabStageOpened += _ => RequestRefresh(false);
            PrefabStage.prefabStageClosing += _ => RequestRefresh(false);
        }

        public static IReadOnlyList<RequiredIssue> Issues => issues;
        public static int IssueCount => issues.Count;
        public static bool HasIssues => issues.Count > 0;

        public static int CountIssuesForOwner(Object ownerObject)
        {
            if (ownerObject == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].OwnerObject == ownerObject)
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountMissingRequiredFields(Object ownerObject)
        {
            if (ownerObject == null)
            {
                return 0;
            }

            SerializedObject serializedObject;
            try
            {
                serializedObject = new SerializedObject(ownerObject);
            }
            catch
            {
                return 0;
            }

            int count = 0;
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (iterator.propertyPath == "m_Script")
                {
                    continue;
                }

                SerializedProperty property = iterator.Copy();
                if (!RequiredSerializedPropertyUtility.TryGetRequiredField(property, out _))
                {
                    continue;
                }

                if (IsPropertyMissing(ownerObject, property))
                {
                    count++;
                }
            }

            return count;
        }

        [MenuItem("Tools/Control/Required/Refresh Issues")]
        private static void RefreshFromMenu()
        {
            RefreshNow(true);
        }

        public static void RequestRefresh()
        {
            RequestRefresh(true);
        }

        public static void RequestRefresh(bool includeAssets)
        {
            queuedRefreshIncludesAssets |= includeAssets;
            nextRefreshTime = EditorApplication.timeSinceStartup + 0.2d;
            if (!refreshQueued)
            {
                EditorApplication.update += RefreshWhenReady;
            }

            refreshQueued = true;
        }

        public static void RefreshNow()
        {
            RefreshNow(true);
        }

        public static void RefreshNow(bool includeAssets)
        {
            refreshQueued = false;
            queuedRefreshIncludesAssets = false;
            EditorApplication.update -= RefreshWhenReady;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                RequestRefresh(includeAssets);
                return;
            }

            List<RequiredIssue> preservedAssetIssues = includeAssets ? null : GetPreservedAssetIssues();
            issues.Clear();
            if (preservedAssetIssues != null)
            {
                issues.AddRange(preservedAssetIssues);
            }

            ScanLoadedScenes();
            ScanPrefabStage();
            if (includeAssets)
            {
                ScanAssets();
            }

            IssuesChanged?.Invoke();
            InternalEditorUtility.RepaintAllViews();
        }

        private static void RefreshWhenReady()
        {
            if (!refreshQueued || EditorApplication.timeSinceStartup < nextRefreshTime)
            {
                return;
            }

            bool includeAssets = queuedRefreshIncludesAssets;
            RefreshNow(includeAssets);
        }

        private static List<RequiredIssue> GetPreservedAssetIssues()
        {
            string prefabStagePath = GetCurrentPrefabStagePath();
            List<RequiredIssue> preserved = new List<RequiredIssue>();
            for (int i = 0; i < issues.Count; i++)
            {
                RequiredIssue issue = issues[i];
                if (string.IsNullOrEmpty(issue.AssetPath))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(prefabStagePath) && issue.AssetPath == prefabStagePath)
                {
                    continue;
                }

                preserved.Add(issue);
            }

            return preserved;
        }

        private static string GetCurrentPrefabStagePath()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null ? stage.assetPath : string.Empty;
        }

        private static void ScanLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    ScanGameObjectHierarchy(roots[rootIndex], scene.name, null);
                }
            }
        }

        private static void ScanPrefabStage()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || stage.prefabContentsRoot == null)
            {
                return;
            }

            ScanGameObjectHierarchy(stage.prefabContentsRoot, string.Empty, stage.assetPath);
        }

        private static void ScanAssets()
        {
            ScanPrefabAssets();
            ScanScriptableObjectAssets();
        }

        private static void ScanPrefabAssets()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    ScanGameObjectHierarchy(prefab, string.Empty, assetPath);
                }
            }
        }

        private static void ScanScriptableObjectAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                Object[] objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                for (int objectIndex = 0; objectIndex < objects.Length; objectIndex++)
                {
                    if (objects[objectIndex] is ScriptableObject scriptableObject)
                    {
                        ScanObject(scriptableObject, scriptableObject, string.Empty, assetPath);
                    }
                }
            }
        }

        private static void ScanGameObjectHierarchy(GameObject root, string sceneName, string assetPath)
        {
            if (root == null)
            {
                return;
            }

            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    continue;
                }

                Object selectionObject = string.IsNullOrEmpty(assetPath) ? component.gameObject : (Object)root;
                ScanObject(component, selectionObject, sceneName, assetPath);
            }
        }

        private static void ScanObject(Object ownerObject, Object selectionObject, string sceneName, string assetPath)
        {
            if (ownerObject == null)
            {
                return;
            }

            SerializedObject serializedObject;
            try
            {
                serializedObject = new SerializedObject(ownerObject);
            }
            catch
            {
                return;
            }

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (iterator.propertyPath == "m_Script")
                {
                    continue;
                }

                SerializedProperty property = iterator.Copy();
                if (!RequiredSerializedPropertyUtility.TryGetRequiredField(property, out System.Reflection.FieldInfo fieldInfo))
                {
                    continue;
                }

                if (!IsPropertyMissing(ownerObject, property))
                {
                    continue;
                }

                string ownerName = GetOwnerName(ownerObject);
                string ownerTypeName = ownerObject.GetType().Name;
                string fieldName = RequiredSerializedPropertyUtility.GetDisplayName(property, fieldInfo);
                issues.Add(new RequiredIssue(
                    ownerObject,
                    selectionObject ?? ownerObject,
                    ownerName,
                    ownerTypeName,
                    property.propertyPath,
                    fieldName,
                    sceneName,
                    assetPath));
            }
        }

        private static bool IsPropertyMissing(Object ownerObject, SerializedProperty property)
        {
            if (RequiredValidator.IsMissingProperty(property))
            {
                return true;
            }

            object value = RequiredSerializedPropertyUtility.GetValue(ownerObject, property.propertyPath);
            return RequiredValidator.IsMissingValue(value);
        }

        private static string GetOwnerName(Object ownerObject)
        {
            if (ownerObject is Component component && component.gameObject != null)
            {
                return component.gameObject.name;
            }

            return ownerObject != null ? ownerObject.name : "Missing Object";
        }
    }
}
#endif



