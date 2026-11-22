using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 固定した依存HLSLを共有Cacheへ保存し、利用セッションへ記録します。
    /// </summary>
    public static class FluxParticleShaderIncludeStore
    {
        /// <summary>
        /// 生成Shaderの依存HLSLを固定し、既存ファイルを上書きせず保存します。
        /// </summary>
        /// <param name="source">生成Shader本文</param>
        /// <param name="session">生成セッション</param>
        /// <param name="owner">生成物の利用元</param>
        /// <returns>固定includeを参照するShader本文と依存一覧</returns>
        public static FluxParticleShaderSnapshot Save(string source, FluxParticleGenerationSession session, string owner)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            session.RequireRunning();
            var snapshot = FluxParticleShaderSnapshot.Create(source, session.ShaderRoot, ReadSource);
            Save(snapshot, session, owner);
            return snapshot;
        }

        internal static void Save(FluxParticleShaderSnapshot snapshot, FluxParticleGenerationSession session, string owner)
        {
            foreach (var file in snapshot.IncludeSources)
            {
                if (File.Exists(file.Key) && File.ReadAllText(file.Key) != file.Value)
                {
                    throw new InvalidOperationException("Generated Include was modified: " + file.Key);
                }
            }
            FluxParticleGenerationSession.EnsureFolder(session.ShaderRoot);
            // 全依存を書き出してからImportし、親includeだけ先に参照される状態を避けます。
            var written = new List<string>();
            try
            {
                foreach (var file in snapshot.IncludeSources)
                {
                    if (File.Exists(file.Key))
                    {
                        written.Add(file.Key);
                        continue;
                    }
                    using (var stream = new FileStream(file.Key, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        written.Add(file.Key);
                        using (var writer = new StreamWriter(stream)) writer.Write(file.Value);
                    }
                }
            }
            finally
            {
                // 書き込み済みの全パスを先に記録し、途中のImport失敗でも管理対象を失わないようにします。
                foreach (var path in written)
                {
                    session.Record(path, "ShaderInclude", owner, snapshot.IncludeDependencies[path]);
                }
                foreach (var path in written)
                {
                    if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                        session.Record(path, "ShaderInclude", owner, snapshot.IncludeDependencies[path]);
                    }
                }
            }
        }

        internal static string ReadSource(string path)
        {
            if (path.StartsWith("Packages/", StringComparison.Ordinal))
            {
                var package = PackageInfo.FindForAssetPath(path);
                if (package == null) throw new FileNotFoundException("Shader dependency package was not found: " + path);
                var relative = path.Substring(("Packages/" + package.name + "/").Length);
                return File.ReadAllText(Path.Combine(package.resolvedPath, relative));
            }
            return File.ReadAllText(path);
        }
    }
}
