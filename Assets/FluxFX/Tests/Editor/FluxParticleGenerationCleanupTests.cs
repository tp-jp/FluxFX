using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleGenerationCleanupTests
    {
        string root;
        string materialPath;
        string lookupPath;
        FluxParticleGenerationSession session;
        GameObject instance;

        [SetUp]
        public void SetUp()
        {
            root = "Assets/FluxFX/Tests/Cleanup_" + Guid.NewGuid().ToString("N");
            session = FluxParticleGenerationSession.Begin("Test", root);
            FluxParticleGenerationSession.EnsureFolder(session.MaterialRoot);
            FluxParticleGenerationSession.EnsureFolder(session.LookupRoot);
            lookupPath = session.LookupRoot + "/Lookup.asset";
            materialPath = session.MaterialRoot + "/Render.mat";
            var texture = new Texture2D(1, 1);
            AssetDatabase.CreateAsset(texture, lookupPath);
            var material = new Material(Shader.Find("Unlit/Texture")) { mainTexture = texture };
            AssetDatabase.CreateAsset(material, materialPath);
            session.Record(lookupPath, "Lookup", "test");
            session.Record(materialPath, "Material", "test");
            AssetDatabase.SaveAssets();
        }

        [TearDown]
        public void TearDown()
        {
            if (instance != null)
            {
                Object.DestroyImmediate(instance);
            }
            session?.Dispose();
            AssetDatabase.DeleteAsset(root);
        }

        void CreateRenderer()
        {
            instance = new GameObject("Cleanup reference", typeof(MeshRenderer));
            instance.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }

        void AssertAssetsRemain()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(materialPath));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(lookupPath));
        }

        int CollectAutomatically(DateTime cutoff)
        {
            var method = typeof(FluxParticleGenerationCleanup).GetMethod("CollectCore",
                BindingFlags.Static | BindingFlags.NonPublic);
            return (int)method.Invoke(null, new object[] { root, cutoff });
        }

        [Test]
        public void RequestsWaitAndProjectNotificationsDoNotScheduleAnotherCollection()
        {
            var type = typeof(FluxParticleGenerationCleanup);
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var pending = type.GetField("_pending", flags);
            var nextAttempt = type.GetField("_nextAttempt", flags);
            var previousPending = pending.GetValue(null);
            var previousAttempt = nextAttempt.GetValue(null);
            var automatic = FluxParticleGenerationCleanup.AutomaticCleanupEnabled;
            try
            {
                FluxParticleGenerationCleanup.AutomaticCleanupEnabled = true;
                var before = EditorApplication.timeSinceStartup;
                FluxParticleGenerationCleanup.Request();
                Assert.IsTrue((bool)pending.GetValue(null));
                Assert.GreaterOrEqual((double)nextAttempt.GetValue(null), before + 60);
                AssertAssetsRemain();
                pending.SetValue(null, false);
                type.GetMethod("OnProjectChanged", flags).Invoke(null, null);
                Assert.IsFalse((bool)pending.GetValue(null));
            }
            finally
            {
                FluxParticleGenerationCleanup.AutomaticCleanupEnabled = automatic;
                pending.SetValue(null, previousPending);
                nextAttempt.SetValue(null, previousAttempt);
            }
        }

        [Test]
        public void DisabledAutomaticCleanupDoesNotScheduleButAllowsManualCollection()
        {
            var automatic = FluxParticleGenerationCleanup.AutomaticCleanupEnabled;
            try
            {
                FluxParticleGenerationCleanup.AutomaticCleanupEnabled = false;
                session.Dispose();
                FluxParticleGenerationCleanup.Request();
                var type = typeof(FluxParticleGenerationCleanup);
                var flags = BindingFlags.Static | BindingFlags.NonPublic;
                type.GetMethod("OnProjectChanged", flags).Invoke(null, null);
                type.GetMethod("Update", flags).Invoke(null, null);
                Assert.IsFalse((bool)type.GetField("_pending", flags).GetValue(null));
                AssertAssetsRemain();
                type.GetMethod("CleanFromMenu", flags).Invoke(null, null);
                Assert.IsTrue((bool)type.GetField("_pending", flags).GetValue(null));
                Assert.IsTrue((bool)type.GetField("_manual", flags).GetValue(null));
                Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(root));
            }
            finally
            {
                FluxParticleGenerationCleanup.AutomaticCleanupEnabled = automatic;
            }
        }

        [Test]
        public void AutomaticCleanupRetainsRecentCacheButRemovesSessionMaterial()
        {
            session.Dispose();
            Assert.AreEqual(1, CollectAutomatically(DateTime.UtcNow.AddDays(-1)));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>(materialPath));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(lookupPath));
            Assert.AreEqual(1, FluxParticleGenerationCleanup.Collect(root));
        }

        [Test]
        public void AutomaticCleanupPreservesLiveReferencesEvenForOldCache()
        {
            CreateRenderer();
            session.Dispose();
            Assert.AreEqual(0, CollectAutomatically(DateTime.UtcNow.AddDays(1)));
            AssertAssetsRemain();
            Object.DestroyImmediate(instance);
            instance = null;
            Assert.AreEqual(2, CollectAutomatically(DateTime.UtcNow.AddDays(1)));
        }

        [Test]
        public void LatestSessionUseProtectsSharedCache()
        {
            session.Dispose();
            var manifest = File.ReadAllText(session.ManifestPath);
            var data = JsonUtility.FromJson<FluxParticleGenerationSession.Manifest>(manifest);
            foreach (var asset in data.assets)
            {
                asset.lastUsedUtc = DateTime.UtcNow.AddDays(-2).ToString("O");
            }
            File.WriteAllText(session.ManifestPath, JsonUtility.ToJson(data));
            using (var reuse = FluxParticleGenerationSession.Begin("Reuse", root))
            {
                reuse.Record(lookupPath, "Lookup", "reuse");
            }
            Assert.AreEqual(1, CollectAutomatically(DateTime.UtcNow.AddDays(-1)));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(lookupPath));
        }

        [Test]
        public void TrailMeshCacheRemainsReferencedAndIsCollectedAfterRelease()
        {
            FluxParticleGenerationSession.EnsureFolder(session.MeshRoot);
            var path = session.MeshRoot + "/Trail.asset";
            var mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, path);
            session.Record(path, "Mesh", "test");
            CreateRenderer();
            instance.AddComponent<MeshFilter>().sharedMesh = mesh;
            session.Dispose();
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Mesh>(path));
            Object.DestroyImmediate(instance);
            instance = null;
            Assert.AreEqual(3, FluxParticleGenerationCleanup.Collect(root));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Mesh>(path));
        }

        [Test]
        public void RunningSessionProtectsAssetsUntilItEnds()
        {
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
            AssertAssetsRemain();
            session.Dispose();
            Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(root));
            Assert.IsFalse(AssetDatabase.IsValidFolder(session.MaterialRoot));
            Assert.IsFalse(File.Exists(materialPath));
            Assert.IsFalse(File.Exists(lookupPath));
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
        }

        [Test]
        public void UnsavedSceneReferenceProtectsMaterialAndItsLookup()
        {
            CreateRenderer();
            session.Dispose();
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
            AssertAssetsRemain();
            Object.DestroyImmediate(instance);
            instance = null;
            Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(root));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedAndInterruptedSessionsReleaseUnusedAssets(bool interrupted)
        {
            if (interrupted)
            {
                session.Interrupt();
            }
            else
            {
                session.RecordFailure("Expected cleanup test failure");
                session.Dispose();
            }
            var deleted = FluxParticleGenerationCleanup.Collect(root);
            if (deleted != 2)
            {
                var report = new StringBuilder();
                report.AppendLine("Deleted: " + deleted);
                report.AppendLine("Session: " + session.State);
                var remaining = new[] { materialPath, lookupPath }.Where(File.Exists).ToArray();
                foreach (var path in remaining)
                {
                    report.AppendLine("Remaining: " + path + " GUID=" + AssetDatabase.AssetPathToGUID(path));
                }
                foreach (var path in AssetDatabase.GetAllAssetPaths())
                {
                    if (remaining.Contains(path) || AssetDatabase.IsValidFolder(path)) continue;
                    var references = AssetDatabase.GetDependencies(path, true).Intersect(remaining).ToArray();
                    if (references.Length == 0) continue;
                    report.AppendLine("Asset reference: " + path + " -> " + string.Join(", ", references));
                }
                foreach (var value in Resources.FindObjectsOfTypeAll<Object>())
                {
                    if (value == null || EditorUtility.IsPersistent(value)) continue;
                    var references = EditorUtility.CollectDependencies(new[] { value })
                        .Select(AssetDatabase.GetAssetPath).Intersect(remaining).ToArray();
                    if (references.Length == 0) continue;
                    report.AppendLine("Live reference: " + value.GetType().FullName + " / " + value.name +
                        " / " + value.GetInstanceID() + " -> " + string.Join(", ", references));
                }
                Directory.CreateDirectory("Temp/FluxFXTests");
                var reportPath = "Temp/FluxFXTests/CleanupFailure.txt";
                File.WriteAllText(reportPath, report.ToString());
                Assert.Fail("Expected 2 deletions. Diagnostics: " + reportPath + "\n" + report);
            }
        }

        [Test]
        public void SavedPrefabProtectsMaterialAndItsLookup()
        {
            CreateRenderer();
            var prefabPath = root + "/Referenced.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            instance = null;
            session.Dispose();
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
            AssertAssetsRemain();
            AssetDatabase.DeleteAsset(prefabPath);
            Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(root));
        }

        [Test]
        public void UnrecordedAndReplacedAssetsAreNotDeleted()
        {
            session.Dispose();
            var originalGuid = AssetDatabase.AssetPathToGUID(materialPath);
            AssetDatabase.DeleteAsset(materialPath);
            var replacement = new Material(Shader.Find("Unlit/Texture"))
            {
                mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(lookupPath)
            };
            var replacementPath = root + "/Replacement.mat";
            AssetDatabase.CreateAsset(replacement, replacementPath);
            Assert.AreNotEqual(originalGuid, AssetDatabase.AssetPathToGUID(replacementPath));
            Assert.IsEmpty(AssetDatabase.MoveAsset(replacementPath, materialPath));
            Assert.AreNotEqual(originalGuid, AssetDatabase.AssetPathToGUID(materialPath));
            var unrecorded = session.MaterialRoot + "/User.mat";
            AssetDatabase.CreateAsset(new Material(replacement), unrecorded);
            AssetDatabase.SaveAssets();
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
            AssertAssetsRemain();
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(unrecorded));
        }

        [Test]
        public void InvalidManifestAbortsBeforeDeletingAssets()
        {
            session.Dispose();
            var saved = File.ReadAllText(session.ManifestPath);
            try
            {
                File.WriteAllText(session.ManifestPath, "{}");
                Assert.Throws<InvalidOperationException>(() => FluxParticleGenerationCleanup.Collect(root));
                AssertAssetsRemain();
            }
            finally
            {
                File.WriteAllText(session.ManifestPath, saved);
            }
        }
    }
}
