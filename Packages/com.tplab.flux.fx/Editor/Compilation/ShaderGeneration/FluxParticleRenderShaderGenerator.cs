using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Editor.Compilation.Compiler;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Compilation.ShaderGeneration
{
    /// <summary>
    /// ParticleのBillboard・Mesh描画Shaderを生成します。
    /// </summary>
    public static class FluxParticleRenderShaderGenerator
    {
        /// <summary>
        /// コンパイル済みの描画定義からShaderLabソースを生成します。
        /// </summary>
        /// <param name="program">ParticleのShader生成結果</param>
        /// <param name="mode">Particleの描画方式</param>
        /// <param name="worldSpace">Stateの位置をワールド座標として扱うか</param>
        /// <returns>ShaderLabソース</returns>
        public static string Generate(FluxParticleShaderProgram program, FluxParticleRenderMode mode = FluxParticleRenderMode.Billboard, bool worldSpace = false)
        {
            if (mode != FluxParticleRenderMode.Billboard && mode != FluxParticleRenderMode.Mesh)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }
            if (!program.Compilation.StateLayout.TryGetBinding("particle.position", out var position))
            {
                throw new InvalidOperationException("Position State is required for particle rendering.");
            }
            var sampler = FluxParticleHlslNames.PageProperty(position.PageId, FluxParticlePageVersion.Updated);
            var channels = "rgba".Substring(position.ChannelOffset, position.ComponentCount);
            var declaration = "sampler2D " + sampler + ";";
            var extra = program.RenderSource.Contains(declaration) ? "" : declaration + "\n";
            var pageProperties = string.Join("\n", program.Compilation.StateLayout.Pages.Select(page =>
                $"[HideInInspector] {FluxParticleHlslNames.PageProperty(page.Id, FluxParticlePageVersion.Updated)} (\"State Page {page.Id}\", 2D) = \"black\" {{}}"));
            if (program.Compilation.Providers.ContainsKey("step.time"))
            {
                pageProperties += "\n[HideInInspector] " + FluxParticleHlslNames.ProviderProperty("step.time") +
                    " (\"Simulation Time\", Float) = 0";
            }
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
            var vertexCode = "float3 center = " + (worldSpace ? "position" : "mul(unity_ObjectToWorld, float4(position, 1.0)).xyz") + ";\n" +
                "float3 right = UNITY_MATRIX_I_V._m00_m10_m20;\nfloat3 up = UNITY_MATRIX_I_V._m01_m11_m21;\n" +
                rotationCode + "\nfloat3 vertex = center + (right * quad.x + up * quad.y) * render." + FluxParticleHlslNames.Value("render.size") + ";";
            if (mode == FluxParticleRenderMode.Mesh)
            {
                var hasRotation = program.Compilation.Operations.Any(operation => operation.Id == "render.rotation-start");
                vertexCode = "float3 local = input.vertex.xyz;\n";
                if (hasRotation)
                {
                    vertexCode += "float3 s, c;\nsincos(radians(render." + FluxParticleHlslNames.Value("render.rotation") + "), s, c);\n" +
                        "local.xy = float2(local.x * c.z - local.y * s.z, local.x * s.z + local.y * c.z);\n" +
                        "local.yz = float2(local.y * c.x - local.z * s.x, local.y * s.x + local.z * c.x);\n" +
                        "local.xz = float2(local.x * c.y + local.z * s.y, -local.x * s.y + local.z * c.y);\n";
                }
                vertexCode += "float3 vertex = position + local * render." + FluxParticleHlslNames.Value("render.size") + ";\n";
                if (!worldSpace)
                {
                    vertexCode += "vertex = mul(unity_ObjectToWorld, float4(vertex, 1.0)).xyz;\n";
                }
            }
            return FluxParticleProgramShaderGenerator.ExpandTemplate("ParticleRender.shader.template", new Dictionary<string, string>
            {
                ["ShaderName"] = "Hidden/FluxFX/Program/" + mode,
                ["VertexCode"] = vertexCode,
                ["PageProperties"] = pageProperties,
                ["ParameterProperties"] = FluxParticleProgramShaderGenerator.GenerateParameterProperties(program.Compilation),
                ["RenderCode"] = renderCode,
                ["Position"] = $"tex2Dlod({sampler}, float4(stateUV, 0.0, 0.0)).{channels}",
                ["Color"] = "render." + FluxParticleHlslNames.Value("render.color"),
                ["UvTransform"] = "render." + FluxParticleHlslNames.Value("render.uv-transform"),
            });
        }
    }
}
