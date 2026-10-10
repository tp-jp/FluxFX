using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Shaders
{
    public sealed class FluxParticleShaderDraft
    {
        public FluxParticleExecutionStage Stage { get; }
        public string Source { get; }

        public FluxParticleShaderDraft(FluxParticleExecutionStage stage, string source)
        {
            Stage = stage;
            Source = source;
        }
    }
}