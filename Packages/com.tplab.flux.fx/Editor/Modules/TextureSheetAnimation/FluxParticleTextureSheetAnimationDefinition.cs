using System;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// Texture Sheet AnimationのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleTextureSheetAnimationDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleTextureSheetAnimationModule);
        public string ModuleId => "tplab.fluxfx.texture-sheet-animation";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var sheet = (FluxParticleTextureSheetAnimationModule)module;
            if (sheet.TilesX < 1 || sheet.TilesY < 1 || (long)sheet.TilesX * sheet.TilesY > 16777216)
            {
                throw new InvalidOperationException("Texture sheet tile counts must be positive and the frame count must fit float integer precision.");
            }
            if (float.IsNaN(sheet.Cycles) || float.IsInfinity(sheet.Cycles) || sheet.Cycles < 0)
            {
                throw new InvalidOperationException("Texture sheet cycles must be finite and non-negative.");
            }
            context.AddParameter(new FluxParticleParameterDefinition("texture-sheet.tiles", FluxParticleValueType.Float2,
                FluxParticleParameterPolicy.Static), new Vector4(sheet.TilesX, sheet.TilesY, 0, 0));
            context.AddParameter(new FluxParticleParameterDefinition("texture-sheet.cycles", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(sheet.Cycles, 0, 0, 0));
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
                new[] { "texture-sheet.tiles", "texture-sheet.cycles" }));
        }
    }
}