using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.TextureSheetAnimation
{
    /// <summary>
    /// Texture Sheet AnimationのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleTextureSheetAnimationDefinition : IFluxParticleModuleDefinition, IFluxParticleModuleBindingDefinition
    {
        public Type ModuleType => typeof(FluxParticleTextureSheetAnimationModule);
        public string ModuleId => "tplab.fluxfx.texture-sheet-animation";
        public string EnabledParameterId => "texture-sheet.enabled";
        public FluxParticleParameterDefinition[] GetHandleParameters(FluxParticleModule module) => new FluxParticleParameterDefinition[]
        {
            new FluxParticleParameterDefinition("texture-sheet.tiles", FluxParticleValueType.Float2, FluxParticleParameterPolicy.Static),
            new FluxParticleParameterDefinition("texture-sheet.cycles", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live)
        };

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            context.AddParameter(new FluxParticleParameterDefinition(EnabledParameterId, FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new UnityEngine.Vector4(1, 0, 0, 0));
            var handleParameters = GetHandleParameters(module);
            var sheet = (FluxParticleTextureSheetAnimationModule)module;
            if (sheet.TilesX < 1 || sheet.TilesY < 1 || (long)sheet.TilesX * sheet.TilesY > 16777216)
            {
                throw new InvalidOperationException("Texture sheet tile counts must be positive and the frame count must fit float integer precision.");
            }
            if (float.IsNaN(sheet.Cycles) || float.IsInfinity(sheet.Cycles) || sheet.Cycles < 0)
            {
                throw new InvalidOperationException("Texture sheet cycles must be finite and non-negative.");
            }
            context.AddParameter(handleParameters[0], new Vector4(sheet.TilesX, sheet.TilesY, 0, 0));
            context.AddParameter(handleParameters[1], new Vector4(sheet.Cycles, 0, 0, 0));
            context.AddOperation(new FluxParticleOperationDefinition("render.texture-sheet-animation", ModuleId, FluxParticlePhase.Render,
                new[]
                {
                    new FluxParticleValueRead("render.uv-transform", FluxParticleReadVersion.AfterOperation, "render.uv-start"),
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("render.uv-transform", FluxParticleWriteKind.Modify) },
                new[] { "render.uv-start" },
                "Packages/com.tplab.flux.fx/Editor/Modules/TextureSheetAnimation/TextureSheetAnimation.hlsl", "FluxFX_TextureSheetAnimation",
                new[] { "texture-sheet.tiles", "texture-sheet.cycles" }, EnabledParameterId));
        }
    }
}