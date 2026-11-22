using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 生成Shaderと依存HLSLを内容で識別して保存し、利用セッションへ記録します。
    /// 設定からのShader生成やMaterialへの割り当ては呼び出し元が行います。
    /// </summary>
    public static class FluxParticleShaderStore
    {
        /// <summary>
        /// 依存を固定してからShaderを保存し、コンパイルエラーがないことを確認します。
        /// 失敗はセッションへ記録して例外で通知します。成功・失敗ともセッションを終了しません。
        /// Play/BuildではGenerationLifecycle.Currentを共有し、手動実行では呼び出し元が開始・終了します。
        /// </summary>
        /// <param name="source">生成Shader本文。直下のincludeはAssets/Packages相対パス（短縮パスはAssets基準）</param>
        /// <param name="session">実行中の明示的な生成セッション</param>
        /// <param name="owner">空でない利用元識別子</param>
        /// <returns>依存を固定して保存し、現在のEditor環境でコンパイル確認したShader</returns>
        public static Shader Save(string source, FluxParticleGenerationSession session, string owner)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            session.RequireRunning();
            var path = session.ShaderRoot + "/<unresolved>.shader";
            FluxParticleShaderSnapshot snapshot = null;
            try
            {
                if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Shader source is required.", nameof(source));
                if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Shader owner is required.", nameof(owner));
                snapshot = FluxParticleShaderSnapshot.Create(source, session.ShaderRoot, FluxParticleShaderIncludeStore.ReadSource);
                source = snapshot.Source;
                path = session.ShaderRoot + "/" + Hash128.Compute(source) + ".shader";
                if (File.Exists(path) && File.ReadAllText(path) != source)
                {
                    throw new InvalidOperationException("Generated Shader was modified: " + path);
                }
                FluxParticleShaderIncludeStore.Save(snapshot, session, owner);
                var written = false;
                try
                {
                    if (!File.Exists(path))
                    {
                        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            written = true;
                            using (var writer = new StreamWriter(stream)) writer.Write(source);
                        }
                    }
                    else
                    {
                        written = true;
                    }
                }
                finally
                {
                    // 書き込み途中の失敗も記録し、参照されない生成物を後から整理できるようにします。
                    if (written)
                    {
                        if (AssetDatabase.LoadAssetAtPath<Shader>(path) == null)
                        {
                            session.Record(path, "Shader", owner, snapshot.IncludeSources.Keys);
                            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                        }
                        session.Record(path, "Shader", owner, snapshot.IncludeSources.Keys);
                    }
                }
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null || ShaderUtil.ShaderHasError(shader))
                {
                    var messages = shader == null ? "Shader import returned no asset." : string.Join("\n",
                        ShaderUtil.GetShaderMessages(shader).Select(message => $"{message.file}:{message.line}: {message.message}"));
                    throw new InvalidOperationException("Shader failed to compile: " + messages);
                }
                return shader;
            }
            catch (Exception error)
            {
                var dependencies = snapshot == null ? "See dependency error below." : string.Join("\n",
                    snapshot.OriginalPaths.Select(pair => pair.Key + " -> " + pair.Value));
                var message = $"Shader generation failed. Owner: {owner}\nShader: {path}\nDependencies: {dependencies}\n{error.Message}";
                session.RecordFailure(message);
                throw new InvalidOperationException(message, error);
            }
        }
    }
}
