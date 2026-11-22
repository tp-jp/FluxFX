using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleGenerationCleanupPlanTests
    {
        const BindingFlags InternalStatic = BindingFlags.Static | BindingFlags.NonPublic;
        const string SchedulePath = "ProjectSettings/FluxFXGeneration/CleanupSchedule.json";
        string _root;
        string _materialPath;
        string _lookupPath;
        string _scheduleBefore;
        bool _automatic;
        FluxParticleGenerationSession _session;
        GameObject _instance;

        string LedgerPath => "ProjectSettings/FluxFXGeneration/Roots/" + Hash128.Compute(_root) + ".json";

        [SetUp]
        public void SetUp()
        {
            _automatic = FluxParticleGenerationCleanup.AutomaticCleanupEnabled;
            _scheduleBefore = File.Exists(SchedulePath) ? File.ReadAllText(SchedulePath) : null;
            FluxParticleGenerationCleanup.AutomaticCleanupEnabled = false;
            _root = FluxParticleTestPaths.TemporaryRoot + "/CleanupPlan_" + Guid.NewGuid().ToString("N");
            _session = FluxParticleGenerationSession.Begin("Cleanup plan test", _root);
            FluxParticleGenerationSession.EnsureFolder(_session.LookupRoot);
            FluxParticleGenerationSession.EnsureFolder(_session.MaterialRoot);
            _lookupPath = _session.LookupRoot + "/Lookup.asset";
            _materialPath = _session.MaterialRoot + "/Render.mat";
            var texture = new Texture2D(1, 1);
            AssetDatabase.CreateAsset(texture, _lookupPath);
            AssetDatabase.CreateAsset(new Material(Shader.Find("Unlit/Texture")) { mainTexture = texture }, _materialPath);
            _session.Record(_lookupPath, "Lookup", "fixture");
            _session.Record(_materialPath, "Material", "fixture");
        }

        [TearDown]
        public void TearDown()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
            _session.Dispose();
            AssetDatabase.DeleteAsset(_root);
            File.Delete(LedgerPath);
            File.Delete(_session.ManifestPath);
            if (_scheduleBefore == null) File.Delete(SchedulePath);
            else File.WriteAllText(SchedulePath, _scheduleBefore);
            FluxParticleGenerationCleanup.AutomaticCleanupEnabled = _automatic;
        }

        [Test]
        public void AnalyzeReportsSizeWithoutDeletingAndCollectRemovesDependencies()
        {
            _session.Dispose();
            var report = FluxParticleGenerationCleanup.Analyze(_root);
            Assert.IsFalse(report.failed, string.Join("\n", report.diagnostics));
            Assert.IsTrue(report.fullScan);
            Assert.AreEqual(2, report.CandidateCount);
            Assert.Greater(report.CandidateBytes, 0);
            Assert.IsTrue(File.Exists(report.logPath));
            Assert.IsTrue(File.Exists(_materialPath));
            Assert.IsTrue(File.Exists(_lookupPath));
            Assert.AreEqual(2, FluxParticleGenerationCleanup.CollectReviewed(report).DeletedCount);
            Assert.IsFalse(File.Exists(LedgerPath));
        }

        [Test]
        public void ReferenceAddedAfterAnalysisIsRecheckedAndExplained()
        {
            _session.Dispose();
            var review = FluxParticleGenerationCleanup.Analyze(_root);
            CreateReference();
            var report = FluxParticleGenerationCleanup.CollectReviewed(review);
            Assert.AreEqual(0, report.DeletedCount);
            Assert.IsTrue(report.items.All(item => item.status == "Keep"));
            Assert.IsTrue(report.items.All(item => !string.IsNullOrEmpty(item.reference)));
            Assert.IsTrue(report.items.Any(item => item.reference.Contains("Cleanup plan reference")));
        }

        [Test]
        public void NewlyCreatedCandidatesAreNotPartOfReviewedDeletion()
        {
            _session.Dispose();
            var review = FluxParticleGenerationCleanup.Analyze(_root);
            var path = _session.LookupRoot + "/Later.asset";
            using (var later = FluxParticleGenerationSession.Begin("Later", _root))
            {
                AssetDatabase.CreateAsset(new Texture2D(1, 1), path);
                later.Record(path, "Lookup", "later");
            }
            var report = FluxParticleGenerationCleanup.CollectReviewed(review);
            Assert.AreEqual(2, report.DeletedCount);
            Assert.AreEqual("Keep", report.items.Single(item => item.path == path).status);
            Assert.IsTrue(File.Exists(path));
        }

        [Test]
        public void OwnershipSurvivesLossOfLibrarySessionManifest()
        {
            _session.Dispose();
            File.Delete(_session.ManifestPath);
            var report = FluxParticleGenerationCleanup.Analyze(_root);
            Assert.IsFalse(report.blocked, string.Join("\n", report.diagnostics));
            Assert.AreEqual(2, report.CandidateCount);
            Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(_root));
        }

        [Test]
        public void LostRunningManifestKeepsDurableActiveSessionProtection()
        {
            var manifest = File.ReadAllText(_session.ManifestPath);
            try
            {
                File.Delete(_session.ManifestPath);
                var report = FluxParticleGenerationCleanup.CollectWithReport(_root);
                Assert.IsTrue(report.blocked);
                Assert.IsFalse(report.fullScan);
                Assert.AreEqual(0, report.DeletedCount);
                StringAssert.Contains(_session.Id, string.Join("\n", report.diagnostics));
                Assert.IsTrue(File.Exists(_materialPath));
            }
            finally { File.WriteAllText(_session.ManifestPath, manifest); }
        }

        [Test]
        public void LegacyRecordsMigrateOnlyMatchingIdentities()
        {
            _session.Dispose();
            File.Delete(LedgerPath);
            AssetDatabase.DeleteAsset(_materialPath);
            AssetDatabase.CreateAsset(new Material(Shader.Find("Unlit/Texture"))
                { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_lookupPath) }, _root + "/Replacement.mat");
            Assert.IsEmpty(AssetDatabase.MoveAsset(_root + "/Replacement.mat", _materialPath));
            var report = FluxParticleGenerationCleanup.Analyze(_root);
            Assert.IsFalse(report.failed, string.Join("\n", report.diagnostics));
            Assert.IsTrue(File.Exists(LedgerPath));
            StringAssert.DoesNotContain(AssetDatabase.AssetPathToGUID(_materialPath), File.ReadAllText(LedgerPath));
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(_root));
            Assert.IsTrue(File.Exists(_materialPath));
        }

        [Test]
        public void PreviousEditorHistoryIsCompactedWithoutLosingReferencedOwnership()
        {
            _session.Dispose();
            CreateReference();
            var manifest = JsonUtility.FromJson<FluxParticleGenerationSession.Manifest>(File.ReadAllText(_session.ManifestPath));
            manifest.editorSessionId = Guid.NewGuid().ToString("N");
            File.WriteAllText(_session.ManifestPath, JsonUtility.ToJson(manifest));
            var report = FluxParticleGenerationCleanup.CollectWithReport(_root);
            Assert.IsFalse(report.failed, string.Join("\n", report.diagnostics));
            Assert.AreEqual(0, report.DeletedCount);
            Assert.IsFalse(File.Exists(_session.ManifestPath));
            Assert.IsTrue(File.Exists(LedgerPath));
            Object.DestroyImmediate(_instance);
            _instance = null;
            Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(_root));
        }

        [Test]
        public void CorruptLedgerBlocksDeletionAndIdentifiesTheFile()
        {
            _session.Dispose();
            var saved = File.ReadAllText(LedgerPath);
            try
            {
                File.WriteAllText(LedgerPath, "{}");
                var report = FluxParticleGenerationCleanup.CollectWithReport(_root);
                Assert.IsTrue(report.failed);
                Assert.IsFalse(report.fullScan);
                StringAssert.Contains(LedgerPath, string.Join("\n", report.diagnostics));
                Assert.IsTrue(File.Exists(_materialPath));
            }
            finally { File.WriteAllText(LedgerPath, saved); }
        }

        [Test]
        public void AutomaticCleanupRetainsFailedSessionMaterialForDiagnosis()
        {
            _session.RecordFailure("Expected failure");
            _session.Dispose();
            var collect = typeof(FluxParticleGenerationCleanup).GetMethod("CollectCore", InternalStatic);
            Assert.AreEqual(0, collect.Invoke(null, new object[] { _root, DateTime.UtcNow.AddDays(-1) }));
            Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(_root));
        }

        [Test]
        public void RunningSessionDoesNotStartFullAssetScan()
        {
            var report = FluxParticleGenerationCleanup.Analyze(_root);
            Assert.IsTrue(report.blocked);
            Assert.IsFalse(report.fullScan);
            Assert.AreEqual(0, report.CandidateCount);
        }

        [Test]
        public void ChildIncludeSurvivesParentDeletionFailureRegardlessOfFilenameOrder()
        {
            FluxParticleGenerationSession.EnsureFolder(_session.ShaderRoot);
            var child = _session.ShaderRoot + "/A-child.hlsl";
            var parent = _session.ShaderRoot + "/Z-parent.hlsl";
            File.WriteAllText(child, "float Child() { return 1; }");
            File.WriteAllText(parent, "#include \"" + child + "\"");
            AssetDatabase.ImportAsset(child, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(parent, ImportAssetOptions.ForceSynchronousImport);
            _session.Record(child, "ShaderInclude", "test");
            _session.Record(parent, "ShaderInclude", "test", new[] { child });
            _session.Dispose();
            var report = FluxParticleGenerationCleanup.Analyze(_root);
            var dependencies = new Dictionary<string, string[]> { [parent] = new[] { child }, [_materialPath] = new[] { _lookupPath } };
            var assembly = typeof(FluxParticleGenerationCleanup).Assembly;
            var analysis = assembly.GetType("TpLab.Flux.FX.Editor.Build.FluxParticleGenerationCleanupAnalysis", true);
            var order = (List<FluxParticleGenerationCleanupReport.Item>)analysis.GetMethod("Order", InternalStatic)
                .Invoke(null, new object[] { report, dependencies });
            Assert.Less(order.FindIndex(item => item.path == parent), order.FindIndex(item => item.path == child));
            var ledgerType = assembly.GetType("TpLab.Flux.FX.Editor.Build.FluxParticleGenerationLedger", true);
            var ledger = ledgerType.GetMethod("Load", InternalStatic).Invoke(null, new object[] { _root });
            var records = (IEnumerable)ledger.GetType().GetField("assets").GetValue(ledger);
            var entryType = ledgerType.GetNestedType("Entry", BindingFlags.NonPublic);
            var dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), entryType));
            foreach (FluxParticleGenerationSession.AssetRecord asset in records) dictionary.Add(asset.path, asset);
            Func<string, bool> delete = path => path != parent && AssetDatabase.DeleteAsset(path);
            analysis.GetMethod("Delete", InternalStatic).Invoke(null, new object[] { report, order, dependencies, dictionary, delete });
            Assert.AreEqual("Failed", report.items.Single(item => item.path == parent).status);
            Assert.AreEqual("Keep", report.items.Single(item => item.path == child).status);
            Assert.IsTrue(File.Exists(parent));
            Assert.IsTrue(File.Exists(child));
        }

        [Test]
        public void CyclesAndTheirDependenciesAreKept()
        {
            var report = new FluxParticleGenerationCleanupReport();
            foreach (var path in new[] { "a", "b", "child" }) report.items.Add(new FluxParticleGenerationCleanupReport.Item { path = path, status = "Candidate" });
            var dependencies = new Dictionary<string, string[]> { ["a"] = new[] { "b" }, ["b"] = new[] { "a", "child" } };
            var type = typeof(FluxParticleGenerationCleanup).Assembly.GetType("TpLab.Flux.FX.Editor.Build.FluxParticleGenerationCleanupAnalysis", true);
            var order = (IList)type.GetMethod("Order", InternalStatic).Invoke(null, new object[] { report, dependencies });
            Assert.AreEqual(0, order.Count);
            Assert.IsTrue(report.items.All(item => item.status == "Keep"));
        }

        [Test]
        public void ModifiedIncludeIsKeptEvenWhenTheSessionFinishesAfterEditing()
        {
            FluxParticleGenerationSession.EnsureFolder(_session.ShaderRoot);
            var path = _session.ShaderRoot + "/Edited.hlsl";
            File.WriteAllText(path, "float Value() { return 1; }");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            _session.Record(path, "ShaderInclude", "test");
            File.WriteAllText(path, "float Value() { return 2; }");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            _session.Dispose();
            var report = FluxParticleGenerationCleanup.CollectWithReport(_root);
            Assert.IsFalse(report.failed, string.Join("\n", report.diagnostics));
            Assert.AreEqual("Keep", report.items.Single(item => item.path == path).status);
            StringAssert.Contains("content", report.items.Single(item => item.path == path).reason);
            Assert.IsTrue(File.Exists(path));
        }

        [Test]
        public void ExplicitGenerationCanRecordARecreatedAssetWithANewGuid()
        {
            var oldGuid = AssetDatabase.AssetPathToGUID(_lookupPath);
            AssetDatabase.DeleteAsset(_materialPath);
            AssetDatabase.DeleteAsset(_lookupPath);
            using (var next = FluxParticleGenerationSession.Begin("Recreate", _root))
            {
                var replacement = next.LookupRoot + "/Replacement.asset";
                AssetDatabase.CreateAsset(new Texture2D(1, 1), replacement);
                Assert.IsEmpty(AssetDatabase.MoveAsset(replacement, _lookupPath));
                Assert.AreNotEqual(oldGuid, AssetDatabase.AssetPathToGUID(_lookupPath));
                next.Record(_lookupPath, "Lookup", "new generation");
            }
            _session.Dispose();
            var report = FluxParticleGenerationCleanup.CollectWithReport(_root);
            Assert.IsFalse(report.failed, string.Join("\n", report.diagnostics));
            Assert.AreEqual(1, report.DeletedCount);
        }

        [Test]
        public void AutomaticTriggerRequiresNewCompletionAndFull24Hours()
        {
            var type = typeof(FluxParticleGenerationCleanup).Assembly.GetType("TpLab.Flux.FX.Editor.Build.FluxParticleGenerationCleanupSchedule", true);
            var schedule = Activator.CreateInstance(type, true);
            var now = DateTime.UtcNow;
            type.GetField("completedRevision").SetValue(schedule, 2L);
            type.GetField("scannedRevision").SetValue(schedule, 1L);
            type.GetField("lastAutomaticScanUtc").SetValue(schedule, now.ToString("O"));
            var due = type.GetMethod("IsDue", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsFalse((bool)due.Invoke(schedule, new object[] { now.AddHours(24).AddTicks(-1) }));
            Assert.IsTrue((bool)due.Invoke(schedule, new object[] { now.AddHours(24) }));
            type.GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(schedule, null);
            schedule = type.GetMethod("Load", InternalStatic).Invoke(null, null);
            Assert.IsTrue((bool)due.Invoke(schedule, new object[] { now.AddDays(2) }));
            type.GetField("nextCandidateCheckUtc").SetValue(schedule, now.AddDays(3).ToString("O"));
            Assert.IsTrue((bool)due.Invoke(schedule, new object[] { now.AddDays(2) }), "New work does not wait for cache expiry");
            type.GetField("scannedRevision").SetValue(schedule, 2L);
            Assert.IsFalse((bool)due.Invoke(schedule, new object[] { now.AddDays(2) }));
            Assert.IsTrue((bool)due.Invoke(schedule, new object[] { now.AddDays(3) }), "Retention expiry needs no new generation");
            type.GetField("nextCandidateCheckUtc").SetValue(schedule, null);
            Assert.IsFalse((bool)due.Invoke(schedule, new object[] { now.AddDays(100) }));
        }

        void CreateReference()
        {
            _instance = new GameObject("Cleanup plan reference", typeof(MeshRenderer));
            _instance.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(_materialPath);
        }
    }
}
