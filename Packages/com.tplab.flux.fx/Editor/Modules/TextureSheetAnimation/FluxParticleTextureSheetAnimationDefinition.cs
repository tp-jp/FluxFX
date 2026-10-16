using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.TextureSheetAnimation
{
    /// <summary>
    /// Texture Sheet AnimationのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleTextureSheetAnimationDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleTextureSheetAnimationModule);

        public string ModuleId => FluxParticleTextureSheetAnimationContract.Module.Id;

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var sheet = (FluxParticleTextureSheetAnimationModule)module;
            var contribution = new FluxParticleModuleContribution(context, FluxParticleTextureSheetAnimationContract.Module, sheet.Enabled);
            var tiles = new Vector2(sheet.TilesX, sheet.TilesY);
            if ((double)tiles.x != sheet.TilesX || (double)tiles.y != sheet.TilesY)
            {
                throw new InvalidOperationException("Texture sheet tile counts must be exactly representable as floats.");
            }
            var tilesParameter = contribution.Bind(FluxParticleTextureSheetAnimationContract.Tiles, tiles);
            var cyclesParameter = contribution.Bind(FluxParticleTextureSheetAnimationContract.Cycles, sheet.Cycles);
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
