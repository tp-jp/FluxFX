using System;
using System.IO;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    // 時刻と生成完了の変更番号だけを確認し、待機中にAssetを列挙しません。
    [Serializable]
    internal sealed class FluxParticleGenerationCleanupSchedule
    {
        internal const string FilePath = "ProjectSettings/FluxFXGeneration/CleanupSchedule.json";
        public int version = 1;
        public long completedRevision;
        public long scannedRevision;
        public string lastFullScanUtc;
        public string nextCandidateCheckUtc;

        internal bool Pending => completedRevision > scannedRevision;

        internal bool IsDue(DateTime now) => Pending && (string.IsNullOrEmpty(nextCandidateCheckUtc) ||
            now >= FluxParticleGenerationLedger.ParseUtc(nextCandidateCheckUtc)) && (string.IsNullOrEmpty(lastFullScanUtc) ||
            now - FluxParticleGenerationLedger.ParseUtc(lastFullScanUtc) >= TimeSpan.FromDays(1));

        internal static FluxParticleGenerationCleanupSchedule Load()
        {
            if (!File.Exists(FilePath)) return new FluxParticleGenerationCleanupSchedule();
            var value = JsonUtility.FromJson<FluxParticleGenerationCleanupSchedule>(File.ReadAllText(FilePath));
            if (value == null || value.version != 1 || value.completedRevision < value.scannedRevision ||
                value.scannedRevision < 0 || (!string.IsNullOrEmpty(value.lastFullScanUtc) &&
                    FluxParticleGenerationLedger.ParseUtc(value.lastFullScanUtc) == DateTime.MaxValue) ||
                (!string.IsNullOrEmpty(value.nextCandidateCheckUtc) && FluxParticleGenerationLedger.ParseUtc(value.nextCandidateCheckUtc) == DateTime.MaxValue))
            {
                throw new InvalidOperationException("Invalid cleanup schedule: " + FilePath);
            }
            return value;
        }

        internal void Save() => FluxParticleGenerationLedger.WriteAtomic(FilePath, JsonUtility.ToJson(this, true));
    }
}
