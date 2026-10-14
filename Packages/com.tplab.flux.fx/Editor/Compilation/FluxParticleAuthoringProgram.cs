using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// 既存Authoringの基本構成を新Compilerの宣言とParameter初期値へ変換します。
    /// 未移行の設定は無視せず、コンパイル前に検出します。
    /// </summary>
    public sealed class FluxParticleAuthoringProgram
    {
        /// <summary>
        /// State LayoutとShaderを含むコンパイル済みProgramです。
        /// </summary>
        public FluxParticleShaderProgram Program { get; }

        /// <summary>
        /// 論理Parameter IDに対応する初期値です。Floatはx成分を使用します。
        /// </summary>
        public IReadOnlyDictionary<string, Vector4> InitialValues { get; }

        /// <summary>
        /// Authoringを検証し、対応するModuleだけから基本Programを構築します。
        /// </summary>
        /// <param name="authoring">変換対象。</param>
        public FluxParticleAuthoringProgram(FluxParticleAuthoring authoring)
        {
            FluxParticleAuthoringValidator.Validate(authoring);
            var definitions = FluxParticleAuthoringDefinitionBuilder.Create(authoring, out var source, out var moduleContext, out var hasRotation);
            FluxParticleAuthoringDefinitionBuilder.AddProviders(authoring, source, definitions);
            FluxParticleAuthoringDefinitionBuilder.AddOperationsAndParameters(authoring, source, definitions, hasRotation);
            var values = FluxParticleAuthoringInitialValues.Create(authoring, moduleContext.InitialValues, hasRotation);
            Program = new FluxParticleProgramComposer().Compose(definitions, "Hidden/FluxFX/Program", FluxParticleCoreDefinitions.CreateLifecycle());
            InitialValues = new System.Collections.ObjectModel.ReadOnlyDictionary<string, Vector4>(values);
        }

        /// <summary>
        /// Render Operationの出力とState Layoutを使い、Billboard用Shaderを生成します。
        /// </summary>
        /// <returns>UnityへImportするShaderLabソース。</returns>
        public string GenerateRenderer()
        {
            if (!Program.Program.StateLayout.TryGetBinding("particle.position", out var position))
            {
                throw new InvalidOperationException("Position State is required for Billboard rendering.");
            }
            var sampler = FluxParticleHlslNames.PageProperty(position.PageId, FluxParticlePageVersion.Updated);
            var channels = "rgba".Substring(position.ChannelOffset, position.ComponentCount);
            // RenderSource only declares Pages that its Operations read. Position may need a separate sampler.
            var declaration = "sampler2D " + sampler + ";";
            var extra = Program.RenderSource.Contains(declaration) ? "" : declaration + "\n";
            var pageProperties = string.Join("\n", Program.Program.StateLayout.Pages.Select(page =>
                $"[HideInInspector] {FluxParticleHlslNames.PageProperty(page.Id, FluxParticlePageVersion.Updated)} (\"State Page {page.Id}\", 2D) = \"black\" {{}}"));
            var randomSize = InitialValues.ContainsKey("render.size-range");
            var sizeExpression = FluxParticleHlslNames.ParameterProperty("render.size");
            var renderCode = extra + Program.RenderSource;
            if (randomSize)
            {
                if (!Program.Program.StateLayout.TryGetBinding("particle.spawn-key", out var spawnKey))
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
            var rotationCode = Program.Program.Operations.Any(operation => operation.Id == "render.rotation-start")
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

        static bool FiniteNonNegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;

        static void Require(bool valid, string requirement)
        {
            if (valid)
            {
                return;
            }
            throw new InvalidOperationException($"Program Backend currently requires: {requirement}.");
        }
    }
}
