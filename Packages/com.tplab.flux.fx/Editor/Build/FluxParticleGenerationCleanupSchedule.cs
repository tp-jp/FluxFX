using System;
using System.IO;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    // 時刻と生成完了の変更番号だけを確認し、待機中にAssetを列挙しません。
    [Serializable]
    internal sealed class FluxParticleGenerationCleanupSchedule
    {
        internal const string FilePath = FluxParticleGenerationStorage.DirectoryPath + "/CleanupSchedule.json";
        public int version = 2;
        public long completedRevision;
        public long scannedRevision;
        public string lastFullScanUtc;
        public string lastAutomaticScanUtc;
        public string nextCandidateCheckUtc;

        internal bool Pending => completedRevision > scannedRevision;

        internal bool IsDue(DateTime now) => (Pending || !string.IsNullOrEmpty(nextCandidateCheckUtc) &&
            now >= FluxParticleGenerationLedger.ParseUtc(nextCandidateCheckUtc)) && (string.IsNullOrEmpty(lastAutomaticScanUtc) ||
            now - FluxParticleGenerationLedger.ParseUtc(lastAutomaticScanUtc) >= TimeSpan.FromDays(1));

        internal static FluxParticleGenerationCleanupSchedule Load()
        {
            FluxParticleGenerationStorage.EnsureMigrated();
            if (!File.Exists(FilePath))
            {
                var existing = File.Exists(FluxParticleGenerationLedger.PathFor(FluxParticleGenerationSession.DefaultRoot)) ||
                    Directory.Exists(FluxParticleGenerationSession.ManifestRoot);
                return new FluxParticleGenerationCleanupSchedule { completedRevision = existing ? 1 : 0 };
            }
            return Parse(File.ReadAllText(FilePath), FilePath);
        }

        internal static FluxParticleGenerationCleanupSchedule Parse(string source, string path)
        {
            var value = JsonUtility.FromJson<FluxParticleGenerationCleanupSchedule>(source);
            if (value == null || (value.version != 1 && value.version != 2) || value.completedRevision < value.scannedRevision ||
                value.scannedRevision < 0 || (!string.IsNullOrEmpty(value.lastFullScanUtc) &&
                    FluxParticleGenerationLedger.ParseUtc(value.lastFullScanUtc) == DateTime.MaxValue) ||
                (!string.IsNullOrEmpty(value.lastAutomaticScanUtc) && FluxParticleGenerationLedger.ParseUtc(value.lastAutomaticScanUtc) == DateTime.MaxValue) ||
                (!string.IsNullOrEmpty(value.nextCandidateCheckUtc) && FluxParticleGenerationLedger.ParseUtc(value.nextCandidateCheckUtc) == DateTime.MaxValue))
            {
                throw new InvalidOperationException("Invalid cleanup schedule: " + path);
            }
            if (value.version == 1)
            {
                value.version = 2;
                value.lastAutomaticScanUtc = value.lastFullScanUtc;
                // 旧版で走査済みにした保持中Cacheも、次回起動で一度再評価します。
                value.completedRevision++;
            }
            return value;
        }

        internal void Save()
        {
            FluxParticleGenerationStorage.EnsureMigrated();
            FluxParticleGenerationLedger.WriteAtomic(FilePath, JsonUtility.ToJson(this, true));
        }
    }
}
