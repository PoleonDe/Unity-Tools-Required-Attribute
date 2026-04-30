#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
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
        private static readonly HashSet<string> queuedAssetPathsToScan = new HashSet<string>();
        private static readonly HashSet<string> queuedAssetFoldersToScan = new HashSet<string>();
        private static readonly HashSet<string> queuedAssetPathsToRemove = new HashSet<string>();
        private static readonly HashSet<string> queuedAssetFoldersToRemove = new HashSet<string>();
        private static bool refreshQueued;
        private static bool queuedSceneRefresh;
        private static bool queuedFullAssetRefresh;
        private static double nextRefreshTime;

        public static event Action IssuesChanged;

        static RequiredScanner()
        {
            EditorApplication.hierarchyChanged += () => RequestRefresh(false);
            Undo.undoRedoPerformed += () => RequestRefresh(false);
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
            if (IsRefreshBlocked())
            {
                return;
            }

            queuedSceneRefresh = true;
            if (includeAssets)
            {
                queuedFullAssetRefresh = true;
                ClearTargetedAssetRefreshQueue();
            }

            QueueRefresh();
        }

        public static void RequestAssetRefresh(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (IsRefreshBlocked())
            {
                return;
            }

            if (queuedFullAssetRefresh)
            {
                QueueRefresh();
                return;
            }

            bool hasQueuedChanges = false;
            hasQueuedChanges |= QueueImportedAssetPaths(importedAssets);
            hasQueuedChanges |= QueueDeletedAssetPaths(deletedAssets);
            hasQueuedChanges |= QueueMovedAssetPaths(movedAssets, movedFromAssetPaths);

            if (hasQueuedChanges)
            {
                QueueRefresh();
            }
        }

        public static void RefreshNow()
        {
            RefreshNow(true);
        }

        public static void RefreshNow(bool includeAssets)
        {
            ClearRefreshQueue();
            if (IsRefreshBlocked())
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                RequestRefresh(includeAssets);
                return;
            }

            Refresh(true, includeAssets, null, null, null, null);
        }

        private static void RefreshWhenReady()
        {
            if (!refreshQueued || EditorApplication.timeSinceStartup < nextRefreshTime)
            {
                return;
            }

            if (IsRefreshBlocked())
            {
                ClearRefreshQueue();
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                nextRefreshTime = EditorApplication.timeSinceStartup + 0.5d;
                return;
            }

            bool scanScenes = queuedSceneRefresh;
            bool fullAssetRefresh = queuedFullAssetRefresh;
            List<string> assetPathsToScan = new List<string>(queuedAssetPathsToScan);
            List<string> assetFoldersToScan = new List<string>(queuedAssetFoldersToScan);
            List<string> assetPathsToRemove = new List<string>(queuedAssetPathsToRemove);
            List<string> assetFoldersToRemove = new List<string>(queuedAssetFoldersToRemove);

            ClearRefreshQueue();
            Refresh(
                scanScenes,
                fullAssetRefresh,
                assetPathsToScan,
                assetFoldersToScan,
                assetPathsToRemove,
                assetFoldersToRemove);
        }

        private static void QueueRefresh()
        {
            nextRefreshTime = EditorApplication.timeSinceStartup + 0.2d;
            if (!refreshQueued)
            {
                EditorApplication.update += RefreshWhenReady;
            }

            refreshQueued = true;
        }

        private static void ClearRefreshQueue()
        {
            refreshQueued = false;
            queuedSceneRefresh = false;
            queuedFullAssetRefresh = false;
            ClearTargetedAssetRefreshQueue();
            EditorApplication.update -= RefreshWhenReady;
        }

        private static void ClearTargetedAssetRefreshQueue()
        {
            queuedAssetPathsToScan.Clear();
            queuedAssetFoldersToScan.Clear();
            queuedAssetPathsToRemove.Clear();
            queuedAssetFoldersToRemove.Clear();
        }

        private static void Refresh(
            bool scanScenes,
            bool fullAssetRefresh,
            IReadOnlyCollection<string> assetPathsToScan,
            IReadOnlyCollection<string> assetFoldersToScan,
            IReadOnlyCollection<string> assetPathsToRemove,
            IReadOnlyCollection<string> assetFoldersToRemove)
        {
            List<RequiredIssue> preservedIssues = GetPreservedIssues(
                scanScenes,
                fullAssetRefresh,
                assetPathsToScan,
                assetFoldersToScan,
                assetPathsToRemove,
                assetFoldersToRemove);

            issues.Clear();
            issues.AddRange(preservedIssues);

            if (scanScenes)
            {
                ScanActiveScene();
            }

            if (fullAssetRefresh)
            {
                ScanAssets();
            }
            else
            {
                ScanAssets(assetPathsToScan, assetFoldersToScan);
            }

            IssuesChanged?.Invoke();
            InternalEditorUtility.RepaintAllViews();
        }

        private static List<RequiredIssue> GetPreservedIssues(
            bool scanScenes,
            bool fullAssetRefresh,
            IReadOnlyCollection<string> assetPathsToScan,
            IReadOnlyCollection<string> assetFoldersToScan,
            IReadOnlyCollection<string> assetPathsToRemove,
            IReadOnlyCollection<string> assetFoldersToRemove)
        {
            List<RequiredIssue> preserved = new List<RequiredIssue>();
            for (int i = 0; i < issues.Count; i++)
            {
                RequiredIssue issue = issues[i];
                if (string.IsNullOrEmpty(issue.AssetPath))
                {
                    if (!scanScenes)
                    {
                        preserved.Add(issue);
                    }

                    continue;
                }

                if (fullAssetRefresh)
                {
                    continue;
                }

                if (ContainsPath(assetPathsToScan, issue.AssetPath)
                    || ContainsPath(assetPathsToRemove, issue.AssetPath)
                    || IsPathInAnyFolder(issue.AssetPath, assetFoldersToScan)
                    || IsPathInAnyFolder(issue.AssetPath, assetFoldersToRemove))
                {
                    continue;
                }

                preserved.Add(issue);
            }

            return preserved;
        }

        private static void ScanActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                ScanGameObjectHierarchy(roots[rootIndex], scene.name, null);
            }
        }

        private static void ScanAssets()
        {
            ScanPrefabAssets();
            ScanScriptableObjectAssets();
        }

        private static void ScanAssets(IReadOnlyCollection<string> assetPaths, IReadOnlyCollection<string> assetFolders)
        {
            if ((assetPaths == null || assetPaths.Count == 0) && (assetFolders == null || assetFolders.Count == 0))
            {
                return;
            }

            HashSet<string> pathsToScan = new HashSet<string>();
            if (assetPaths != null)
            {
                foreach (string assetPath in assetPaths)
                {
                    if (IsInspectableAssetPath(assetPath))
                    {
                        pathsToScan.Add(assetPath);
                    }
                }
            }

            if (assetFolders != null)
            {
                foreach (string folderPath in assetFolders)
                {
                    AddAssetsInFolder(pathsToScan, folderPath, "t:Prefab");
                    AddAssetsInFolder(pathsToScan, folderPath, "t:ScriptableObject");
                }
            }

            foreach (string assetPath in pathsToScan)
            {
                ScanAsset(assetPath);
            }
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

        private static void ScanAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                ScanGameObjectHierarchy(prefab, string.Empty, assetPath);
            }

            Object[] objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int objectIndex = 0; objectIndex < objects.Length; objectIndex++)
            {
                if (objects[objectIndex] is ScriptableObject scriptableObject)
                {
                    ScanObject(scriptableObject, scriptableObject, string.Empty, assetPath);
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

        private static bool QueueImportedAssetPaths(string[] importedAssets)
        {
            bool hasQueuedChanges = false;
            if (importedAssets == null)
            {
                return false;
            }

            for (int i = 0; i < importedAssets.Length; i++)
            {
                string assetPath = importedAssets[i];
                if (!IsInspectableAssetPath(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                queuedAssetPathsToScan.Add(assetPath);
                hasQueuedChanges = true;
            }

            return hasQueuedChanges;
        }

        private static bool QueueDeletedAssetPaths(string[] deletedAssets)
        {
            bool hasQueuedChanges = false;
            if (deletedAssets == null)
            {
                return false;
            }

            for (int i = 0; i < deletedAssets.Length; i++)
            {
                string assetPath = deletedAssets[i];
                if (!string.IsNullOrEmpty(assetPath))
                {
                    queuedSceneRefresh = true;
                    hasQueuedChanges = true;
                }

                if (IsInspectableAssetPath(assetPath))
                {
                    queuedAssetPathsToRemove.Add(assetPath);
                    hasQueuedChanges = true;
                }
                else if (IsLikelyFolderPath(assetPath))
                {
                    queuedAssetFoldersToRemove.Add(assetPath);
                    hasQueuedChanges = true;
                }
            }

            return hasQueuedChanges;
        }

        private static bool IsRefreshBlocked()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static bool QueueMovedAssetPaths(string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool hasQueuedChanges = false;
            if (movedAssets == null)
            {
                return false;
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                string movedAssetPath = movedAssets[i];
                string movedFromAssetPath = movedFromAssetPaths != null && i < movedFromAssetPaths.Length
                    ? movedFromAssetPaths[i]
                    : string.Empty;

                if (AssetDatabase.IsValidFolder(movedAssetPath))
                {
                    queuedAssetFoldersToScan.Add(movedAssetPath);
                    if (!string.IsNullOrEmpty(movedFromAssetPath))
                    {
                        queuedAssetFoldersToRemove.Add(movedFromAssetPath);
                    }

                    hasQueuedChanges = true;
                    continue;
                }

                if (!IsInspectableAssetPath(movedAssetPath) && !IsInspectableAssetPath(movedFromAssetPath))
                {
                    continue;
                }

                if (IsInspectableAssetPath(movedAssetPath))
                {
                    queuedAssetPathsToScan.Add(movedAssetPath);
                }

                if (IsInspectableAssetPath(movedFromAssetPath))
                {
                    queuedAssetPathsToRemove.Add(movedFromAssetPath);
                }

                hasQueuedChanges = true;
            }

            return hasQueuedChanges;
        }

        private static void AddAssetsInFolder(HashSet<string> pathsToScan, string folderPath, string filter)
        {
            if (pathsToScan == null || string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets(filter, new[] { folderPath });
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (IsInspectableAssetPath(assetPath))
                {
                    pathsToScan.Add(assetPath);
                }
            }
        }

        private static bool IsInspectableAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string extension = Path.GetExtension(assetPath);
            return string.Equals(extension, ".prefab", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".asset", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLikelyFolderPath(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) && string.IsNullOrEmpty(Path.GetExtension(assetPath));
        }

        private static bool ContainsPath(IReadOnlyCollection<string> paths, string assetPath)
        {
            if (paths == null)
            {
                return false;
            }

            foreach (string path in paths)
            {
                if (string.Equals(path, assetPath, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPathInAnyFolder(string assetPath, IReadOnlyCollection<string> folderPaths)
        {
            if (string.IsNullOrEmpty(assetPath) || folderPaths == null)
            {
                return false;
            }

            foreach (string folderPath in folderPaths)
            {
                if (IsPathInFolder(assetPath, folderPath))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPathInFolder(string assetPath, string folderPath)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(folderPath))
            {
                return false;
            }

            string normalizedFolderPath = folderPath.TrimEnd('/');
            return string.Equals(assetPath, normalizedFolderPath, StringComparison.Ordinal)
                || assetPath.StartsWith(normalizedFolderPath + "/", StringComparison.Ordinal);
        }
    }
}
#endif



