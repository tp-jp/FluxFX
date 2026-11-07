using System;
using System.Collections.Generic;
using System.IO;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Inspector
{
    /// <summary>
    /// 削除中のFluxFX PrefabがInspectorの終了処理で再保存されることを防ぎます。
    /// </summary>
    internal sealed class FluxParticlePrefabDeletionGuard : AssetModificationProcessor
    {
        static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
            if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) return AssetDeleteResult.DidNotDelete;
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null || root.GetComponentInChildren<FluxParticleSystem>(true) == null) return AssetDeleteResult.DidNotDelete;

            var dirtyObjects = new HashSet<Object>();
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                CollectDirty(transform.gameObject, path, dirtyObjects);
                CollectDirty(PrefabUtility.GetPrefabInstanceHandle(transform.gameObject), path, dirtyObjects);
                foreach (var component in transform.GetComponents<Component>())
                {
                    CollectDirty(component, path, dirtyObjects);
                }
            }
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                CollectDirty(asset, path, dirtyObjects);
            }
            if (dirtyObjects.Count == 0) return AssetDeleteResult.DidNotDelete;

            var guid = AssetDatabase.AssetPathToGUID(path);
            // Unity 2022.3のPrefabImporterEditorは削除後のOnDestroyでもDirtyなAssetを保存する。
            // 削除するAssetだけを対象にし、削除が拒否された場合は未保存状態を戻す。
            foreach (var asset in dirtyObjects)
            {
                EditorUtility.ClearDirty(asset);
            }
            EditorApplication.delayCall += () => RestoreIfNotDeleted(path, guid, dirtyObjects);
            return AssetDeleteResult.DidNotDelete;
        }

        static void CollectDirty(Object asset, string path, HashSet<Object> dirtyObjects)
        {
            if (asset == null || !EditorUtility.IsPersistent(asset) ||
                AssetDatabase.GetAssetPath(asset) != path || !EditorUtility.IsDirty(asset)) return;
            dirtyObjects.Add(asset);
        }

        static void RestoreIfNotDeleted(string path, string guid, HashSet<Object> dirtyObjects)
        {
            if (!File.Exists(path) || AssetDatabase.AssetPathToGUID(path) != guid) return;
            foreach (var asset in dirtyObjects)
            {
                if (asset != null && AssetDatabase.GetAssetPath(asset) == path)
                {
                    EditorUtility.SetDirty(asset);
                }
            }
        }
    }
}
