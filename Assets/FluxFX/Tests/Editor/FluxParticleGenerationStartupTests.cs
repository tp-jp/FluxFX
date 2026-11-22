using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleGenerationStartupTests
    {
        const BindingFlags InternalStatic = BindingFlags.Static | BindingFlags.NonPublic;
        const string Preference = "TpLab.Flux.FX.AutomaticGenerationCleanup";
        const string SchedulePath = "ProjectSettings/FluxFXGeneration/CleanupSchedule.json";
        string _root;
        string _key;
        string _scheduleBefore;
        bool _hadPreference;
        bool _automatic;
        FluxParticleGenerationSession _session;
        Type _startup;

        [SetUp]
        public void SetUp()
        {
            _hadPreference = EditorPrefs.HasKey(Preference);
            _automatic = FluxParticleGenerationCleanup.AutomaticCleanupEnabled;
            _scheduleBefore = File.Exists(SchedulePath) ? File.ReadAllText(SchedulePath) : null;
            Directory.CreateDirectory(Path.GetDirectoryName(SchedulePath));
            File.WriteAllText(SchedulePath, "{\"version\":2,\"completedRevision\":1,\"scannedRevision\":0}");
            FluxParticleGenerationCleanup.AutomaticCleanupEnabled = true;
            _root = FluxParticleTestPaths.TemporaryRoot + "/StartupCleanup_" + Guid.NewGuid().ToString("N");
            _key = "FluxFX.Test.Startup." + Guid.NewGuid().ToString("N");
            _startup = typeof(FluxParticleGenerationCleanup).Assembly.GetType("TpLab.Flux.FX.Editor.Build.FluxParticleGenerationCleanupStartup", true);
            _session = FluxParticleGenerationSession.Begin("Startup cleanup test", _root);
            FluxParticleGenerationSession.EnsureFolder(_session.MaterialRoot);
            FluxParticleGenerationSession.EnsureFolder(_session.LookupRoot);
            var texture = new Texture2D(1, 1);
            var lookup = _session.LookupRoot + "/Lookup.asset";
            AssetDatabase.CreateAsset(texture, lookup);
            var material = _session.MaterialRoot + "/Render.mat";
            AssetDatabase.CreateAsset(new Material(Shader.Find("Unlit/Texture")) { mainTexture = texture }, material);
            _session.Record(lookup, "Lookup", "startup-test");
            _session.Record(material, "Material", "startup-test");
            _session.Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            _session?.Dispose();
            AssetDatabase.DeleteAsset(_root);
            FluxParticleTestPaths.RemoveEmptyGenerationLedger(_root);
            File.Delete(_session.ManifestPath);
            SessionState.EraseInt(_key);
            if (_scheduleBefore == null) File.Delete(SchedulePath);
            else File.WriteAllText(SchedulePath, _scheduleBefore);
            FluxParticleGenerationCleanup.AutomaticCleanupEnabled = _automatic;
            if (!_hadPreference) EditorPrefs.DeleteKey(Preference);
        }

        [Test]
        public void DefaultsOnButStoredOffIsPreserved()
        {
            EditorPrefs.DeleteKey(Preference);
            Assert.IsTrue(FluxParticleGenerationCleanup.AutomaticCleanupEnabled);
            FluxParticleGenerationCleanup.AutomaticCleanupEnabled = false;
            Assert.IsFalse(FluxParticleGenerationCleanup.AutomaticCleanupEnabled);
            Assert.IsNull(Collect(null));
        }

        [Test]
        public void FirstReadyOpportunityRunsOnceWithoutTimeDelayAcrossReloadInitialization()
        {
            Initialize(false);
            Assert.IsFalse(Enter(false, false, false));
            Assert.IsFalse(Enter(true, true, false));
            Initialize(true); // 同じSessionStateを読み直すドメインリロード
            Assert.IsTrue(Enter(true, false, false));
            Initialize(false);
            Assert.IsFalse(Enter(true, false, false));
        }

        [Test]
        public void UpgradeInAnAlreadyOpenEditorDoesNotRun()
        {
            Initialize(true);
            Assert.IsFalse(Enter(true, false, false));
        }

        [Test]
        public void StartingOtherWorkConsumesOpportunityInsteadOfDelaying()
        {
            Initialize(false);
            Assert.IsFalse(Enter(false, true, true));
            Assert.IsFalse(Enter(true, false, false));
            Initialize(false);
            Assert.IsFalse(Enter(true, false, false));
        }

        [Test]
        public void ToggleAndRequestsDoNotRearmConsumedStartup()
        {
            Initialize(false);
            Assert.IsTrue(Enter(true, false, false));
            FluxParticleGenerationCleanup.AutomaticCleanupEnabled = false;
            FluxParticleGenerationCleanup.Request();
            FluxParticleGenerationCleanup.AutomaticCleanupEnabled = true;
            Initialize(false);
            Assert.IsFalse(Enter(true, false, false));
        }

        [Test]
        public void StartupCollectionPersistsThrottleAndSchedulesRetainedCache()
        {
            var report = Collect(null);
            Assert.IsFalse(report.failed, string.Join("\n", report.diagnostics));
            Assert.AreEqual(1, report.DeletedCount);
            var state = File.ReadAllText(SchedulePath);
            StringAssert.Contains("lastAutomaticScanUtc", state);
            var schedule = LoadSchedule();
            Assert.IsNotEmpty((string)schedule.GetType().GetField("nextCandidateCheckUtc").GetValue(schedule));
            FluxParticleGenerationCleanup.Request();
            FluxParticleGenerationCleanup.AutomaticCleanupEnabled = true; // 永続記録を再読込
            Assert.IsNull(Collect(null), "Another startup within 24 hours cannot scan");
        }

        [Test]
        public void CancellationBeforeDeletionKeepsAssetsReservationAndDailyLimit()
        {
            var report = Collect((message, value) => message == "Removing unused generated assets");
            Assert.IsTrue(report.cancelled);
            Assert.IsFalse(report.failed);
            Assert.IsTrue(report.fullScan);
            Assert.AreEqual(0, report.DeletedCount);
            Assert.IsTrue(File.Exists(_session.MaterialRoot + "/Render.mat"));
            Assert.IsTrue(File.Exists(_session.LookupRoot + "/Lookup.asset"));
            var schedule = LoadSchedule();
            Assert.IsTrue((bool)schedule.GetType().GetProperty("Pending", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(schedule));
            Assert.IsNull(Collect(null));
        }

        [Test]
        public void CancellationBeforeScanDoesNotConsumeDailyLimit()
        {
            var report = Collect((message, value) => true);
            Assert.IsTrue(report.cancelled);
            Assert.IsFalse(report.fullScan);
            var schedule = LoadSchedule();
            Assert.IsTrue(string.IsNullOrEmpty((string)schedule.GetType().GetField("lastAutomaticScanUtc").GetValue(schedule)));
        }

        [Test]
        public void LegacyScheduleRetainsPreviousScanLimitAndQueuesReview()
        {
            var now = DateTime.UtcNow;
            File.WriteAllText(SchedulePath, "{\"version\":1,\"completedRevision\":4,\"scannedRevision\":4,\"lastFullScanUtc\":\"" + now.ToString("O") + "\"}");
            var schedule = LoadSchedule();
            var type = schedule.GetType();
            Assert.AreEqual(2, type.GetField("version").GetValue(schedule));
            var due = type.GetMethod("IsDue", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsFalse((bool)due.Invoke(schedule, new object[] { now.AddHours(24).AddTicks(-1) }));
            Assert.IsTrue((bool)due.Invoke(schedule, new object[] { now.AddHours(24) }));
        }

        void Initialize(bool alreadyOpen) => _startup.GetMethod("Initialize", InternalStatic).Invoke(null, new object[] { _key, alreadyOpen });

        bool Enter(bool ready, bool importing, bool interrupted) =>
            (bool)_startup.GetMethod("TryEnter", InternalStatic).Invoke(null, new object[] { _key, ready, importing, interrupted });

        FluxParticleGenerationCleanupReport Collect(Func<string, float, bool> progress) =>
            (FluxParticleGenerationCleanupReport)typeof(FluxParticleGenerationCleanup).GetMethod("CollectAtStartup", InternalStatic)
                .Invoke(null, new object[] { _root, progress });

        static object LoadSchedule() => typeof(FluxParticleGenerationCleanup).Assembly
            .GetType("TpLab.Flux.FX.Editor.Build.FluxParticleGenerationCleanupSchedule", true).GetMethod("Load", InternalStatic).Invoke(null, null);
    }
}
