using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TpLab.Flux.FX.Editor.Authoring
{
    /// <summary>
    /// 旧Systemの編集設定をAuthoringへ移し、Prefabの項目単位の継承を維持します。
    /// </summary>
    public static class FluxParticleSettingsMigration
    {
        const string VersionProperty = "settingsVersion";
        const int CurrentVersion = 1;
        static readonly string[] _properties = { "particleCount", "playOnAwake" };

        /// <summary>
        /// 設定の移行が必要か確認します。Objectは変更しません。
        /// </summary>
        /// <param name="authoring">確認対象の編集設定</param>
        /// <returns>旧Systemからの移行が必要な場合はtrue</returns>
        public static bool NeedsMigration(FluxParticleAuthoring authoring)
        {
            using (var settings = new SerializedObject(authoring))
            {
                var version = settings.FindProperty(VersionProperty);
                if (version.intValue < CurrentVersion) return true;
                if (version.prefabOverride) return false;
                var system = authoring.GetComponent<FluxParticleSystem>();
                if (system == null) return false;
                using (var legacy = new SerializedObject(system))
                {
                    if (_properties.Any(name => legacy.FindProperty(name).prefabOverride)) return true;
                }
                var source = PrefabUtility.GetCorrespondingObjectFromSource(authoring);
                return source != null && NeedsMigration(source);
            }
        }

        /// <summary>
        /// 移行前の設定も含めてベイク元の値を取得します。保存データは変更しません。
        /// </summary>
        /// <param name="authoring">ベイク元の編集設定</param>
        /// <param name="particleCount">解決したParticle Slot数</param>
        /// <param name="playOnAwake">解決した起動時の自動再生</param>
        public static void Read(FluxParticleAuthoring authoring, out int particleCount, out bool playOnAwake)
        {
            particleCount = authoring.ParticleCount;
            playOnAwake = authoring.PlayOnAwake;
            var system = authoring.GetComponent<FluxParticleSystem>();
            if (system == null || !NeedsMigration(authoring)) return;
            using (var settings = new SerializedObject(authoring))
            using (var legacy = new SerializedObject(system))
            {
                var unversioned = settings.FindProperty(VersionProperty).intValue < CurrentVersion;
                var source = PrefabUtility.GetCorrespondingObjectFromSource(authoring);
                if (source != null && NeedsMigration(source))
                {
                    Read(source, out var inheritedCount, out var inheritedPlay);
                    if (!settings.FindProperty("particleCount").prefabOverride)
                    {
                        particleCount = inheritedCount;
                    }
                    if (!settings.FindProperty("playOnAwake").prefabOverride)
                    {
                        playOnAwake = inheritedPlay;
                    }
                }
                if (UseLegacy(settings, legacy, "particleCount", unversioned))
                {
                    particleCount = legacy.FindProperty("particleCount").intValue;
                }
                if (UseLegacy(settings, legacy, "playOnAwake", unversioned))
                {
                    playOnAwake = legacy.FindProperty("playOnAwake").boolValue;
                }
            }
        }

        /// <summary>
        /// 対象と参照元Prefabの旧設定を移行します。Sceneの変更はUndo対象とし、自動保存しません。
        /// </summary>
        /// <param name="authoring">移行対象の編集設定</param>
        public static void MigrateTarget(FluxParticleAuthoring authoring)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            if (EditorUtility.IsPersistent(authoring))
            {
                MigratePrefab(AssetDatabase.GetAssetPath(authoring), visited);
            }
            else
            {
                MigrateSources(authoring, visited);
                MigrateComponent(authoring, true);
            }
        }

        /// <summary>
        /// 階層内の設定と参照元Prefabを移行します。
        /// </summary>
        /// <param name="root">移行対象階層のルート</param>
        public static void MigrateHierarchy(GameObject root)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var authoring in root.GetComponentsInChildren<FluxParticleAuthoring>(true))
            {
                MigrateSources(authoring, visited);
                MigrateComponent(authoring, true);
            }
        }

        [MenuItem("Tools/FluxFX/Migration/Migrate Prefabs and Open Scenes")]
        static void MigrateProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                MigratePrefab(AssetDatabase.GUIDToAssetPath(guid), visited);
            }
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    MigrateHierarchy(root);
                }
            }
            Debug.Log("[FluxFX] Settings migration finished. Save changed open scenes. Other scenes can be migrated when opened; Play/build also reads legacy settings.");
        }

        static bool UseLegacy(SerializedObject settings, SerializedObject legacy, string name, bool unversioned)
        {
            if (settings.FindProperty(name).prefabOverride) return false;
            return unversioned || legacy.FindProperty(name).prefabOverride;
        }

        static void MigrateSources(FluxParticleAuthoring authoring, HashSet<string> visited)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(authoring);
            if (source == null) return;
            var path = AssetDatabase.GetAssetPath(source);
            MigratePrefab(path, visited);
        }

        static void MigratePrefab(string path, HashSet<string> visited)
        {
            if (string.IsNullOrEmpty(path) || !visited.Add(path)) return;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponentInChildren<FluxParticleAuthoring>(true) == null) return;
            // 先に親を移行する。子に残る旧SystemのOverrideはその後にAuthoringへ移す。
            foreach (var authoring in asset.GetComponentsInChildren<FluxParticleAuthoring>(true))
            {
                MigrateSources(authoring, visited);
            }
            if (!asset.GetComponentsInChildren<FluxParticleAuthoring>(true).Any(NeedsMigration)) return;
            if (asset.GetComponentsInChildren<FluxParticleAuthoring>(true).Any(EditorUtility.IsDirty) ||
                asset.GetComponentsInChildren<FluxParticleSystem>(true).Any(EditorUtility.IsDirty))
            {
                throw new InvalidOperationException("Save pending prefab edits before migrating: " + path);
            }
            if (!AssetDatabase.IsOpenForEdit(path))
            {
                throw new InvalidOperationException("Cannot migrate read-only prefab: " + path);
            }
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var changed = false;
                foreach (var authoring in contents.GetComponentsInChildren<FluxParticleAuthoring>(true))
                {
                    changed |= MigrateComponent(authoring, false);
                }
                if (changed && PrefabUtility.SaveAsPrefabAsset(contents, path) == null)
                {
                    throw new InvalidOperationException("Failed to save migrated prefab: " + path);
                }
            }
            finally
            {
                // UdonSharpの初期化が登録する遅延処理より後に破棄する。
                EditorApplication.delayCall += () =>
                {
                    if (contents != null) PrefabUtility.UnloadPrefabContents(contents);
                };
            }
        }

        static bool MigrateComponent(FluxParticleAuthoring authoring, bool undo)
        {
            if (!NeedsMigration(authoring)) return false;
            var system = authoring.GetComponent<FluxParticleSystem>();
            if (system == null) throw new InvalidOperationException("Settings migration requires FluxParticleSystem: " + authoring.name);
            Read(authoring, out var count, out var play);
            if (undo) Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { authoring, system }, "Migrate Flux FX Settings");
            using (var settings = new SerializedObject(authoring))
            using (var legacy = new SerializedObject(system))
            {
                var version = settings.FindProperty(VersionProperty);
                var unversioned = version.intValue < CurrentVersion;
                var overrides = _properties.Where(name => legacy.FindProperty(name).prefabOverride).ToArray();
                if (UseLegacy(settings, legacy, "particleCount", unversioned))
                {
                    settings.FindProperty("particleCount").intValue = count;
                }
                if (UseLegacy(settings, legacy, "playOnAwake", unversioned))
                {
                    settings.FindProperty("playOnAwake").boolValue = play;
                }
                version.intValue = CurrentVersion;
                settings.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.IsPartOfPrefabInstance(authoring))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
                    // 同値の明示Overrideも保持する。移行済みの判定をこのPrefab階層に記録する。
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(authoring);
                    if (source != null)
                    {
                        var modifications = (PrefabUtility.GetPropertyModifications(authoring) ?? Array.Empty<PropertyModification>()).ToList();
                        foreach (var name in overrides.Concat(new[] { VersionProperty }))
                        {
                            modifications.RemoveAll(item => item.target == source && item.propertyPath == name);
                            var value = name == VersionProperty ? CurrentVersion.ToString() :
                                name == "particleCount" ? authoring.ParticleCount.ToString() : authoring.PlayOnAwake ? "1" : "0";
                            modifications.Add(new PropertyModification { target = source, propertyPath = name, value = value });
                        }
                        PrefabUtility.SetPropertyModifications(authoring, modifications.ToArray());
                    }
                    foreach (var name in overrides)
                    {
                        PrefabUtility.RevertPropertyOverride(legacy.FindProperty(name), undo ? InteractionMode.UserAction : InteractionMode.AutomatedAction);
                        legacy.Update();
                    }
                }
            }
            EditorUtility.SetDirty(authoring);
            return true;
        }
    }
}
