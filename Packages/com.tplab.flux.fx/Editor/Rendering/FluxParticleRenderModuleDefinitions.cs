using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Rendering
{
    /// <summary>
    /// ColorOverLifetimeの描画評価に必要なOperationを定義します。
    /// 描画時にAge、Lifetime、StartColorを参照します。
    /// </summary>
    public sealed class ColorOverLifetimeRenderDefinition : FluxParticleRenderModuleDefinition<FluxParticleColorOverLifetimeModule>
    {
        /// <summary>
        /// ColorOverLifetime Moduleの識別子です。
        /// </summary>
        public override string ModuleId => "tplab.fluxfx.color-over-lifetime";

        /// <summary>
        /// Simulation後のAge、Lifetime、StartColorを参照するRendering Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるColorOverLifetime Module。</param>
        /// <returns>Color評価用Operation一覧。</returns>
        public override IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleColorOverLifetimeModule module)
        {
            yield return new FluxParticleOperation(
                ModuleId,
                "evaluate",
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render,
                FluxParticleAttribute.Age |
                FluxParticleAttribute.Lifetime |
                FluxParticleAttribute.StartColor,
                FluxParticleAttribute.None,
                100,
                new[]
                {
                    new FluxParticleStateRead(FluxParticleAttribute.Age, FluxParticleStateReadSource.Updated),
                    new FluxParticleStateRead(FluxParticleAttribute.Lifetime, FluxParticleStateReadSource.Updated),
                    new FluxParticleStateRead(FluxParticleAttribute.StartColor, FluxParticleStateReadSource.Updated)
                });
        }
    }

    /// <summary>
    /// SizeOverLifetimeの描画評価に必要なOperationを定義します。
    /// 描画時にAge、Lifetime、StartSizeを参照します。
    /// </summary>
    public sealed class SizeOverLifetimeRenderDefinition : FluxParticleRenderModuleDefinition<FluxParticleSizeOverLifetimeModule>
    {
        /// <summary>
        /// SizeOverLifetime Moduleの識別子です。
        /// </summary>
        public override string ModuleId => "tplab.fluxfx.size-over-lifetime";

        /// <summary>
        /// Simulation後のAge、Lifetime、StartSizeを参照するRendering Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるSizeOverLifetime Module。</param>
        /// <returns>Size評価用Operation一覧。</returns>
        public override IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleSizeOverLifetimeModule module)
        {
            yield return new FluxParticleOperation(
                ModuleId,
                "evaluate",
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render,
                FluxParticleAttribute.Age |
                FluxParticleAttribute.Lifetime |
                FluxParticleAttribute.StartSize,
                FluxParticleAttribute.None,
                100,
                new[]
                {
                    new FluxParticleStateRead(FluxParticleAttribute.Age, FluxParticleStateReadSource.Updated),
                    new FluxParticleStateRead(FluxParticleAttribute.Lifetime, FluxParticleStateReadSource.Updated),
                    new FluxParticleStateRead(FluxParticleAttribute.StartSize, FluxParticleStateReadSource.Updated)
                });
        }
    }

    /// <summary>
    /// RotationOverLifetimeの描画評価に必要なOperationを定義します。
    /// 現行Rendererと同様に、回転の時間変化は描画側で評価します。
    /// </summary>
    public sealed class RotationOverLifetimeRenderDefinition : FluxParticleRenderModuleDefinition<FluxParticleRotationOverLifetimeModule>
    {
        /// <summary>
        /// RotationOverLifetime Moduleの識別子です。
        /// </summary>
        public override string ModuleId => "tplab.fluxfx.rotation-over-lifetime";

        /// <summary>
        /// Simulation後のAge、Lifetime、StartRotationを参照するRendering Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるRotationOverLifetime Module。</param>
        /// <returns>Rotation評価用Operation一覧。</returns>
        public override IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleRotationOverLifetimeModule module)
        {
            yield return new FluxParticleOperation(
                ModuleId,
                "evaluate",
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render,
                FluxParticleAttribute.Age |
                FluxParticleAttribute.Lifetime |
                FluxParticleAttribute.StartRotation,
                FluxParticleAttribute.None,
                100,
                new[]
                {
                    new FluxParticleStateRead(FluxParticleAttribute.Age, FluxParticleStateReadSource.Updated),
                    new FluxParticleStateRead(FluxParticleAttribute.Lifetime, FluxParticleStateReadSource.Updated),
                    new FluxParticleStateRead(FluxParticleAttribute.StartRotation, FluxParticleStateReadSource.Updated)
                });
        }
    }

    /// <summary>
    /// TextureSheetAnimationの描画評価に必要なOperationを定義します。
    /// Particleの正規化Ageに基づいて描画時のUVを計算するため、状態Bufferは変更しません。
    /// </summary>
    public sealed class TextureSheetAnimationRenderDefinition : FluxParticleRenderModuleDefinition<FluxParticleTextureSheetAnimationModule>
    {
        /// <summary>
        /// TextureSheetAnimation Moduleの識別子です。
        /// </summary>
        public override string ModuleId => "tplab.fluxfx.texture-sheet-animation";

        /// <summary>
        /// Simulation後のAgeとLifetimeを参照するRendering Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるTextureSheetAnimation Module。</param>
        /// <returns>UV評価用Operation一覧。</returns>
        public override IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleTextureSheetAnimationModule module)
        {
            yield return new FluxParticleOperation(
                ModuleId,
                "evaluate",
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render,
                FluxParticleAttribute.Age |
                FluxParticleAttribute.Lifetime,
                FluxParticleAttribute.None,
                100,
                new[]
                {
                    new FluxParticleStateRead(FluxParticleAttribute.Age, FluxParticleStateReadSource.Updated),
                    new FluxParticleStateRead(FluxParticleAttribute.Lifetime, FluxParticleStateReadSource.Updated)
                });
        }
    }
}