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
            var compilation = new FluxParticleProgramCompiler().Compile(definitions);
            var shaders = new FluxParticleProgramShaderGenerator().Generate(compilation, "Hidden/FluxFX/Program", FluxParticleCoreDefinitions.CreateLifecycle());
            var rendererSource = FluxParticleBillboardShaderGenerator.Generate(shaders);
            return new FluxParticleBuildResult(shaders, initialValues, rendererSource);
        }
    }
}
