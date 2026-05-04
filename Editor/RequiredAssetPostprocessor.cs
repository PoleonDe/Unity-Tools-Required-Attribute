#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using UnityEditor;

namespace Control.Tools.Required.Editor
{
    internal sealed class RequiredAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            RequiredScanner.RequestAssetRefresh(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }
    }
}
#endif



