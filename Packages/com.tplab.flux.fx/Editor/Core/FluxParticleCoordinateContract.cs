using TpLab.Flux.FX.Editor.Compilation.Contracts;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts;

namespace TpLab.Flux.FX.Editor.Core
{
    /// <summary>
    /// 座標の共有契約。EffectScaleは読み取り専用Providerで、粒子位置は基準座標です。
    /// </summary>
    public static class FluxParticleCoordinateContract
    {
        /// <summary>
        /// 確定倍率のValue IDとParameter ID。OperationからはProvidedとして読み取ります。
        /// </summary>
        public const string EffectScale = "system.effect-scale";

        /// <summary>
        /// 倍率を適用していない粒子位置のPersistent Value ID
        /// </summary>
        public const string ReferencePosition = "particle.position";

        /// <summary>
        /// Localシミュレーションの座標・距離変換を提供するHLSL
        /// </summary>
        public const string Hlsl = "Packages/com.tplab.flux.fx/Runtime/Shaders/Coordinates.hlsl";

        /// <summary>
        /// 表示倍率をビルド値へ固定し、Executor経由でも描画・カリングとの不一致を拒否します。
        /// </summary>
        internal static FluxParticleParameterDefinition CreateScaleParameter(float scale)
        {
            return FluxParticleParameterContractCompiler.Compile(FluxParticleParameter.Float(EffectScale).ApiName("EffectScale")
                .Policy(Contracts.FluxParticleParameterPolicy.Static).Minimum(scale).Maximum(scale).Build());
        }
    }
}
