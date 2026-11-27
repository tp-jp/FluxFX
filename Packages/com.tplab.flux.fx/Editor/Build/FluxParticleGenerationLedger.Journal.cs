using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    internal static partial class FluxParticleGenerationLedger
    {
        [Serializable]
        sealed class JournalRecord
        {
            public long sequence;
            public Entry asset;
        }

        [Serializable]
        sealed class JournalEnvelope
        {
            public string data;
            public string hash;
        }

        static readonly Dictionary<string, Ledger> _records = new Dictionary<string, Ledger>();

        internal static string JournalPath(string root) => PathFor(root) + ".journal";

        static string Stamp(string root) => FluxParticleGenerationStorage.Fingerprint(PathFor(root)) + "|" +
            FluxParticleGenerationStorage.Fingerprint(JournalPath(root));

        // Record完了時には変更分をディスクへ確定する。Domain Reloadや異常終了時も終了処理に依存しない。
        static void Append(FluxParticleGenerationSession session, FluxParticleGenerationSession.AssetRecord asset)
        {
            var root = session.Root;
            if (!_records.TryGetValue(root, out var ledger) || ledger.storageStamp != Stamp(root))
            {
                ledger = Load(root);
                _records[root] = ledger;
            }
            try
            {
                if (ledger.activeSessions.All(value => value.id != session.Id))
                {
                    ledger.activeSessions.Add(new ActiveSession { id = session.Id, editorSessionId = session.EditorSessionId });
                    Save(ledger);
                    _records[root] = ledger;
                }
                var previous = ledger.assets.FirstOrDefault(value => value.path == asset.path);
                var before = previous == null ? null : JsonUtility.ToJson(previous);
                Merge(ledger, asset, false);
                var entry = ledger.assets.First(value => value.path == asset.path);
                if (before == JsonUtility.ToJson(entry)) return;
                var record = new JournalRecord { sequence = checked(ledger.journalSequence + 1), asset = entry };
                var data = JsonUtility.ToJson(record);
                var envelope = new JournalEnvelope { data = data, hash = Hash128.Compute(data).ToString() };
                var bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(envelope) + "\n");
                using (var stream = new FileStream(JournalPath(root), FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                ledger.journalSequence = record.sequence;
                ledger.storageStamp = Stamp(root);
            }
            catch
            {
                _records.Remove(root);
                throw;
            }
        }

        static void Replay(Ledger ledger)
        {
            var path = JournalPath(ledger.root);
            if (!File.Exists(path)) return;
            try
            {
                var text = File.ReadAllText(path);
                if (text.Length != 0 && !text.EndsWith("\n", StringComparison.Ordinal))
                    throw new InvalidOperationException("Incomplete generation journal record.");
                foreach (var line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var envelope = JsonUtility.FromJson<JournalEnvelope>(line);
                    if (envelope == null || string.IsNullOrEmpty(envelope.data) ||
                        envelope.hash != Hash128.Compute(envelope.data).ToString())
                        throw new InvalidOperationException("Generation journal checksum mismatch.");
                    var record = JsonUtility.FromJson<JournalRecord>(envelope.data);
                    if (record == null || record.sequence <= 0 || !HasExpectedPath(ledger.root, record.asset) ||
                        record.asset.owners == null || record.asset.dependencies == null)
                        throw new InvalidOperationException("Invalid generation journal record.");
                    if (record.sequence <= ledger.journalSequence) continue;
                    if (record.sequence != ledger.journalSequence + 1)
                        throw new InvalidOperationException("Generation journal sequence is incomplete.");
                    Merge(ledger, record.asset, false);
                    ledger.journalSequence = record.sequence;
                }
            }
            catch (Exception error)
            {
                throw new InvalidOperationException("Cannot read generation journal: " + path + "\n" + error.Message, error);
            }
        }
    }
}
