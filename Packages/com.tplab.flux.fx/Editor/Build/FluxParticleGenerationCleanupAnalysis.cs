using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Build
{
    internal static class FluxParticleGenerationCleanupAnalysis
    {
        internal static FluxParticleGenerationCleanupReport Run(string root, DateTime? cutoff, bool delete,
            FluxParticleGenerationCleanupReport review = null, Func<string, float, bool> progress = null,
            Action onFullScan = null)
        {
            root = FluxParticleGenerationSession.ValidateRoot(root);
            var report = new FluxParticleGenerationCleanupReport { root = root, createdUtc = DateTime.UtcNow.ToString("O") };
            try
            {
                if (FluxParticleGenerationCleanup.IsBusy)
                {
                    report.blocked = true;
                    report.diagnostics.Add("Play, Build, compilation or asset import is active.");
                    return report;
                }
                CheckProgress(progress, "Checking generation records", 0);
                var sessions = LoadSessions();
                var ledger = FluxParticleGenerationLedger.Migrate(root, sessions);
                var ledgers = FluxParticleGenerationLedger.LoadAll();
                var assets = ledger.assets.ToDictionary(asset => asset.path, StringComparer.Ordinal);
                var reviewed = review?.items.Where(item => item.status == "Candidate").ToDictionary(item => item.path, item => item.guid);
                foreach (var asset in ledger.assets)
                {
                    var item = new FluxParticleGenerationCleanupReport.Item
                    {
                        path = asset.path, guid = asset.guid, kind = asset.kind, bytes = Bytes(asset.path) + Bytes(asset.path + ".meta"),
                        status = "Candidate", reason = "Unreferenced managed asset"
                    };
                    report.items.Add(item);
                    if (!File.Exists(asset.path))
                    {
                        Set(item, "Missing", "File is absent");
                    }
                    else if (!FluxParticleGenerationLedger.Matches(root, asset))
                    {
                        Set(item, "Keep", "GUID, asset kind or shader content differs from the ownership record");
                    }
                    else if (reviewed != null && (!reviewed.TryGetValue(asset.path, out var guid) || guid != asset.guid))
                    {
                        Set(item, "Keep", "Not in the reviewed selection");
                    }
                    else if (cutoff.HasValue && asset.kind != "Material" && FluxParticleGenerationLedger.ParseUtc(asset.lastUsedUtc) > cutoff.Value)
                    {
                        Set(item, "Keep", "Cache was used within the last 24 hours");
                    }
                    else if (cutoff.HasValue && !string.IsNullOrEmpty(asset.retainUntilUtc) &&
                        FluxParticleGenerationLedger.ParseUtc(asset.retainUntilUtc) > DateTime.UtcNow)
                    {
                        Set(item, "Keep", "Failed or interrupted generation is retained for diagnosis");
                    }
                }
                if (ledger.activeSessions.Count != 0 || sessions.Any(session => session.Root == root && session.State == "Running"))
                {
                    report.blocked = true;
                    foreach (var item in report.items.Where(item => item.status == "Candidate")) Set(item, "Keep", "Generation session is running");
                    report.diagnostics.Add("Running sessions: " + string.Join(", ", ledger.activeSessions.Select(value => value.id)) +
                        ". The caller must finish manual sessions; elapsed time does not end them.");
                    return report;
                }
                if (report.CandidateCount == 0)
                {
                    if (delete)
                    {
                        FluxParticleGenerationLedger.Compact(ledger, sessions);
                    }
                    return report;
                }

                CheckProgress(progress, "Checking generated asset references", 0.15f);
                onFullScan?.Invoke();
                FluxParticleGenerationCleanup.FullScanStarted(root);
                report.fullScan = true;
                var candidates = report.items.Where(item => item.status == "Candidate").ToDictionary(item => item.path, StringComparer.Ordinal);
                var recorded = ledgers.SelectMany(value => value.assets).Cast<FluxParticleGenerationSession.AssetRecord>()
                    .Concat(sessions.SelectMany(value => value.Assets)).Where(asset => asset.dependencies != null)
                    .GroupBy(asset => asset.path).ToDictionary(group => group.Key,
                        group => group.SelectMany(asset => asset.dependencies).Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray());
                var dependencies = new Dictionary<string, string[]>(StringComparer.Ordinal);
                foreach (var path in candidates.Keys)
                {
                    recorded.TryGetValue(path, out var children);
                    dependencies[path] = AssetDatabase.GetDependencies(path, false).Concat(children ?? Array.Empty<string>())
                        .Where(child => child != path).Distinct().ToArray();
                }
                CheckProgress(progress, "Checking saved asset references", 0.3f);
                var allPaths = AssetDatabase.GetAllAssetPaths().Where(path => !AssetDatabase.IsValidFolder(path)).ToArray();
                var external = allPaths.Where(path => !candidates.ContainsKey(path)).ToArray();
                var protectedPaths = new HashSet<string>(AssetDatabase.GetDependencies(external, true), StringComparer.Ordinal);
                var references = new Dictionary<string, string>(StringComparer.Ordinal);
                var needingSource = new HashSet<string>(candidates.Keys.Where(protectedPaths.Contains));
                if (onFullScan == null)
                {
                    FindReferences(external, needingSource, values => AssetDatabase.GetDependencies(values, true), path => "Asset: " + path, references);
                }
                CheckProgress(progress, "Checking open scenes and live objects", 0.55f);
                var liveObjects = Resources.FindObjectsOfTypeAll<Object>().Where(value => value != null && !EditorUtility.IsPersistent(value)).ToArray();
                var livePaths = new HashSet<string>(EditorUtility.CollectDependencies(liveObjects).Select(AssetDatabase.GetAssetPath)
                    .Where(path => !string.IsNullOrEmpty(path)), StringComparer.Ordinal);
                needingSource = new HashSet<string>(candidates.Keys.Where(path => livePaths.Contains(path) && !references.ContainsKey(path)));
                if (onFullScan == null)
                {
                    FindReferences(liveObjects, needingSource, values => EditorUtility.CollectDependencies(values).Select(AssetDatabase.GetAssetPath),
                        value => "Object: " + value.GetType().Name + " / " + value.name + " (#" + value.GetInstanceID() + ")", references);
                }
                protectedPaths.UnionWith(livePaths);
                protectedPaths.UnionWith(AssetDatabase.GetDependencies(protectedPaths.ToArray(), true));
                Protect(protectedPaths, recorded, references);
                foreach (var item in candidates.Values.Where(item => protectedPaths.Contains(item.path)))
                {
                    references.TryGetValue(item.path, out item.reference);
                    Set(item, "Keep", "Referenced asset or object");
                }
                foreach (var path in allPaths.Where(path => path.StartsWith(root + "/", StringComparison.Ordinal) && !assets.ContainsKey(path)))
                {
                    report.items.Add(new FluxParticleGenerationCleanupReport.Item
                    {
                        path = path, status = "Keep", reason = "No ownership record", bytes = Bytes(path) + Bytes(path + ".meta")
                    });
                }
                var order = Order(report, dependencies);
                if (delete)
                {
                    DeleteWithProgress(report, order, dependencies, assets, AssetDatabase.DeleteAsset, progress);
                    FluxParticleGenerationLedger.Compact(ledger, sessions);
                    foreach (var session in sessions.Where(value => value.Root == root && value.State != "Running"))
                    {
                        RemoveEmptyFolder(session.MaterialRoot);
                        RemoveEmptyFolder(root + "/Sessions/" + session.Id);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                report.blocked = true;
                report.cancelled = true;
                report.diagnostics.Add("Cleanup cancelled; remaining assets are kept.");
                foreach (var item in report.items.Where(item => item.status == "Candidate")) Set(item, "Keep", "Cleanup cancelled");
            }
            catch (Exception error)
            {
                report.blocked = true;
                report.failed = true;
                report.diagnostics.Add(error.Message);
                foreach (var item in report.items.Where(item => item.status == "Candidate")) Set(item, "Keep", "Analysis or cleanup failed");
            }
            finally
            {
                SaveReport(report);
            }
            return report;
        }

        // 削除順はファイル名や拡張子ではなく依存グラフから決めます。
        internal static List<FluxParticleGenerationCleanupReport.Item> Order(FluxParticleGenerationCleanupReport report,
            Dictionary<string, string[]> dependencies)
        {
            var candidates = report.items.Where(item => item.status == "Candidate").ToDictionary(item => item.path);
            var parents = candidates.Keys.ToDictionary(path => path, path => 0);
            foreach (var path in candidates.Keys)
            {
                if (!dependencies.TryGetValue(path, out var children)) continue;
                foreach (var child in children.Where(candidates.ContainsKey).Distinct()) parents[child]++;
            }
            var pending = new SortedSet<string>(parents.Where(pair => pair.Value == 0).Select(pair => pair.Key), StringComparer.Ordinal);
            var result = new List<FluxParticleGenerationCleanupReport.Item>();
            while (pending.Count != 0)
            {
                var path = pending.Min;
                pending.Remove(path);
                result.Add(candidates[path]);
                if (!dependencies.TryGetValue(path, out var children)) continue;
                foreach (var child in children.Where(candidates.ContainsKey).Distinct())
                {
                    if (--parents[child] == 0)
                    {
                        pending.Add(child);
                    }
                }
            }
            foreach (var item in candidates.Values.Where(item => parents[item.path] != 0)) Set(item, "Keep", "Dependency cycle or dependency of a cycle");
            return result;
        }

        internal static void Delete(FluxParticleGenerationCleanupReport report, List<FluxParticleGenerationCleanupReport.Item> order,
            Dictionary<string, string[]> dependencies, Dictionary<string, FluxParticleGenerationLedger.Entry> assets, Func<string, bool> delete)
        {
            DeleteWithProgress(report, order, dependencies, assets, delete, null);
        }

        static void DeleteWithProgress(FluxParticleGenerationCleanupReport report, List<FluxParticleGenerationCleanupReport.Item> order,
            Dictionary<string, string[]> dependencies, Dictionary<string, FluxParticleGenerationLedger.Entry> assets, Func<string, bool> delete,
            Func<string, float, bool> progress)
        {
            var protectedPaths = new HashSet<string>();
            var references = new Dictionary<string, string>();
            var index = 0;
            foreach (var item in order)
            {
                CheckProgress(progress, "Removing unused generated assets", 0.75f + 0.25f * index++ / Math.Max(1, order.Count));
                if (item.status != "Candidate") continue;
                if (protectedPaths.Contains(item.path))
                {
                    Set(item, "Keep", "A referring asset could not be deleted");
                    references.TryGetValue(item.path, out item.reference);
                    continue;
                }
                if (FluxParticleGenerationCleanup.IsBusy)
                {
                    report.blocked = true;
                    foreach (var remaining in order.Where(value => value.status == "Candidate")) Set(remaining, "Keep", "Editor became busy");
                    break;
                }
                try
                {
                    if (!FluxParticleGenerationLedger.Matches(report.root, assets[item.path])) throw new InvalidOperationException("Asset identity changed after analysis");
                    if (!delete(item.path)) throw new IOException("AssetDatabase.DeleteAsset did not delete the asset");
                    Set(item, "Deleted", "Deleted");
                }
                catch (Exception error)
                {
                    Set(item, "Failed", error.Message);
                    protectedPaths.Add(item.path);
                    references[item.path] = item.path;
                    Protect(protectedPaths, dependencies, references);
                }
            }
        }

        static void CheckProgress(Func<string, float, bool> progress, string message, float value)
        {
            if (progress != null && progress(message, value)) throw new OperationCanceledException();
        }

        static FluxParticleGenerationSession[] LoadSessions()
        {
            if (!Directory.Exists(FluxParticleGenerationSession.ManifestRoot)) return Array.Empty<FluxParticleGenerationSession>();
            return Directory.GetFiles(FluxParticleGenerationSession.ManifestRoot, "*.json").Select(path =>
            {
                try { return FluxParticleGenerationSession.Load(Path.GetFileNameWithoutExtension(path)); }
                catch (Exception error) { throw new InvalidOperationException("Cannot read generation session: " + path + "\n" + error.Message, error); }
            }).ToArray();
        }

        // 参照元を1件ずつ全探索せず、集合を二分して対象を含む側だけを辿ります。
        static void FindReferences<T>(T[] sources, HashSet<string> targets, Func<T[], IEnumerable<string>> dependencies,
            Func<T, string> describe, Dictionary<string, string> references)
        {
            void Visit(int start, int count, HashSet<string> paths)
            {
                if (count == 0 || paths.Count == 0) return;
                if (count == 1)
                {
                    var reference = describe(sources[start]);
                    foreach (var path in paths) references[path] = reference;
                    return;
                }
                var half = count / 2;
                var left = new T[half];
                Array.Copy(sources, start, left, 0, half);
                var matches = new HashSet<string>(dependencies(left).Where(paths.Contains));
                Visit(start, half, matches);
                var remaining = new HashSet<string>(paths.Except(matches));
                Visit(start + half, count - half, remaining);
            }
            Visit(0, sources.Length, targets);
        }

        static void Protect(HashSet<string> protectedPaths, Dictionary<string, string[]> dependencies, Dictionary<string, string> references)
        {
            var pending = new Queue<string>(protectedPaths);
            while (pending.Count != 0)
            {
                var parent = pending.Dequeue();
                if (!dependencies.TryGetValue(parent, out var children)) continue;
                foreach (var child in children)
                {
                    if (!protectedPaths.Add(child)) continue;
                    references[child] = references.TryGetValue(parent, out var origin) ? origin + " -> " + parent : parent;
                    pending.Enqueue(child);
                }
            }
        }

        static void Set(FluxParticleGenerationCleanupReport.Item item, string status, string reason)
        {
            item.status = status;
            item.reason = reason;
        }

        static long Bytes(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

        static void SaveReport(FluxParticleGenerationCleanupReport report)
        {
            const string directory = "Temp/FluxFX/GenerationCleanup";
            Directory.CreateDirectory(directory);
            report.logPath = directory + "/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N") + ".json";
            File.WriteAllText(report.logPath, JsonUtility.ToJson(report, true));
        }

        static void RemoveEmptyFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path) && !Directory.EnumerateFileSystemEntries(path).Any())
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
