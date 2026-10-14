using System;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// 論理定義の依存関係を解決し、Shader Programを構築します。
    /// Authoring固有の設定解釈やModule種別には依存しません。
    /// </summary>
    public sealed class FluxParticleProgramComposer
    {
        /// <summary>
        /// 定義の依存関係を確定し、GPU実行用のShader Programを生成します。
        /// </summary>
        /// <param name="definitions">Module Contributionを含む論理定義。</param>
        /// <param name="shaderName">生成するShader名。</param>
        /// <param name="lifecycle">Particleの生存期間の定義。</param>
        /// <returns>GPU PassとShaderソースを含むProgram。</returns>
        public FluxParticleShaderProgram Compose(FluxParticleDefinitionBuilder definitions, string shaderName,
            FluxParticleShaderLifecycle lifecycle)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            return new FluxParticleProgramShaderGenerator().Generate(definitions, shaderName, lifecycle);
        }
    }
}
