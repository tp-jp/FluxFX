using System;
using System.Collections.Generic;
using System.Linq;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// ParticleのBillboard描画Shaderを生成します。
    /// </summary>
    public static class FluxParticleBillboardShaderGenerator
    {
        /// <summary>
        /// コンパイル済みの描画定義からShaderLabソースを生成します。
        /// </summary>
        /// <param name="program">ParticleのShader生成結果</param>
        /// <returns>ShaderLabソース</returns>
        public static string Generate(FluxParticleShaderProgram program)
        {
            if (!program.Compilation.StateLayout.TryGetBinding("particle.position", out var position))
            {
                throw new InvalidOperationException("Position State is required for Billboard rendering.");
            }
            var sampler = FluxParticleHlslNames.PageProperty(position.PageId, FluxParticlePageVersion.Updated);
            var channels = "rgba".Substring(position.ChannelOffset, position.ComponentCount);
            var declaration = "sampler2D " + sampler + ";";
            var extra = program.RenderSource.Contains(declaration) ? "" : declaration + "\n";
            var pageProperties = string.Join("\n", program.Compilation.StateLayout.Pages.Select(page =>
                $"[HideInInspector] {FluxParticleHlslNames.PageProperty(page.Id, FluxParticlePageVersion.Updated)} (\"State Page {page.Id}\", 2D) = \"black\" {{}}"));
            foreach (var provider in program.Compilation.Providers.Values.Where(provider => provider.Kind == FluxParticleProviderKind.LookupTable)
                .OrderBy(provider => provider.ValueId, StringComparer.Ordinal))
            {
                pageProperties += Environment.NewLine + "[HideInInspector] " + FluxParticleHlslNames.ProviderProperty(provider.ValueId) +
                    @" (""Lookup Table"", 2D) = ""white"" {}";
            }
            var renderCode = extra + program.RenderSource;
            var rotationCode = program.Compilation.Operations.Any(operation => operation.Id == "render.rotation-start")
                ? "float angle = radians(render." + FluxParticleHlslNames.Value("render.rotation") + ".z);\n" +
                  "float s;\n" +
                  "float c;\n" +
                  "sincos(angle, s, c);\n" +
                  "float2 quad = float2(input.vertex.x * c - input.vertex.y * s, input.vertex.x * s + input.vertex.y * c);"
                : "float2 quad = input.vertex.xy;";
            return FluxParticleProgramShaderGenerator.ExpandTemplate("Billboard", new Dictionary<string, string>
            {
                ["PageProperties"] = pageProperties,
                ["ParameterProperties"] = FluxParticleProgramShaderGenerator.GenerateParameterProperties(program.Compilation),
                ["RenderCode"] = renderCode,
                ["Position"] = $"tex2Dlod({sampler}, float4(stateUV, 0.0, 0.0)).{channels}",
                ["Color"] = "render." + FluxParticleHlslNames.Value("render.color"),
                ["Size"] = "render." + FluxParticleHlslNames.Value("render.size"),
                ["RotationCode"] = rotationCode
            });
        }
    }
}
