using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
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
        const double QuietSeconds = 60;
        const double CollectionInterval = 300;
        const double MaintenanceInterval = 3600;
        const string AutomaticCleanupKey = "TpLab.Flux.FX.AutomaticGenerationCleanup";
        const string AutomaticCleanupMenu = "Tools/FluxFX/Automatic Generated Asset Cleanup";
        static bool _pending;
        static bool _manual;
        static double _nextAttempt;
        static double _nextCollection;
        static double _nextMaintenance;

        /// <summary>
        /// 自動クリーンアップの有効状態。既定は無効で、設定はEditor全体で共有
        /// </summary>
        public static bool AutomaticCleanupEnabled
        {
            get => EditorPrefs.GetBool(AutomaticCleanupKey, false);
            set
            {
                EditorPrefs.SetBool(AutomaticCleanupKey, value);
                _pending = false;
                _manual = false;
                if (value)
                {
                    Request();
                }
            }
        }

        /// <summary>
        /// 終了済みセッションの未使用Assetを削除します。
        /// </summary>
        /// <param name="root">生成先のルート</param>
        /// <returns>削除したAssetの数</returns>
        public static int Collect(string root = FluxParticleGenerationSession.DefaultRoot)
        {
            return CollectCore(root, null);
        }

        /// <summary>
        /// 自動整理が有効な場合に、クリーンアップを予約します。
        /// </summary>
        public static void Request()
        {
            if (!AutomaticCleanupEnabled) return;
            _pending = true;
            _nextAttempt = EditorApplication.timeSinceStartup + QuietSeconds;
        }

        static FluxParticleGenerationCleanup()
        {
            EditorApplication.update += Update;
            EditorApplication.projectChanged += OnProjectChanged;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += scene => Request();
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += scene => Request();
        }

        static void OnProjectChanged()
        {
            if (!AutomaticCleanupEnabled) return;
            // 削除自身の通知では新しい整理を予約しません。
            _nextAttempt = EditorApplication.timeSinceStartup + QuietSeconds;
        }

        static bool IsBusy => EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer ||
            FluxParticleGenerationLifecycle.Current != null;

        static void Update()
        {
            if (!_manual && !AutomaticCleanupEnabled) return;
            var now = EditorApplication.timeSinceStartup;
            if (IsBusy)
            {
                _nextAttempt = now + QuietSeconds;
                return;
            }
            if (!_pending && now >= _nextMaintenance)
            {
                Request();
            }
            if (!_pending || (!_manual && (now < _nextAttempt || now < _nextCollection))) return;
            var automatic = !_manual;
            _pending = false;
            _manual = false;
            _nextCollection = now + CollectionInterval;
            _nextMaintenance = now + MaintenanceInterval;
            try
            {
                var deleted = CollectCore(FluxParticleGenerationSession.DefaultRoot,
                    automatic ? (DateTime?)DateTime.UtcNow.AddDays(-1) : null);
                if (deleted > 0)
                {
                    Debug.Log("[FluxFX] Removed unused generated assets: " + deleted);
                }
            }
            catch (Exception error)
            {
                Request();
                Debug.LogWarning("[FluxFX] Generation cleanup was deferred: " + error.Message);
            }
        }

        [MenuItem("Tools/FluxFX/Clean Unused Generated Assets")]
        static void CleanFromMenu()
        {
            _pending = true;
            _manual = true;
        }

        [MenuItem(AutomaticCleanupMenu)]
        static void ToggleAutomaticCleanup()
        {
            AutomaticCleanupEnabled = !AutomaticCleanupEnabled;
        }

        [MenuItem(AutomaticCleanupMenu, true)]
        static bool ValidateAutomaticCleanup()
        {
            Menu.SetChecked(AutomaticCleanupMenu, AutomaticCleanupEnabled);
            return true;
        }

        static int CollectCore(string root, DateTime? cacheCutoff)
        {
            if (IsBusy || !Directory.Exists(FluxParticleGenerationSession.ManifestRoot)) return 0;
            var sessions = Directory.GetFiles(FluxParticleGenerationSession.ManifestRoot, "*.json")
                .Select(path => FluxParticleGenerationSession.Load(Path.GetFileNameWithoutExtension(path)))
                .ToArray();
            var owned = sessions.Where(session => session.ShaderRoot == root + "/Cache/Shaders").ToArray();
            if (owned.Any(session => session.State == "Running")) return 0;

            // 同じCacheを再利用したセッションがあれば、最新の利用記録を優先します。
            var recentCache = new HashSet<string>(StringComparer.Ordinal);
            if (cacheCutoff.HasValue)
            {
                foreach (var asset in owned.SelectMany(session => session.Assets))
                {
                    if (asset == null || asset.kind == "Material") continue;
                    if (!DateTime.TryParse(asset.lastUsedUtc, CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var used) || used.ToUniversalTime() > cacheCutoff.Value)
                    {
                        recentCache.Add(asset.path);
                    }
                }
            }

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
                    if (recentCache.Contains(asset.path)) continue;
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
