using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleGenerationJournalTests
    {
        const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.NonPublic;
        static Type LedgerType => typeof(FluxParticleGenerationSession).Assembly.GetType("TpLab.Flux.FX.Editor.Build.FluxParticleGenerationLedger");
        string _root;
        string _asset;
        FluxParticleGenerationSession _session;
        string LedgerPath => "UserSettings/FluxFXGeneration/Roots/" + Hash128.Compute(_root) + ".json";
        string JournalPath => LedgerPath + ".journal";

        [SetUp]
        public void SetUp()
        {
            _root = FluxParticleTestPaths.TemporaryRoot + "/Journal_" + Guid.NewGuid().ToString("N");
            _session = FluxParticleGenerationSession.Begin("Journal test", _root);
            FluxParticleGenerationSession.EnsureFolder(_session.LookupRoot);
            _asset = _session.LookupRoot + "/Lookup.asset";
            AssetDatabase.CreateAsset(new Texture2D(1, 1), _asset);
        }

        [TearDown]
        public void TearDown()
        {
            _session?.Dispose();
            AssetDatabase.DeleteAsset(_root);
            File.Delete(LedgerPath);
            File.Delete(JournalPath);
            if (_session != null) File.Delete(_session.ManifestPath);
        }

        [Test]
        public void IdenticalRecordsDoNotRewriteManifestSnapshotOrJournal()
        {
            _session.Record(_asset, "Lookup", "owner");
            var manifest = File.ReadAllText(_session.ManifestPath);
            var snapshot = File.ReadAllText(LedgerPath);
            var journal = File.ReadAllText(JournalPath);
            var modified = File.GetLastWriteTimeUtc(_session.ManifestPath);
            for (var i = 0; i < 20; i++) FluxParticleGenerationSession.Load(_session.Id).Record(_asset, "Lookup", "owner");
            Assert.AreEqual(manifest, File.ReadAllText(_session.ManifestPath));
            Assert.AreEqual(modified, File.GetLastWriteTimeUtc(_session.ManifestPath));
            Assert.AreEqual(snapshot, File.ReadAllText(LedgerPath));
            Assert.AreEqual(journal, File.ReadAllText(JournalPath));
        }

        [Test]
        public void MultipleHandlesAndSessionsUnionOwnersAndDependenciesAfterCacheLoss()
        {
            var handle = FluxParticleGenerationSession.Load(_session.Id);
            _session.Record(_asset, "Lookup", "a", new[] { "Assets/dep-a" });
            handle.Record(_asset, "Lookup", "b", new[] { "Assets/dep-b" });
            ClearCaches();
            _session.Record(_asset, "Lookup", "a", new[] { "Assets/dep-c" });
            using (var other = FluxParticleGenerationSession.Begin("Other", _root))
            {
                other.Record(_asset, "Lookup", "c", new[] { "Assets/dep-d" });
                handle.Record(_asset, "Lookup", "d");
            }
            var asset = Records().Single();
            CollectionAssert.AreEquivalent(new[] { "a", "b", "c", "d" }, asset.owners);
            CollectionAssert.AreEquivalent(new[] { "Assets/dep-a", "Assets/dep-b", "Assets/dep-c", "Assets/dep-d" }, asset.dependencies);
            handle.RecordFailure("Expected failure");
            _session.Dispose();
            Assert.AreEqual("Failed", FluxParticleGenerationSession.Load(_session.Id).State);
        }

        [Test]
        public void DurableJournalProtectsAssetEvenWhenRunningManifestIsLost()
        {
            _session.Record(_asset, "Lookup", "owner");
            var original = File.ReadAllText(_session.ManifestPath);
            try
            {
                File.Delete(_session.ManifestPath);
                ClearCaches();
                Assert.AreEqual(AssetDatabase.AssetPathToGUID(_asset), Records().Single().guid);
                var report = FluxParticleGenerationCleanup.CollectWithReport(_root);
                Assert.IsTrue(report.blocked);
                Assert.AreEqual(0, report.DeletedCount);
                Assert.IsTrue(File.Exists(_asset));
            }
            finally { File.WriteAllText(_session.ManifestPath, original); }
        }

        [Test]
        public void CompletedCheckpointCanReplayLeftoverJournalWithoutDuplicatingOrReplacingOwnership()
        {
            _session.Record(_asset, "Lookup", "owner");
            var journal = File.ReadAllText(JournalPath);
            _session.Dispose();
            Assert.IsFalse(File.Exists(JournalPath));
            File.WriteAllText(JournalPath, journal);
            ClearCaches();
            var asset = Records().Single();
            CollectionAssert.AreEqual(new[] { "owner" }, asset.owners);
            Assert.AreEqual(AssetDatabase.AssetPathToGUID(_asset), asset.guid);
            using (var other = FluxParticleGenerationSession.Begin("Next", _root)) other.Record(_asset, "Lookup", "next");
            CollectionAssert.AreEquivalent(new[] { "owner", "next" }, Records().Single().owners);
        }

        [Test]
        public void PartialJournalBlocksCleanupInsteadOfDiscardingEvidence()
        {
            _session.Record(_asset, "Lookup", "owner");
            var journal = File.ReadAllText(JournalPath);
            try
            {
                File.AppendAllText(JournalPath, "{partial");
                ClearCaches();
                var report = FluxParticleGenerationCleanup.CollectWithReport(_root);
                Assert.IsTrue(report.failed);
                Assert.AreEqual(0, report.DeletedCount);
                StringAssert.Contains(JournalPath, string.Join("\n", report.diagnostics));
                Assert.IsTrue(File.Exists(_asset));
            }
            finally { File.WriteAllText(JournalPath, journal); }
        }

        [Test]
        public void FailedCheckpointLeavesJournalRecoverable()
        {
            _session.Record(_asset, "Lookup", "owner");
            using (var locked = new FileStream(LedgerPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.Throws<IOException>(() => _session.Dispose());
                Assert.IsTrue(File.Exists(JournalPath));
            }
            ClearCaches();
            Assert.AreEqual(1, Records().Length);
            var report = FluxParticleGenerationCleanup.Analyze(_root);
            Assert.IsFalse(report.failed, string.Join("\n", report.diagnostics));
            Assert.IsFalse(report.blocked);
            Assert.AreEqual(1, report.CandidateCount);
        }

        [Test]
        public void ExternalSessionTerminationIsNotOverwrittenByCachedHandle()
        {
            _session.Record(_asset, "Lookup", "owner");
            var manifest = JsonUtility.FromJson<FluxParticleGenerationSession.Manifest>(File.ReadAllText(_session.ManifestPath));
            manifest.state = "Interrupted";
            manifest.endedUtc = DateTime.UtcNow.ToString("O");
            File.WriteAllText(_session.ManifestPath, JsonUtility.ToJson(manifest));
            Assert.Throws<InvalidOperationException>(() => _session.Record(_asset, "Lookup", "late"));
            Assert.AreEqual("Interrupted", FluxParticleGenerationSession.Load(_session.Id).State);
        }

        [Test]
        public void FailedDependencyEnumerationDoesNotLeakUncommittedChangesToOtherHandles()
        {
            _session.Record(_asset, "Lookup", "owner");
            Assert.Throws<InvalidOperationException>(() => _session.Record(_asset, "Lookup", "failed", FailingDependencies()));
            FluxParticleGenerationSession.Load(_session.Id).Record(_asset, "Lookup", "owner");
            var record = Records().Single();
            CollectionAssert.AreEqual(new[] { "owner" }, record.owners);
            Assert.IsEmpty(record.dependencies);
        }

        [Test]
        public void UnsavedOwnersAreUniqueAndStableWithinSession()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var first = new GameObject("First");
            var second = new GameObject("Second");
            SceneManager.MoveGameObjectToScene(first, scene);
            SceneManager.MoveGameObjectToScene(second, scene);
            try
            {
                var a = first.AddComponent<FluxParticleAuthoring>();
                var b = second.AddComponent<FluxParticleAuthoring>();
                var id = Owner(a, _session);
                StringAssert.StartsWith("Temporary:", id);
                Assert.AreEqual(id, Owner(a, _session));
                Assert.AreNotEqual(id, Owner(b, _session));
                using (var other = FluxParticleGenerationSession.Begin("Other", _root)) Assert.AreNotEqual(id, Owner(a, other));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void SavedPrefabInstancesStayDistinctAndStableAcrossSceneReloadAndSessions()
        {
            var originalActive = SceneManager.GetActiveScene();
            Assert.IsEmpty(originalActive.path, "Run in Test Runner's isolated empty scene.");
            Assert.IsEmpty(originalActive.GetRootGameObjects());
            var scene = originalActive;
            var path = _root + "/Owners.unity";
            try
            {
                var template = new GameObject("Template");
                template.AddComponent<FluxParticleAuthoring>();
                var prefab = PrefabUtility.SaveAsPrefabAsset(template, _root + "/Owner.prefab");
                Object.DestroyImmediate(template);
                var a = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<FluxParticleAuthoring>();
                var b = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<FluxParticleAuthoring>();
                a.name = "a"; b.name = "b";
                EditorSceneManager.SaveScene(scene, path);
                var first = Owner(a, _session);
                var second = Owner(b, _session);
                Assert.AreNotEqual(first, second);
                Assert.AreNotEqual(first, Owner(prefab.GetComponent<FluxParticleAuthoring>(), _session));
                StringAssert.StartsWith("GlobalObjectId_", first);
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                using (var other = FluxParticleGenerationSession.Begin("Later", _root))
                {
                    foreach (var go in scene.GetRootGameObjects())
                        Assert.AreEqual(go.name == "a" ? first : second, Owner(go.GetComponent<FluxParticleAuthoring>(), other));
                    var unsaved = new GameObject("Unsaved addition").AddComponent<FluxParticleAuthoring>();
                    var another = new GameObject("Another unsaved addition").AddComponent<FluxParticleAuthoring>();
                    try
                    {
                        var unsavedId = Owner(unsaved, other);
                        Assert.AreEqual(unsavedId, Owner(unsaved, other));
                        Assert.AreNotEqual(first, unsavedId);
                        Assert.AreNotEqual(second, unsavedId);
                        Assert.AreNotEqual(unsavedId, Owner(another, other));
                    }
                    finally
                    {
                        Object.DestroyImmediate(unsaved.gameObject);
                        Object.DestroyImmediate(another.gameObject);
                    }
                }
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        static string Owner(Object value, FluxParticleGenerationSession session) => (string)typeof(FluxParticleGenerationSession).Assembly
            .GetType("TpLab.Flux.FX.Editor.Build.FluxParticleGenerationOwner").GetMethod("GetId", StaticMembers).Invoke(null, new object[] { value, session });

        static System.Collections.Generic.IEnumerable<string> FailingDependencies()
        {
            yield return "Assets/not-committed";
            throw new InvalidOperationException("Expected enumeration failure");
        }

        FluxParticleGenerationSession.AssetRecord[] Records()
        {
            var ledger = LedgerType.GetMethod("Load", StaticMembers).Invoke(null, new object[] { _root });
            return ((IEnumerable)ledger.GetType().GetField("assets").GetValue(ledger)).Cast<FluxParticleGenerationSession.AssetRecord>().ToArray();
        }

        static void ClearCaches()
        {
            ((IDictionary)LedgerType.GetField("_records", StaticMembers).GetValue(null)).Clear();
            ((IDictionary)typeof(FluxParticleGenerationSession).GetField("_loaded", StaticMembers).GetValue(null)).Clear();
        }
    }
}
