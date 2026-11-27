using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    // 所有情報はLibraryの再作成を跨いで保持し、実行履歴とは別に集約します。
    internal static partial class FluxParticleGenerationLedger
    {
        internal const string DirectoryPath = FluxParticleGenerationStorage.DirectoryPath + "/Roots";

        [Serializable]
        internal sealed class Entry : FluxParticleGenerationSession.AssetRecord
        {
            public string contentId;
            public string retainUntilUtc;
        }

        [Serializable]
        internal sealed class ActiveSession
        {
            public string id;
            public string editorSessionId;
        }

        [Serializable]
        internal sealed class Ledger
        {
            public int version = 1;
            public long journalSequence;
            [NonSerialized]
            internal string storageStamp;
            public string root;
            public List<Entry> assets = new List<Entry>();
            public List<ActiveSession> activeSessions = new List<ActiveSession>();
        }

        internal static string PathFor(string root) => DirectoryPath + "/" + Hash128.Compute(root) + ".json";

        internal static Ledger Load(string root)
        {
            FluxParticleGenerationStorage.EnsureMigrated();
            root = FluxParticleGenerationSession.ValidateRoot(root);
            var path = PathFor(root);
            if (!File.Exists(path) && File.Exists(JournalPath(root)))
                throw new InvalidOperationException("Generation ledger snapshot is missing: " + path);
            return File.Exists(path) ? Read(path) : new Ledger { root = root, storageStamp = Stamp(root) };
        }

        internal static Ledger[] LoadAll()
        {
            FluxParticleGenerationStorage.EnsureMigrated();
            if (Directory.Exists(DirectoryPath))
            {
                foreach (var journal in Directory.GetFiles(DirectoryPath, "*.json.journal"))
                {
                    var snapshot = journal.Substring(0, journal.Length - ".journal".Length);
                    if (!File.Exists(snapshot)) throw new InvalidOperationException("Generation ledger snapshot is missing: " + snapshot);
                }
            }
            return Directory.Exists(DirectoryPath) ? Directory.GetFiles(DirectoryPath, "*.json").Select(Read).ToArray() : Array.Empty<Ledger>();
        }

        internal static void Begin(FluxParticleGenerationSession session)
        {
            var ledger = Load(session.Root);
            ledger.activeSessions.Add(new ActiveSession { id = session.Id, editorSessionId = session.EditorSessionId });
            Save(ledger);
        }

        internal static void Record(FluxParticleGenerationSession session, FluxParticleGenerationSession.AssetRecord asset)
        {
            Append(session, asset);
        }

        internal static void Finish(FluxParticleGenerationSession session)
        {
            var ledger = Load(session.Root);
            foreach (var asset in session.Assets)
            {
                // 終了が遅れた旧セッションから、新しい生成処理の所有情報へ巻き戻しません。
                Merge(ledger, asset, true);
                if (session.State == "Failed" || session.State == "Interrupted")
                {
                    var entry = ledger.assets.First(value => value.path == asset.path);
                    entry.retainUntilUtc = Later(entry.retainUntilUtc, RetainUntil(session));
                }
            }
            ledger.activeSessions.RemoveAll(value => value.id == session.Id);
            Save(ledger);
        }

        // 明示的な分析時だけ旧履歴を取り込みます。名前やフォルダから所有を推測しません。
        internal static Ledger Migrate(string root, IEnumerable<FluxParticleGenerationSession> sessions)
        {
            var ledger = Load(root);
            var before = JsonUtility.ToJson(ledger);
            foreach (var session in sessions.Where(value => value.Root == root))
            {
                foreach (var asset in session.Assets)
                {
                    if (!Matches(root, asset)) continue;
                    Merge(ledger, asset, true);
                    if (session.State == "Failed" || session.State == "Interrupted")
                    {
                        var entry = ledger.assets.First(value => value.path == asset.path);
                        entry.retainUntilUtc = Later(entry.retainUntilUtc, RetainUntil(session));
                    }
                }
                if (session.State == "Running")
                {
                    if (ledger.activeSessions.All(value => value.id != session.Id))
                    {
                        ledger.activeSessions.Add(new ActiveSession { id = session.Id, editorSessionId = session.EditorSessionId });
                    }
                }
                else
                {
                    ledger.activeSessions.RemoveAll(value => value.id == session.Id);
                }
            }
            // SessionStateのEditor識別子はドメインリロードでは変わりません。
            // 別のEditor実行で取り残された印だけを復旧し、時間経過だけでは解除しません。
            ledger.activeSessions.RemoveAll(value => value.editorSessionId != FluxParticleGenerationSession.CurrentEditorSessionId);
            if (JsonUtility.ToJson(ledger) != before)
            {
                Save(ledger);
            }
            return ledger;
        }

        internal static bool Matches(string root, FluxParticleGenerationSession.AssetRecord asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.guid) || !HasExpectedPath(root, asset)) return false;
            if (AssetDatabase.AssetPathToGUID(asset.path) != asset.guid) return false;
            var type = AssetDatabase.GetMainAssetTypeAtPath(asset.path);
            if (asset is Entry entry && !string.IsNullOrEmpty(entry.contentId) &&
                (asset.kind == "Shader" || asset.kind == "ShaderInclude") && File.Exists(asset.path) &&
                Hash128.Compute(File.ReadAllText(asset.path)).ToString() != entry.contentId) return false;
            return asset.kind == "Shader" && type == typeof(Shader) || asset.kind == "ShaderInclude" && type == typeof(ShaderInclude) ||
                asset.kind == "Material" && type == typeof(Material) || asset.kind == "Lookup" && type == typeof(Texture2D) ||
                asset.kind == "Mesh" && type == typeof(Mesh);
        }

        internal static bool HasExpectedPath(string root, FluxParticleGenerationSession.AssetRecord asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.path)) return false;
            try
            {
                if (FluxParticleGenerationSession.ValidateRoot(asset.path) != asset.path) return false;
            }
            catch (ArgumentException) { return false; }
            var directory = Path.GetDirectoryName(asset.path)?.Replace('\\', '/');
            if (asset.kind == "Shader" || asset.kind == "ShaderInclude") return directory == root + "/Cache/Shaders";
            if (asset.kind == "Lookup") return directory == root + "/Cache/Lookups";
            if (asset.kind == "Mesh") return directory == root + "/Cache/Meshes";
            var prefix = root + "/Sessions/";
            if (asset.kind != "Material" || !directory.StartsWith(prefix, StringComparison.Ordinal)) return false;
            var parts = directory.Substring(prefix.Length).Split('/');
            return parts.Length == 2 && parts[1] == "Materials" && Guid.TryParseExact(parts[0], "N", out _);
        }

        internal static void Save(Ledger ledger)
        {
            FluxParticleGenerationStorage.EnsureMigrated();
            if (ledger.storageStamp != null && ledger.storageStamp != Stamp(ledger.root))
                throw new InvalidOperationException("Generation ledger changed before checkpoint: " + PathFor(ledger.root));
            _records.Remove(ledger.root);
            WriteAtomic(PathFor(ledger.root), JsonUtility.ToJson(ledger, true));
            // Snapshotを先に確定する。削除前に中断しても連番で二重適用を防ぐ。
            File.Delete(JournalPath(ledger.root));
            ledger.storageStamp = Stamp(ledger.root);
        }

        internal static void Compact(Ledger ledger, IEnumerable<FluxParticleGenerationSession> sessions)
        {
            ledger.assets.RemoveAll(asset => !File.Exists(asset.path) && !Directory.Exists(asset.path));
            Save(ledger);
            foreach (var session in sessions.Where(value => value.Root == ledger.root && value.State != "Running" &&
                value.EditorSessionId != FluxParticleGenerationSession.CurrentEditorSessionId))
            {
                // 現在のEditorが持つセッションハンドルは保ち、前回以前の移行済み履歴だけを削除します。
                if (session.Assets.All(asset => !File.Exists(asset.path) || ledger.assets.Any(entry =>
                    entry.path == asset.path && entry.guid == asset.guid && entry.kind == asset.kind)))
                {
                    File.Delete(session.ManifestPath);
                }
            }
            if (ledger.assets.Count == 0 && ledger.activeSessions.Count == 0)
            {
                File.Delete(PathFor(ledger.root));
            }
        }

        internal static DateTime ParseUtc(string text)
        {
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value)
                ? value.ToUniversalTime() : DateTime.MaxValue;
        }

        internal static void WriteAtomic(string path, string source)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, source);
            const int attempts = 6;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Replace(temporary, path, null);
                    }
                    else
                    {
                        File.Move(temporary, path);
                    }
                    return;
                }
                catch (IOException) when (attempt < attempts - 1)
                {
                    System.Threading.Thread.Sleep(20 * (attempt + 1));
                }
            }
        }

        static Ledger Read(string path)
        {
            var ledger = Parse(File.ReadAllText(path), path, DirectoryPath);
            Replay(ledger);
            ledger.storageStamp = Stamp(ledger.root);
            return ledger;
        }

        // 移行元も通常の読込と同じスキーマ・生成ルート・ファイル名で検証します。
        internal static Ledger Parse(string source, string path, string directory)
        {
            try
            {
                var ledger = JsonUtility.FromJson<Ledger>(source);
                if (ledger == null || ledger.version != 1 || ledger.journalSequence < 0 || ledger.assets == null || ledger.activeSessions == null ||
                    Path.GetFullPath(directory + "/" + Hash128.Compute(FluxParticleGenerationSession.ValidateRoot(ledger.root)) + ".json") != Path.GetFullPath(path) ||
                    ledger.assets.Any(asset => !HasExpectedPath(ledger.root, asset)) ||
                    ledger.assets.Select(asset => asset.path).Distinct().Count() != ledger.assets.Count ||
                    ledger.activeSessions.Any(value => value == null || !Guid.TryParseExact(value.id, "N", out _) || string.IsNullOrEmpty(value.editorSessionId)))
                {
                    throw new InvalidOperationException("Invalid ownership ledger.");
                }
                return ledger;
            }
            catch (Exception error)
            {
                throw new InvalidOperationException("Cannot read generation ledger: " + path + "\n" + error.Message, error);
            }
        }

        static void Merge(Ledger ledger, FluxParticleGenerationSession.AssetRecord asset, bool migration)
        {
            if (!HasExpectedPath(ledger.root, asset)) throw new InvalidOperationException("Invalid generated asset path: " + asset?.path);
            var entry = ledger.assets.FirstOrDefault(value => value.path == asset.path);
            if (entry != null && (!string.IsNullOrEmpty(entry.guid) && entry.guid != asset.guid || entry.kind != asset.kind))
            {
                if (migration) return;
                // 新しい生成処理による明示的なRecordだけが、再作成したAssetの所有情報を更新できます。
                // 旧履歴からの移行や整理では異なるGUIDを引き継ぎません。
                ledger.assets.Remove(entry);
                entry = null;
            }
            if (entry == null)
            {
                entry = new Entry { path = asset.path, kind = asset.kind };
                ledger.assets.Add(entry);
            }
            entry.guid = asset.guid;
            entry.lastUsedUtc = Later(entry.lastUsedUtc, asset.lastUsedUtc);
            entry.owners = entry.owners.Union(asset.owners ?? new List<string>()).ToList();
            entry.dependencies = entry.dependencies.Union(asset.dependencies ?? new List<string>()).ToList();
            if (string.IsNullOrEmpty(entry.contentId) && (asset.kind == "Shader" || asset.kind == "ShaderInclude"))
            {
                entry.contentId = asset is Entry recorded && !string.IsNullOrEmpty(recorded.contentId)
                    ? recorded.contentId : File.Exists(asset.path) ? Hash128.Compute(File.ReadAllText(asset.path)).ToString() : null;
            }
        }

        static string Later(string first, string second) => string.IsNullOrEmpty(first) || ParseUtc(second) > ParseUtc(first) ? second : first;

        static string RetainUntil(FluxParticleGenerationSession session) =>
            (session.EndedUtc >= DateTime.MaxValue.AddDays(-1) ? DateTime.MaxValue : session.EndedUtc.AddDays(1)).ToString("O");
    }
}
