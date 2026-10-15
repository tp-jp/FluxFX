using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 生成Assetの参照と生成セッションの状態を記録します。
    /// </summary>
    public sealed class FluxParticleGenerationSession : IDisposable
    {
        public const string DefaultRoot = "Assets/FluxFX/Generated";
        public const string ManifestRoot = "Library/FluxFX/GenerationSessions";

        [Serializable]
        public sealed class AssetRecord
        {
            public string path;
            public string guid;
            public string kind;
            public string lastUsedUtc;
            public List<string> owners = new List<string>();
        }

        [Serializable]
        public sealed class Manifest
        {
            public int version = 1;
            public string id;
            public string purpose;
            public string editorSessionId;
            public string root;
            public string state = "Running";
            public string startedUtc;
            public string endedUtc;
            public string error;
            public List<AssetRecord> assets = new List<AssetRecord>();
        }

        Manifest manifest;

        /// <summary>
        /// セッションの識別子
        /// </summary>
        public string Id => manifest.id;

        /// <summary>
        /// 実行目的
        /// </summary>
        public string Purpose => manifest.purpose;

        /// <summary>
        /// セッションの状態
        /// </summary>
        public string State => manifest.state;

        /// <summary>
        /// 共有Shaderの保存先
        /// </summary>
        public string ShaderRoot => manifest.root + "/Cache/Shaders";

        /// <summary>
        /// 共有Lookupの保存先
        /// </summary>
        public string LookupRoot => manifest.root + "/Cache/Lookups";

        /// <summary>
        /// このセッションのMaterial保存先
        /// </summary>
        public string MaterialRoot => manifest.root + "/Sessions/" + Id + "/Materials";

        /// <summary>
        /// 管理情報の保存先
        /// </summary>
        public string ManifestPath => ManifestRoot + "/" + Id + ".json";

        internal string EditorSessionId => manifest.editorSessionId;

        internal static string CurrentEditorSessionId
        {
            get
            {
                const string key = "FluxFX.Generation.EditorInstance";
                var id = SessionState.GetString(key, "");
                if (!string.IsNullOrEmpty(id)) return id;
                id = Guid.NewGuid().ToString("N");
                SessionState.SetString(key, id);
                return id;
            }
        }

        FluxParticleGenerationSession(Manifest manifest)
        {
            this.manifest = manifest;
        }

        /// <summary>
        /// 生成セッションを開始します。
        /// </summary>
        public static FluxParticleGenerationSession Begin(string purpose, string root = DefaultRoot)
        {
            if (string.IsNullOrWhiteSpace(purpose)) throw new ArgumentException("Generation purpose is required.", nameof(purpose));
            var session = new FluxParticleGenerationSession(new Manifest
            {
                id = Guid.NewGuid().ToString("N"),
                purpose = purpose,
                editorSessionId = CurrentEditorSessionId,
                root = ValidateRoot(root),
                startedUtc = DateTime.UtcNow.ToString("O")
            });
            session.Save();
            return session;
        }

        /// <summary>
        /// 保存済みのセッションを読み込みます。
        /// </summary>
        public static FluxParticleGenerationSession Load(string id)
        {
            if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("Invalid generation session ID.", nameof(id));
            var record = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestRoot + "/" + id + ".json"));
            if (record == null || record.version != 1 || record.id != id) throw new InvalidOperationException("Invalid generation manifest.");
            record.root = ValidateRoot(record.root);
            return new FluxParticleGenerationSession(record);
        }

        /// <summary>
        /// セッションが使用するAssetと利用元を記録します。
        /// </summary>
        public void Record(string path, string kind, string owner)
        {
            RequireRunning();
            if (path != ValidateRoot(path)) throw new ArgumentException("Asset path must be normalized.", nameof(path));
            if (!path.StartsWith(ShaderRoot + "/", StringComparison.Ordinal) &&
                !path.StartsWith(LookupRoot + "/", StringComparison.Ordinal) &&
                !path.StartsWith(MaterialRoot + "/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Asset is outside this generation session.", nameof(path));
            }
            var record = manifest.assets.FirstOrDefault(asset => asset.path == path);
            if (record == null)
            {
                record = new AssetRecord { path = path, kind = kind };
                manifest.assets.Add(record);
            }
            record.guid = AssetDatabase.AssetPathToGUID(path);
            record.lastUsedUtc = DateTime.UtcNow.ToString("O");
            if (!record.owners.Contains(owner))
            {
                record.owners.Add(owner);
            }
            Save();
        }

        /// <summary>
        /// 処理中の失敗を記録します。セッションの終了は実行全体の終了時に行います。
        /// </summary>
        public void RecordFailure(string message)
        {
            RequireRunning();
            manifest.error = message;
            Save();
        }

        /// <summary>
        /// 中断したセッションとして終了します。
        /// </summary>
        public void Interrupt()
        {
            Finish("Interrupted");
        }

        /// <summary>
        /// 実行を終了し、成功または失敗の状態を保存します。
        /// </summary>
        public void Dispose()
        {
            Finish(null);
        }

        public void RequireRunning()
        {
            manifest = Load(Id).manifest;
            if (State != "Running") throw new InvalidOperationException("Generation session has already ended: " + Id);
        }

        void Finish(string state)
        {
            manifest = Load(Id).manifest;
            if (State != "Running") return;
            manifest.state = state ?? (string.IsNullOrEmpty(manifest.error) ? "Completed" : "Failed");
            manifest.endedUtc = DateTime.UtcNow.ToString("O");
            Save();
        }

        void Save()
        {
            Directory.CreateDirectory(ManifestRoot);
            var temporary = ManifestPath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(manifest, true));
            const int attempts = 6;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                try
                {
                    if (File.Exists(ManifestPath))
                    {
                        File.Replace(temporary, ManifestPath, null);
                    }
                    else
                    {
                        File.Move(temporary, ManifestPath);
                    }
                    return;
                }
                catch (IOException) when (attempt < attempts - 1)
                {
                    System.Threading.Thread.Sleep(20 * (attempt + 1));
                }
            }
        }

        static string ValidateRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Output root is required.", nameof(root));
            root = root.Replace('\\', '/').TrimEnd('/');
            var segments = root.Split('/');
            if (!root.StartsWith("Assets/", StringComparison.Ordinal) ||
                segments.Any(segment => segment.Length == 0 || segment == "." || segment == ".." ||
                    segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            {
                throw new ArgumentException("Output root must be a folder under Assets.", nameof(root));
            }
            return root;
        }

        /// <summary>
        /// AssetDatabaseへ登録された保存先フォルダを作成します。
        /// </summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(parent.Length + 1));
        }
    }
}
