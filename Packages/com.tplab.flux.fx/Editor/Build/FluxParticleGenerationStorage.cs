using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 端末ごとの生成管理情報を保存し、旧保存先から検証済みの内容だけを移します。
    /// 競合や読込失敗時は生成・整理を停止し、既定値による台帳や予約の上書きを防ぎます。
    /// </summary>
    internal static class FluxParticleGenerationStorage
    {
        internal const string DirectoryPath = "UserSettings/FluxFXGeneration";
        const string LegacyDirectoryPath = "ProjectSettings/FluxFXGeneration";
        static bool _migrated;

        // 自前の原子的置換と外部変更を検出し、古いメモリ状態で保存しないための指紋。
        internal static string Fingerprint(string path)
        {
            var file = new FileInfo(path);
            return file.Exists ? file.Length + ":" + file.LastWriteTimeUtc.Ticks + ":" + file.CreationTimeUtc.Ticks : "missing";
        }

        internal static void EnsureMigrated()
        {
            if (_migrated) return;
            Migrate(LegacyDirectoryPath, DirectoryPath);
            _migrated = true;
        }

        /// <summary>
        /// 全対象を検証してから同一バイト列を保存し、保存結果の確認後だけ旧ファイルを削除します。
        /// 保存途中の再実行では同一内容だけを再利用し、異なる内容はどちらも残して停止します。
        /// </summary>
        /// <param name="sourceDirectory">旧管理情報のディレクトリ</param>
        /// <param name="destinationDirectory">新管理情報のディレクトリ</param>
        internal static void Migrate(string sourceDirectory, string destinationDirectory)
        {
            sourceDirectory = Path.GetFullPath(sourceDirectory);
            destinationDirectory = Path.GetFullPath(destinationDirectory);
            if (string.Equals(sourceDirectory, destinationDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Generation metadata source and destination must differ.");
            }
            if (!Directory.Exists(sourceDirectory)) return;
            try
            {
                var files = new List<MigrationFile>();
                var schedule = Path.Combine(sourceDirectory, "CleanupSchedule.json");
                if (File.Exists(schedule))
                {
                    files.Add(Read(schedule, Path.Combine(destinationDirectory, "CleanupSchedule.json"), true));
                }
                var roots = Path.Combine(sourceDirectory, "Roots");
                if (Directory.Exists(roots))
                {
                    foreach (var path in Directory.GetFiles(roots, "*.json"))
                    {
                        files.Add(Read(path, Path.Combine(destinationDirectory, "Roots", Path.GetFileName(path)), false));
                    }
                }
                foreach (var file in files)
                {
                    if (File.Exists(file.Destination)) Verify(file.Destination, file.Bytes);
                }
                foreach (var file in files)
                {
                    if (!File.Exists(file.Destination))
                    {
                        CopyNew(file);
                    }
                }
                // 全件の保存を確認するまでは、移行元を一件も削除しません。
                foreach (var file in files)
                {
                    Verify(file.Source, file.Bytes);
                    Verify(file.Destination, file.Bytes);
                }
                foreach (var file in files)
                {
                    Verify(file.Source, file.Bytes);
                    Verify(file.Destination, file.Bytes);
                    File.Delete(file.Source);
                }
                RemoveEmpty(roots);
                RemoveEmpty(sourceDirectory);
            }
            catch (Exception error)
            {
                throw new InvalidOperationException("Cannot migrate FluxFX generation metadata: " + sourceDirectory + " -> " +
                    destinationDirectory + "\nExisting metadata has not been overwritten. Resolve the reported conflict or access error before retrying.\n" + error.Message, error);
            }
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            // 自動整理がOFFでも保存先は移します。Asset走査や生成物の削除は実行しません。
            EditorApplication.delayCall += MigrateOnLoad;
        }

        static void MigrateOnLoad()
        {
            try { EnsureMigrated(); }
            catch (Exception error) { Debug.LogError(error.Message); }
        }

        static MigrationFile Read(string source, string destination, bool schedule)
        {
            try
            {
                var bytes = File.ReadAllBytes(source);
                using (var reader = new StreamReader(new MemoryStream(bytes)))
                {
                    var text = reader.ReadToEnd();
                    if (schedule)
                    {
                        FluxParticleGenerationCleanupSchedule.Parse(text, source);
                    }
                    else
                    {
                        FluxParticleGenerationLedger.Parse(text, source, Path.GetDirectoryName(source));
                    }
                }
                return new MigrationFile(source, destination, bytes);
            }
            catch (Exception error)
            {
                throw new InvalidOperationException("Cannot read generation metadata: " + source + "\n" + error.Message, error);
            }
        }

        static void CopyNew(MigrationFile file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file.Destination));
            var temporary = file.Destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(file.Bytes, 0, file.Bytes.Length);
                    stream.Flush(true);
                }
                // 他の書込が先行した場合も上書きせず失敗させます。
                File.Move(temporary, file.Destination);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        static void Verify(string path, byte[] bytes)
        {
            if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(bytes))
            {
                throw new IOException("Generation metadata is missing or differs from the migration source: " + path);
            }
        }

        static void RemoveEmpty(string directory)
        {
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }

        sealed class MigrationFile
        {
            internal string Source { get; }
            internal string Destination { get; }
            internal byte[] Bytes { get; }

            internal MigrationFile(string source, string destination, byte[] bytes)
            {
                Source = source;
                Destination = destination;
                Bytes = bytes;
            }
        }
    }
}
