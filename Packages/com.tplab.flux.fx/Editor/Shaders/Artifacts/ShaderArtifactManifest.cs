using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    public sealed class ShaderArtifactManifest
    {
        const string ManifestPath = "Assets/FluxFX/Settings/ShaderArtifactManifest.json";
        const int FormatVersion = 1;

        const string ShaderDirectory = "Assets/FluxFX/Generated/Shaders/";
        const string IncludeDirectory = "Assets/FluxFX/Generated/Includes/";

        readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        bool _dirty;

        sealed class Entry
        {
            public string Path;
            public string Hash;
            public string Kind;
            public string SourcePath;
            public string Stage;
        }

        public bool IsDirty => _dirty;

        public static ShaderArtifactManifest Load()
        {
            var manifest = new ShaderArtifactManifest();

            if (!File.Exists(ManifestPath)) return manifest;

            var root = JObject.Parse(File.ReadAllText(ManifestPath));

            if ((int)root["version"] != FormatVersion)
            {
                throw new InvalidOperationException("Unsupported Shader Artifact Manifest version.");
            }

            var entries = (JArray)root["entries"];

            foreach (var token in entries)
            {
                var entry = new Entry
                {
                    Path = (string)token["path"],
                    Hash = (string)token["hash"],
                    Kind = (string)token["kind"],
                    SourcePath = (string)token["sourcePath"],
                    Stage = (string)token["stage"]
                };

                ValidateEntry(entry);

                if (!manifest._entries.TryAdd(entry.Path, entry))
                {
                    throw new InvalidOperationException($"Duplicate Manifest entry: {entry.Path}");
                }
            }

            return manifest;
        }

        public void Register(ShaderArtifact artifact)
        {
            if (artifact == null) throw new ArgumentNullException(nameof(artifact));

            var entries = new List<Entry>
            {
                new Entry
                {
                    Path = artifact.AssetPath,
                    Hash = ComputeHash(artifact.Source),
                    Kind = "shader",
                    Stage = artifact.Stage.ToString()
                }
            };

            foreach (var include in artifact.Includes.Artifacts)
            {
                entries.Add(new Entry
                {
                    Path = include.AssetPath,
                    Hash = ComputeHash(include.Source),
                    Kind = "include",
                    SourcePath = include.SourcePath
                });
            }

            foreach (var entry in entries)
            {
                ValidateEntry(entry);

                if (!IsUnmodified(entry))
                {
                    throw new InvalidOperationException($"Generated artifact content mismatch: {entry.Path}");
                }

                if (_entries.TryGetValue(entry.Path, out var existing) &&
                    !HasSameIdentity(existing, entry))
                {
                    throw new InvalidOperationException($"Manifest entry conflict: {entry.Path}");
                }
            }

            foreach (var entry in entries)
            {
                if (_entries.ContainsKey(entry.Path)) continue;

                _entries.Add(entry.Path, entry);
                _dirty = true;
            }
        }

        public void Save()
        {
            if (!_dirty) return;

            var entries = new JArray();

            foreach (var entry in _entries.Values.OrderBy(e => e.Path, StringComparer.Ordinal))
            {
                entries.Add(new JObject
                {
                    ["path"] = entry.Path,
                    ["hash"] = entry.Hash,
                    ["kind"] = entry.Kind,
                    ["sourcePath"] = entry.SourcePath,
                    ["stage"] = entry.Stage
                });
            }

            var root = new JObject
            {
                ["version"] = FormatVersion,
                ["entries"] = entries
            };

            var directory = Path.GetDirectoryName(ManifestPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = root.ToString(Formatting.Indented);
            if (File.Exists(ManifestPath) && File.ReadAllText(ManifestPath) == json)
            {
                _dirty = false;
                return;
            }

            File.WriteAllText(ManifestPath, json, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ManifestPath, ImportAssetOptions.ForceSynchronousImport);

            _dirty = false;
        }

        public bool CanDelete(string path)
        {
            return _entries.TryGetValue(path, out var entry) && IsUnmodified(entry);
        }

        public void Unregister(IEnumerable<string> paths)
        {
            var changed = false;

            foreach (var path in paths)
            {
                changed |= _entries.Remove(path);
            }

            if (!changed) return;

            _dirty = true;
            Save();
        }

        static bool HasSameIdentity(Entry a, Entry b)
        {
            return a.Hash == b.Hash &&
                   a.Kind == b.Kind &&
                   a.Stage == b.Stage &&
                   a.SourcePath == b.SourcePath;
        }

        static void ValidateEntry(Entry entry)
        {
            var prefix = entry.Kind == "shader" ? ShaderDirectory :
                entry.Kind == "include" ? IncludeDirectory : null;

            if (prefix == null ||
                string.IsNullOrEmpty(entry.Path) ||
                !entry.Path.StartsWith(prefix, StringComparison.Ordinal) ||
                !IsDirectChild(entry.Path, prefix) ||
                string.IsNullOrEmpty(entry.Hash) ||
                entry.Hash.Length != 64 ||
                entry.Hash.Any(c => !Uri.IsHexDigit(c)))
            {
                throw new InvalidOperationException($"Invalid Manifest entry: {entry.Path}");
            }
        }

        static bool IsDirectChild(string path, string prefix)
        {
            var name = path.Substring(prefix.Length);

            return name.Length > 0 &&
                   name != "." &&
                   name != ".." &&
                   !name.Contains("/") &&
                   !name.Contains("\\");
        }

        static bool IsUnmodified(Entry entry)
        {
            if (!File.Exists(entry.Path)) return false;

            return string.Equals(
                ComputeHash(File.ReadAllText(entry.Path)),
                entry.Hash,
                StringComparison.OrdinalIgnoreCase);
        }

        static string ComputeHash(string content)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}