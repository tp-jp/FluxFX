using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Compilation.Compiler;
using TpLab.Flux.FX.Editor.Compilation.Definitions;

namespace TpLab.Flux.FX.Editor.Compilation.ShaderGeneration
{
    /// <summary>
    /// 確定済みState Layoutから軌跡記録用Shaderを生成します。
    /// </summary>
    public static class FluxParticleTrailShaderGenerator
    {
        public static string Generate(FluxParticleShaderProgram program)
        {
            program.Compilation.StateLayout.TryGetBinding("particle.position", out var position);
            var sampler = FluxParticleHlslNames.PageProperty(position.PageId, FluxParticlePageVersion.Updated);
            var declaration = "sampler2D " + sampler + ";";
            var source = (program.RenderSource.Contains(declaration) ? "" : declaration + "\n") + program.RenderSource;
            var code = FluxParticleProgramShaderGenerator.ExpandTemplate("TrailCapture.hlsl.template", new Dictionary<string, string>
            {
                ["RenderCode"] = source,
                ["Position"] = "tex2D(" + sampler + ", uv)." + "rgba".Substring(position.ChannelOffset, position.ComponentCount)
            });
            return FluxParticleProgramShaderGenerator.ExpandTemplate("Shader.shader.template", new Dictionary<string, string>
            {
                ["ShaderName"] = "Hidden/FluxFX/TrailCapture",
                ["Code"] = code,
                ["Expression"] = "FluxFX_Trail(i.uv)"
            }).Replace("    SubShader", "    Properties {\n" + FluxParticleProgramShaderGenerator.GenerateParameterProperties(program.Compilation) + "\n    }\n    SubShader");
        }
    }
}
