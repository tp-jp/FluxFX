using System;
using System.Collections.Generic;
using System.Text;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// HLSL実装と生成側で共有する衝突しない識別子の規則です。
    /// </summary>
    public static class FluxParticleHlslNames
    {
        /// <summary>
        /// 論理IDをHLSL識別子へ変換します。記号とunderscoreをUTF-16コードで区切ります。
        /// </summary>
        /// <param name="id">空でない論理ID</param>
        /// <returns>決定的で一意な識別子</returns>
        public static string Encode(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID is required.", nameof(id));
            var text = new StringBuilder("n_");
            foreach (var c in id)
            {
                if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9')
                {
                    text.Append(c);
                }
                else
                {
                    text.Append('_').Append(((int)c).ToString("x4")).Append('_');
                }
            }
            return text.ToString();
        }

        /// <summary>
        /// Operation IDからContext型名を生成します。
        /// 区切りや大文字小文字による型名の衝突はShader生成時に検出します。
        /// </summary>
        /// <param name="id">Operation ID</param>
        /// <returns>生成するstruct名</returns>
        public static string Context(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID is required.", nameof(id));
            var name = new StringBuilder("Flux");
            var wordStart = true;
            foreach (var c in id)
            {
                if (c == '.' || c == '-' || c == '_')
                {
                    wordStart = true;
                    continue;
                }
                if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9')
                {
                    name.Append(wordStart ? char.ToUpperInvariant(c) : c);
                }
                else
                {
                    name.Append('X').Append(((int)c).ToString("X4"));
                }
                wordStart = false;
            }
            return name.Append("Context").ToString();
        }

        /// <summary>
        /// Context内のValueフィールド名を生成します。
        /// </summary>
        /// <param name="id">Value ID</param>
        /// <returns>生成するフィールド名</returns>
        public static string Value(string id) => "value" + ReadableName(id);

        /// <summary>
        /// Context内のParameterフィールド名を生成します。
        /// </summary>
        /// <param name="id">Parameter ID</param>
        /// <returns>生成するフィールド名</returns>
        public static string Parameter(string id) => "parameter" + ReadableName(id);

        /// <summary>
        /// OperationのHLSLローカル変数名を生成します。
        /// </summary>
        /// <param name="id">Operation ID</param>
        /// <returns>camelCase形式のローカル変数名</returns>
        public static string OperationLocal(string id) => "operation" + ReadableName(id);

        static string ReadableName(string id)
        {
            var context = Context(id);
            return context.Substring("Flux".Length, context.Length - "Flux".Length - "Context".Length);
        }

        /// <summary>
        /// ParameterのMaterial Property名を生成します。
        /// </summary>
        /// <param name="id">Parameter ID</param>
        /// <returns>生成するUniform名</returns>
        public static string ParameterProperty(string id) => "_FluxParameter_" + Encode(id);

        /// <summary>
        /// ProviderのMaterial Property名を生成します。
        /// </summary>
        /// <param name="id">ProviderのValue ID</param>
        /// <returns>Uniform名またはTexture名</returns>
        public static string ProviderProperty(string id) => "_FluxProvider_" + Encode(id);

        /// <summary>
        /// Page SnapshotのTexture Property名を生成します。
        /// </summary>
        /// <param name="pageId">論理Page ID</param>
        /// <param name="version">StepPreviousまたはUpdated</param>
        /// <returns>Sampler名</returns>
        public static string PageProperty(int pageId, FluxParticlePageVersion version) => "_FluxPage" + pageId + "_" + version;
    }

    /// <summary>
    /// 生存判定に使う論理Valueです。負のAgeは非生存、AgeがLifetime以上なら失効します。
    /// </summary>
    public sealed class FluxParticleShaderLifecycle
    {
        /// <summary>
        /// Persistent Floatで保持するAgeのValue ID
        /// </summary>
        public string AgeValueId { get; }
        /// <summary>
        /// FloatのLifetime Value ID
        /// </summary>
        /// <remarks>Persistent StateまたはProviderで供給します。</remarks>
        public string LifetimeValueId { get; }

        /// <summary>
        /// 各Pass共通のLifecycle判定を設定します。
        /// </summary>
        /// <param name="ageValueId">AgeのValue ID</param>
        /// <param name="lifetimeValueId">LifetimeのValue ID</param>
        public FluxParticleShaderLifecycle(string ageValueId, string lifetimeValueId)
        {
            if (string.IsNullOrWhiteSpace(ageValueId)) throw new ArgumentException("Age ID is required.", nameof(ageValueId));
            if (string.IsNullOrWhiteSpace(lifetimeValueId)) throw new ArgumentException("Lifetime ID is required.", nameof(lifetimeValueId));
            AgeValueId = ageValueId;
            LifetimeValueId = lifetimeValueId;
        }
    }

    /// <summary>
    /// 1回のBlit用Shaderと入力Snapshotを保持します。
    /// </summary>
    public sealed class FluxParticleGeneratedPass
    {
        /// <summary>
        /// 論理Operationと出力Pageの実行計画
        /// </summary>
        public FluxParticleGpuPass Plan { get; }
        /// <summary>
        /// UnityへImportする完全なShaderLabソース
        /// </summary>
        public string Source { get; }
        /// <summary>
        /// Lifecycle評価を含めた入力Page
        /// </summary>
        /// <remarks>Plan.Readsより増える場合があります。</remarks>
        public IReadOnlyList<FluxParticlePageRead> Reads { get; }

        internal FluxParticleGeneratedPass(FluxParticleGpuPass plan, string source, IEnumerable<FluxParticlePageRead> reads)
        {
            Plan = plan;
            Source = source;
            Reads = new List<FluxParticlePageRead>(reads).AsReadOnly();
        }
    }

    /// <summary>
    /// 生成Shaderと実行計画を保持します。
    /// </summary>
    public sealed class FluxParticleShaderProgram
    {
        /// <summary>
        /// 検証済みの論理ProgramとState Layout
        /// </summary>
        public FluxParticleCompiledProgram Compilation { get; }
        /// <summary>
        /// 実行順に並べた1Pass Shader
        /// </summary>
        public IReadOnlyList<FluxParticleGeneratedPass> Passes { get; }
        /// <summary>
        /// PageごとのState初期化Shader
        /// </summary>
        /// <remarks>Compilation.StateLayout.Pagesと同じ順です。</remarks>
        public IReadOnlyList<string> ClearSources { get; }
        /// <summary>
        /// 描画側へ組み込むHLSL
        /// </summary>
        /// <remarks>確定PageのSamplerとFluxFX_EvaluateRenderを宣言します。</remarks>
        public string RenderSource { get; }

        /// <summary>
        /// Step開始時の空きSlotを判定するShader
        /// </summary>
        public string AvailabilitySource { get; }
        /// <summary>
        /// 外部から値またはTextureを供給するProvider
        /// </summary>
        public IReadOnlyList<FluxParticleValueProvider> ExternalProviders { get; }

        internal FluxParticleShaderProgram(FluxParticleCompiledProgram program, IEnumerable<FluxParticleGeneratedPass> passes,
            IEnumerable<string> clearSources, string renderSource,
            IEnumerable<FluxParticleValueProvider> externalProviders, string availabilitySource)
        {
            Compilation = program;
            Passes = new List<FluxParticleGeneratedPass>(passes).AsReadOnly();
            ClearSources = new List<string>(clearSources).AsReadOnly();
            RenderSource = renderSource;
            AvailabilitySource = availabilitySource;
            ExternalProviders = new List<FluxParticleValueProvider>(externalProviders).AsReadOnly();
        }
    }
}
