using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// 実際の管理情報に触れず、保存先移行の内容保持と失敗時の保護を検証します。
    /// </summary>
    public sealed class FluxParticleGenerationStorageTests
    {
        const BindingFlags InternalStatic = BindingFlags.NonPublic | BindingFlags.Static;
        const string Root = "Assets/FluxFX/Generated";
        const string Schedule = "{\"version\":2,\"completedRevision\":12,\"scannedRevision\":8," +
            "\"lastFullScanUtc\":\"2026-11-23T00:30:00Z\",\"lastAutomaticScanUtc\":\"2026-11-23T00:00:00Z\"," +
            "\"nextCandidateCheckUtc\":\"2026-11-24T03:00:00Z\"}";
        string _temporary;
        string _source;
        string _destination;
        Type _storage;

        string LedgerRelative => "Roots/" + Hash128.Compute(Root) + ".json";

        [SetUp]
        public void SetUp()
        {
            _temporary = Path.GetFullPath("Temp/FluxFX/GenerationStorageTests/" + Guid.NewGuid().ToString("N"));
            _source = _temporary + "/ProjectSettings/FluxFXGeneration";
            _destination = _temporary + "/UserSettings/FluxFXGeneration";
            _storage = typeof(FluxParticleGenerationSession).Assembly.GetType("TpLab.Flux.FX.Editor.Build.FluxParticleGenerationStorage", true);
        }

        [TearDown]
        public void TearDown()
        {
            // このテストが作成したTemp内の固有ディレクトリだけを削除します。
            var parent = Path.GetFullPath("Temp/FluxFX/GenerationStorageTests") + Path.DirectorySeparatorChar;
            Assert.That(_temporary.StartsWith(parent, StringComparison.OrdinalIgnoreCase));
            if (Directory.Exists(_temporary)) Directory.Delete(_temporary, true);
        }

        [Test]
        public void PreservesEveryByteIncludingOwnersDependenciesActiveSessionsAndRetention()
        {
            Write(_source, "CleanupSchedule.json", Schedule);
            Write(_source, LedgerRelative, Ledger(Root));
            var scheduleBytes = File.ReadAllBytes(_source + "/CleanupSchedule.json");
            var ledgerBytes = File.ReadAllBytes(_source + "/" + LedgerRelative);
            Migrate();
            CollectionAssert.AreEqual(scheduleBytes, File.ReadAllBytes(_destination + "/CleanupSchedule.json"));
            CollectionAssert.AreEqual(ledgerBytes, File.ReadAllBytes(_destination + "/" + LedgerRelative));
            Assert.IsFalse(Directory.Exists(_source));
            Migrate();
            CollectionAssert.AreEqual(ledgerBytes, File.ReadAllBytes(_destination + "/" + LedgerRelative));
        }

        [Test]
        public void PreservesPendingRequestAndExactTwentyFourHourBoundary()
        {
            Write(_source, "CleanupSchedule.json", Schedule);
            Migrate();
            var type = ScheduleType();
            var value = type.GetMethod("Parse", InternalStatic).Invoke(null,
                new object[] { File.ReadAllText(_destination + "/CleanupSchedule.json"), _destination + "/CleanupSchedule.json" });
            var method = type.GetMethod("IsDue", BindingFlags.NonPublic | BindingFlags.Instance);
            var last = new DateTime(2026, 11, 23, 0, 0, 0, DateTimeKind.Utc);
            Assert.IsFalse((bool)method.Invoke(value, new object[] { last.AddDays(1).AddTicks(-1) }));
            Assert.IsTrue((bool)method.Invoke(value, new object[] { last.AddDays(1) }));
            Assert.AreEqual(12L, type.GetField("completedRevision").GetValue(value));
            Assert.AreEqual(8L, type.GetField("scannedRevision").GetValue(value));
            Assert.AreEqual("2026-11-24T03:00:00Z", type.GetField("nextCandidateCheckUtc").GetValue(value));
        }

        [Test]
        public void MultipleRegisteredRootsAreMigratedWithoutScanningAssets()
        {
            var custom = "Assets/CustomGenerated";
            Write(_source, LedgerRelative, Ledger(Root));
            var relative = "Roots/" + Hash128.Compute(custom) + ".json";
            Write(_source, relative, Ledger(custom));
            Migrate();
            Assert.AreEqual(Ledger(custom), File.ReadAllText(_destination + "/" + relative));
            Assert.IsFalse(Directory.Exists(_source));
        }

        [Test]
        public void IdenticalPartiallyCopiedDestinationCanBeResumed()
        {
            Write(_source, "CleanupSchedule.json", Schedule);
            Write(_source, LedgerRelative, Ledger(Root));
            Write(_destination, "CleanupSchedule.json", Schedule);
            Migrate();
            Assert.AreEqual(Ledger(Root), File.ReadAllText(_destination + "/" + LedgerRelative));
            Assert.IsFalse(Directory.Exists(_source));
        }

        [TestCase("CleanupSchedule.json")]
        [TestCase("ledger")]
        public void ConflictingDestinationStopsBeforeAnyCopyOrDelete(string target)
        {
            var relative = target == "ledger" ? LedgerRelative : target;
            Write(_source, "CleanupSchedule.json", Schedule);
            Write(_source, LedgerRelative, Ledger(Root));
            Write(_destination, relative, "different destination data");
            var error = Assert.Throws<InvalidOperationException>(Migrate);
            StringAssert.Contains(Path.GetFullPath(_destination), error.Message);
            Assert.AreEqual(Schedule, File.ReadAllText(_source + "/CleanupSchedule.json"));
            Assert.AreEqual(Ledger(Root), File.ReadAllText(_source + "/" + LedgerRelative));
            Assert.AreEqual("different destination data", File.ReadAllText(_destination + "/" + relative));
            Assert.AreEqual(1, Directory.GetFiles(_destination, "*", SearchOption.AllDirectories).Length);
        }

        [TestCase("{broken")]
        [TestCase("{\"version\":99}")]
        [TestCase("{\"version\":2,\"completedRevision\":1,\"scannedRevision\":2}")]
        [TestCase("{\"version\":2,\"lastAutomaticScanUtc\":\"invalid\"}")]
        public void InvalidScheduleIsNeverReplacedWithDefaults(string source)
        {
            Write(_source, "CleanupSchedule.json", source);
            Write(_source, LedgerRelative, Ledger(Root));
            var error = Assert.Throws<InvalidOperationException>(Migrate);
            StringAssert.Contains("CleanupSchedule.json", error.Message);
            Assert.AreEqual(source, File.ReadAllText(_source + "/CleanupSchedule.json"));
            Assert.IsFalse(Directory.Exists(_destination));
        }

        [Test]
        public void InvalidLedgerIdentityStopsBeforeMovingValidSchedule()
        {
            Write(_source, "CleanupSchedule.json", Schedule);
            Write(_source, LedgerRelative, Ledger("Assets/OtherRoot"));
            Assert.Throws<InvalidOperationException>(Migrate);
            Assert.IsTrue(File.Exists(_source + "/CleanupSchedule.json"));
            Assert.IsFalse(Directory.Exists(_destination));
        }

        [Test]
        public void CorruptLedgerReportsItsPathWithoutMovingValidSchedule()
        {
            Write(_source, "CleanupSchedule.json", Schedule);
            Write(_source, LedgerRelative, "{broken");
            var error = Assert.Throws<InvalidOperationException>(Migrate);
            StringAssert.Contains(Path.GetFileName(LedgerRelative), error.Message);
            Assert.IsTrue(File.Exists(_source + "/CleanupSchedule.json"));
            Assert.IsFalse(Directory.Exists(_destination));
        }

        [Test]
        public void PartiallyRemovedSourcesKeepAlreadyMigratedFiles()
        {
            Write(_destination, "CleanupSchedule.json", Schedule);
            Write(_destination, LedgerRelative, Ledger(Root));
            Write(_source, LedgerRelative, Ledger(Root));
            Migrate();
            Assert.AreEqual(Schedule, File.ReadAllText(_destination + "/CleanupSchedule.json"));
            Assert.AreEqual(Ledger(Root), File.ReadAllText(_destination + "/" + LedgerRelative));
            Assert.IsFalse(Directory.Exists(_source));
        }

        [Test]
        public void CopyFailureLeavesAllSourcesAndCanBeRetried()
        {
            Write(_source, "CleanupSchedule.json", Schedule);
            Write(_source, LedgerRelative, Ledger(Root));
            Write(_destination, "Roots", "blocks directory creation");
            Assert.Throws<InvalidOperationException>(Migrate);
            Assert.AreEqual(Schedule, File.ReadAllText(_source + "/CleanupSchedule.json"));
            Assert.AreEqual(Ledger(Root), File.ReadAllText(_source + "/" + LedgerRelative));
            File.Delete(_destination + "/Roots");
            Migrate();
            Assert.IsFalse(Directory.Exists(_source));
            Assert.AreEqual(Ledger(Root), File.ReadAllText(_destination + "/" + LedgerRelative));
            Assert.IsEmpty(Directory.GetFiles(_destination, "*.tmp", SearchOption.AllDirectories));
        }

        [Test]
        public void UnknownFilesAndSubdirectoriesAreNotDeleted()
        {
            Write(_source, "CleanupSchedule.json", Schedule);
            Write(_source, LedgerRelative, Ledger(Root));
            Write(_source, "Roots/notes.txt", "keep roots");
            Write(_source, "Other/data.json", "unmanaged");
            Write(_source, "CleanupSchedule.json.tmp", "incomplete write");
            Migrate();
            Assert.AreEqual("keep roots", File.ReadAllText(_source + "/Roots/notes.txt"));
            Assert.AreEqual("unmanaged", File.ReadAllText(_source + "/Other/data.json"));
            Assert.AreEqual("incomplete write", File.ReadAllText(_source + "/CleanupSchedule.json.tmp"));
            Assert.IsFalse(File.Exists(_source + "/CleanupSchedule.json"));
            Assert.IsFalse(File.Exists(_source + "/" + LedgerRelative));
        }

        [Test]
        public void NoLegacyDirectoryDoesNotCreateOrChangeMetadata()
        {
            Migrate();
            Assert.IsFalse(Directory.Exists(_destination));
            Write(_destination, "CleanupSchedule.json", Schedule);
            Migrate();
            Assert.AreEqual(Schedule, File.ReadAllText(_destination + "/CleanupSchedule.json"));
        }

        [Test]
        public void LegacyScheduleVersionIsPreservedForNormalLoadUpgrade()
        {
            var source = "{\"version\":1,\"completedRevision\":2,\"scannedRevision\":2,\"lastFullScanUtc\":\"2026-11-23T00:00:00Z\"}";
            Write(_source, "CleanupSchedule.json", source);
            Migrate();
            Assert.AreEqual(source, File.ReadAllText(_destination + "/CleanupSchedule.json"));
            var type = ScheduleType();
            var value = type.GetMethod("Parse", InternalStatic).Invoke(null, new object[] { source, "test" });
            Assert.AreEqual(2, type.GetField("version").GetValue(value));
            Assert.AreEqual("2026-11-23T00:00:00Z", type.GetField("lastAutomaticScanUtc").GetValue(value));
            Assert.AreEqual(3L, type.GetField("completedRevision").GetValue(value));
        }

        void Migrate()
        {
            try { _storage.GetMethod("Migrate", InternalStatic).Invoke(null, new object[] { _source, _destination }); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }

        static Type ScheduleType() => typeof(FluxParticleGenerationSession).Assembly.GetType(
            "TpLab.Flux.FX.Editor.Build.FluxParticleGenerationCleanupSchedule", true);

        static void Write(string directory, string relative, string contents)
        {
            var path = directory + "/" + relative;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, contents);
        }

        static string Ledger(string root) => "{\"version\":1,\"root\":\"" + root + "\",\"assets\":[{" +
            "\"path\":\"" + root + "/Cache/Shaders/test.shader\",\"guid\":\"0123456789abcdef0123456789abcdef\",\"kind\":\"Shader\"," +
            "\"contentId\":\"content\",\"retainUntilUtc\":\"2026-11-25T00:00:00Z\",\"lastUsedUtc\":\"2026-11-23T00:00:00Z\"," +
            "\"owners\":[\"owner-a\",\"owner-b\"],\"dependencies\":[\"" + root + "/Cache/Shaders/include.hlsl\"]}]," +
            "\"activeSessions\":[{\"id\":\"0123456789abcdef0123456789abcdef\",\"editorSessionId\":\"editor-session\"}]}";
    }
}
