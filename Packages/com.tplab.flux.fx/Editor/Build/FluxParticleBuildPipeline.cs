using TpLab.Flux.FX.Editor.Authoring;
using TpLab.Flux.FX.Editor.Compilation.Compiler;
using TpLab.Flux.FX.Editor.Compilation.ShaderGeneration;
using TpLab.Flux.FX.Editor.Core;
using TpLab.Flux.FX.Scripts;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// Authoring設定からParticleの実行・描画データを生成します。
    /// </summary>
    public static class FluxParticleBuildPipeline
    {
        /// <summary>
        /// Authoring設定を検証し、GPU Passと描画Shaderを生成します。
        /// </summary>
        /// <param name="authoring">Particleの設定</param>
        /// <returns>ビルド結果</returns>
        public static FluxParticleBuildResult Build(FluxParticleAuthoring authoring)
        {
            FluxParticleAuthoringValidator.Validate(authoring);
            var definitions = FluxParticleAuthoringDefinitionBuilder.Create(authoring, out var initialValues, out var moduleBindings);
            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            FluxParticleValuePipelineResolver.Resolve(definitions, "render.color", definitions.OptionalDependencies,
                TpLab.Flux.FX.Editor.Compilation.Definitions.FluxParticlePhase.Render);
            var compilation = new FluxParticleProgramCompiler().Compile(definitions);
            var shaders = new FluxParticleProgramShaderGenerator().Generate(compilation, "Hidden/FluxFX/Program", FluxParticleCoreDefinitions.CreateLifecycle());
            var rendererSource = FluxParticleRenderShaderGenerator.Generate(shaders, authoring.Render.Mode, authoring.SimulationSpace == TpLab.Flux.FX.Udon.FluxParticleSimulationSpace.World);
            return new FluxParticleBuildResult(shaders, initialValues, rendererSource, moduleBindings);
        }
    }
}
