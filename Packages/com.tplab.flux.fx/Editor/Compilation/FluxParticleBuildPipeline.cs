using TpLab.Flux.FX.Scripts;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// Authoring設定からParticleの実行・描画データを生成します。
    /// </summary>
    public static class FluxParticleBuildPipeline
    {
        /// <summary>
        /// Authoring設定を検証し、GPU Passと描画Shaderを生成します。
        /// </summary>
        /// <param name="authoring">Particleの設定。</param>
        /// <returns>ビルド結果。</returns>
        public static FluxParticleBuildResult Build(FluxParticleAuthoring authoring)
        {
            FluxParticleAuthoringValidator.Validate(authoring);
            var definitions = FluxParticleAuthoringDefinitionBuilder.Create(authoring, out var initialValues);
            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            var program = new FluxParticleProgramShaderGenerator().Generate(definitions, "Hidden/FluxFX/Program", FluxParticleCoreDefinitions.CreateLifecycle());
            return new FluxParticleBuildResult(program, initialValues);
        }
    }
}
