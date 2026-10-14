using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        /// <param name="program">コンパイル済みのParticle Shader Program。</param>
        /// <param name="initialValues">Parameterの初期値。</param>
        /// <returns>ShaderLabソース。</returns>
        public static string Generate(FluxParticleShaderProgram program, IReadOnlyDictionary<string, Vector4> initialValues)
        {
            if (!program.Program.StateLayout.TryGetBinding("particle.position", out var position))
            {
                throw new InvalidOperationException("Position State is required for Billboard rendering.");
            }
            var sampler = FluxParticleHlslNames.PageProperty(position.PageId, FluxParticlePageVersion.Updated);
            var channels = "rgba".Substring(position.ChannelOffset, position.ComponentCount);
            var declaration = "sampler2D " + sampler + ";";
            var extra = program.RenderSource.Contains(declaration) ? "" : declaration + "\n";
            var pageProperties = string.Join("\n", program.Program.StateLayout.Pages.Select(page =>
                $"[HideInInspector] {FluxParticleHlslNames.PageProperty(page.Id, FluxParticlePageVersion.Updated)} (\"State Page {page.Id}\", 2D) = \"black\" {{}}"));
            var randomSize = initialValues.ContainsKey("render.size-range");
            var sizeExpression = FluxParticleHlslNames.ParameterProperty("render.size");
            var renderCode = extra + program.RenderSource;
            if (randomSize)
            {
                if (!program.Program.StateLayout.TryGetBinding("particle.spawn-key", out var spawnKey))
                {
                    throw new InvalidOperationException("Random start size requires particle.spawn-key State.");
                }
                var keySampler = FluxParticleHlslNames.PageProperty(spawnKey.PageId, FluxParticlePageVersion.Updated);
                var keyDeclaration = "sampler2D " + keySampler + ";";
                if (!renderCode.Contains(keyDeclaration)) renderCode = keyDeclaration + "\n" + renderCode;
                renderCode = "#include \"Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl\"\n" + renderCode;
                var range = FluxParticleHlslNames.ParameterProperty("render.size-range");
                var spawnKeyExpression = $"(uint)tex2Dlod({keySampler}, float4(stateUV, 0.0, 0.0)).{"rgba"[spawnKey.ChannelOffset]}";
                sizeExpression = $"lerp({range}.x, {range}.y, FluxFXRandom01({spawnKeyExpression} * 59u + 43u))";
            }
            var rotationCode = program.Program.Operations.Any(operation => operation.Id == "render.rotation-start")
                ? "float angle = radians(render." + FluxParticleHlslNames.Value("render.rotation") + ".z);\n" +
                  "float s;\n" +
                  "float c;\n" +
                  "sincos(angle, s, c);\n" +
                  "float2 quad = float2(input.vertex.x * c - input.vertex.y * s, input.vertex.x * s + input.vertex.y * c);"
                : "float2 quad = input.vertex.xy;";
            return FluxParticleProgramShaderGenerator.ExpandTemplate("Billboard", new Dictionary<string, string>
            {
                ["PageProperties"] = pageProperties,
                ["RenderCode"] = renderCode,
                ["Position"] = $"tex2Dlod({sampler}, float4(stateUV, 0.0, 0.0)).{channels}",
                ["Color"] = "render." + FluxParticleHlslNames.Value("render.color"),
                ["Size"] = sizeExpression,
                ["RotationCode"] = rotationCode
            });
        }
    }
}
