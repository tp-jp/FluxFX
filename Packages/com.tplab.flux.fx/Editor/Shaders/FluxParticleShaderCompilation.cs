using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Shaders
{
    public sealed class FluxParticleShaderCompilation
    {
        public FluxParticleExecutionStage Stage { get; }
        public string ShaderName { get; }
        public string Source { get; }
        public string CacheKey { get; }

        public FluxParticleShaderCompilation(FluxParticleExecutionStage stage, string shaderName, string source, string cacheKey)
        {
            Stage = stage;
            ShaderName = shaderName;
            Source = source;
            CacheKey = cacheKey;
        }
    }
}