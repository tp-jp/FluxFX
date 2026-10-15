using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.TextureSheetAnimation
{
    /// <summary>
    /// Texture Sheet AnimationのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleTextureSheetAnimationDefinition : IFluxParticleModuleDefinition
    {
        const int TilesSlot = 0;
        const int CyclesSlot = 1;

        public Type ModuleType => typeof(FluxParticleTextureSheetAnimationModule);

        public string ModuleId => "tplab.fluxfx.texture-sheet-animation";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var sheet = (FluxParticleTextureSheetAnimationModule)module;
            var contribution = new FluxParticleModuleContribution(context, ModuleId, "texture-sheet.enabled", sheet.Enabled, 2);
            if (sheet.TilesX < 1 || sheet.TilesY < 1 || (long)sheet.TilesX * sheet.TilesY > 16777216)
            {
                throw new InvalidOperationException("Texture sheet tile counts must be positive and the frame count must fit float integer precision.");
            }
            if (float.IsNaN(sheet.Cycles) || float.IsInfinity(sheet.Cycles) || sheet.Cycles < 0)
            {
                throw new InvalidOperationException("Texture sheet cycles must be finite and non-negative.");
            }
            var tilesParameter = contribution.Parameter(TilesSlot, "texture-sheet.tiles", FluxParticleValueType.Float2,
                FluxParticleParameterPolicy.Static, new Vector4(sheet.TilesX, sheet.TilesY, 0, 0));
            var cyclesParameter = contribution.Parameter(CyclesSlot, "texture-sheet.cycles", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live, new Vector4(sheet.Cycles, 0, 0, 0));
            contribution.Operation("render.texture-sheet-animation", FluxParticlePhase.Render,
                reads: new[]
                {
                    new FluxParticleValueRead("render.uv-transform", FluxParticleReadVersion.AfterOperation, "render.uv-start"),
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided)
                },
                writes: new[] { new FluxParticleValueWrite("render.uv-transform", FluxParticleWriteKind.Modify) },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/TextureSheetAnimation/TextureSheetAnimation.hlsl", entryPoint: "FluxFX_TextureSheetAnimation",
                switchable: true, parameters: new[] { tilesParameter, cyclesParameter });
            contribution.Complete();
        }
    }
}
