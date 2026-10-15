using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 参照されていない生成Assetを管理記録に基づいて削除します。
    /// </summary>
    [InitializeOnLoad]
    public static class FluxParticleGenerationCleanup
    {
        static bool pending;
        static double nextAttempt;

        static FluxParticleGenerationCleanup()
        {
            EditorApplication.update += Update;
            EditorApplication.projectChanged += Request;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += scene => Request();
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += scene => Request();
        }

        /// <summary>
        /// Editorが待機状態になった後のクリーンアップを予約します。
        /// </summary>
        public static void Request()
        {
            pending = true;
            nextAttempt = EditorApplication.timeSinceStartup + 2;
        }

        static bool IsBusy => EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer ||
            FluxParticleGenerationLifecycle.Current != null;

        static void Update()
        {
            if (!pending || EditorApplication.timeSinceStartup < nextAttempt || IsBusy) return;
            pending = false;
            try
            {
                var deleted = Collect();
                if (deleted > 0)
                {
                    Debug.Log("[FluxFX] Removed unused generated assets: " + deleted);
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("[FluxFX] Generation cleanup was deferred: " + error.Message);
            }
        }

        [MenuItem("Tools/FluxFX/Clean Unused Generated Assets")]
        static void CleanFromMenu()
        {
            Request();
        }

        /// <summary>
        /// 終了済みセッションの未使用Assetを削除します。
        /// </summary>
        /// <param name="root">生成先のルート</param>
        /// <returns>削除したAssetの数</returns>
        public static int Collect(string root = FluxParticleGenerationSession.DefaultRoot)
        {
            if (IsBusy || !Directory.Exists(FluxParticleGenerationSession.ManifestRoot)) return 0;
            var sessions = Directory.GetFiles(FluxParticleGenerationSession.ManifestRoot, "*.json")
                .Select(path => FluxParticleGenerationSession.Load(Path.GetFileNameWithoutExtension(path)))
                .ToArray();
            var owned = sessions.Where(session => session.ShaderRoot == root + "/Cache/Shaders").ToArray();
            if (owned.Any(session => session.State == "Running")) return 0;

            var candidates = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var session in owned)
            {
                if (session.State != "Completed" && session.State != "Failed" && session.State != "Interrupted")
                {
                    throw new InvalidOperationException("Unknown generation session state: " + session.Id);
                }
                foreach (var asset in session.Assets)
                {
                    if (!IsOwnedAsset(session, asset)) continue;
                    if (AssetDatabase.AssetPathToGUID(asset.path) != asset.guid) continue;
                    candidates[asset.path] = asset.guid;
                }
            }
            if (candidates.Count == 0) return 0;

            // 管理対象外のAssetと、未保存のScene・Prefab Stageを参照元として保護します。
            var external = AssetDatabase.GetAllAssetPaths()
                .Where(path => !candidates.ContainsKey(path) && !AssetDatabase.IsValidFolder(path)).ToArray();
            var protectedPaths = new HashSet<string>(AssetDatabase.GetDependencies(external, true), StringComparer.Ordinal);
            var liveObjects = Resources.FindObjectsOfTypeAll<Object>()
                .Where(value => value != null && !EditorUtility.IsPersistent(value)).ToArray();
            foreach (var dependency in EditorUtility.CollectDependencies(liveObjects))
            {
                var path = AssetDatabase.GetAssetPath(dependency);
                if (!string.IsNullOrEmpty(path))
                {
                    protectedPaths.Add(path);
                }
            }
            // 保護したMaterialから参照されるShader・Lookupも残します。
            protectedPaths.UnionWith(AssetDatabase.GetDependencies(protectedPaths.ToArray(), true));

            var deleted = 0;
            foreach (var path in candidates.Keys.OrderBy(path => path.EndsWith(".mat", StringComparison.Ordinal) ? 0 : 1))
            {
                if (protectedPaths.Contains(path)) continue;
                if (IsBusy) break;
                var dependencies = AssetDatabase.GetDependencies(path, true);
                if (AssetDatabase.AssetPathToGUID(path) == candidates[path] && AssetDatabase.DeleteAsset(path))
                {
                    deleted++;
                }
                else
                {
                    protectedPaths.UnionWith(dependencies);
                }
            }
            foreach (var session in owned)
            {
                RemoveEmptyFolder(session.MaterialRoot);
                RemoveEmptyFolder(root + "/Sessions/" + session.Id);
            }
            return deleted;
        }

        static bool IsOwnedAsset(FluxParticleGenerationSession session, FluxParticleGenerationSession.AssetRecord asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.guid) || string.IsNullOrEmpty(asset.path)) return false;
            var directory = Path.GetDirectoryName(asset.path)?.Replace('\\', '/');
            var type = AssetDatabase.GetMainAssetTypeAtPath(asset.path);
            return asset.kind == "Material" && directory == session.MaterialRoot && type == typeof(Material) ||
                asset.kind == "Shader" && directory == session.ShaderRoot && type == typeof(Shader) ||
                asset.kind == "Lookup" && directory == session.LookupRoot && type == typeof(Texture2D) ||
                asset.kind == "Mesh" && directory == session.MeshRoot && type == typeof(Mesh);
        }

        static void RemoveEmptyFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path) || Directory.EnumerateFileSystemEntries(path).Any()) return;
            AssetDatabase.DeleteAsset(path);
        }
    }
}
