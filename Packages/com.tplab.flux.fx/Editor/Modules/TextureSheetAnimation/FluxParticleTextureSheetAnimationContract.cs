using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Editor.Contracts;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.TextureSheetAnimation
{
    /// <summary>
    /// TextureSheetAnimationの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleTextureSheetAnimationContract
    {
        /// <summary>
        /// タイル分割数の契約
        /// </summary>
        public static FluxParticleParameterContract<Vector2> Tiles { get; } =
            FluxParticleParameter.Vector2("texture-sheet.tiles")
                .ApiName("Tiles")
                .Policy(FluxParticleParameterPolicy.Static)
                .Range(Vector2.one, new Vector2(16777216, 16777216))
                .Integer()
                .MaximumProduct(16777216)
                .Build();

        /// <summary>
        /// 繰り返し回数の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Cycles { get; } =
            FluxParticleParameter.Float("texture-sheet.cycles")
                .ApiName("Cycles")
                .Policy(FluxParticleParameterPolicy.Live)
                .Minimum(0)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder("tplab.fluxfx.texture-sheet-animation")
                .Handle("FluxParticleTextureSheetAnimationHandle")
                .Accessor("TextureSheetAnimation")
                .Enabled("texture-sheet.enabled")
                .Parameter(FluxParticleTextureSheetAnimationParameter.Tiles, Tiles)
                .Parameter(FluxParticleTextureSheetAnimationParameter.Cycles, Cycles)
                .Build();
    }
}
