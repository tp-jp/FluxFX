using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// Noise ModuleのGPUパラメータと実行Operationを定義します。
    /// 前フレームのPositionに基づくノイズをVelocityのWorking状態へ加えます。
    /// </summary>
    public sealed class NoiseGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleNoiseModule>
    {
        /// <summary>
        /// Noise Moduleの識別子です。
        /// </summary>
        public override string ModuleId => "tplab.fluxfx.noise";

        /// <summary>
        /// NoiseのGPU実装を保持するHLSLファイルです。
        /// </summary>
        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Noise.hlsl";

        /// <summary>
        /// NoiseのHLSL関数名です。
        /// </summary>
        public override string EntryPoint => "FluxFX_Noise";

        /// <summary>
        /// Velocity Update内での実行優先順位です。
        /// </summary>
        public override int Order => 300;

        /// <summary>
        /// ノイズの強度、空間スケール、時間変化速度をGPUパラメータとして登録します。
        /// </summary>
        /// <param name="module">Noise設定を保持するModule。</param>
        /// <param name="collector">GPUパラメータの登録先。</param>
        public override void CollectParameters(FluxParticleNoiseModule module, FluxParticleParameterCollector collector)
        {
            collector.AddFloat("_NoiseStrength", module.Strength);
            collector.AddFloat("_NoiseScale", module.Scale);
            collector.AddFloat("_NoiseSpeed", module.Speed);
        }

        /// <summary>
        /// PositionのPrevious状態からノイズを計算し、VelocityのWorking状態を更新するOperationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるNoise Module。</param>
        /// <returns>NoiseのOperation一覧。</returns>
        public override IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleNoiseModule module)
        {
            yield return new FluxParticleOperation(
                ModuleId,
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Position | FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Velocity,
                Order,
                new[]
                {
                    new FluxParticleStateRead(
                        FluxParticleAttribute.Position,
                        FluxParticleStateReadSource.Previous),

                    new FluxParticleStateRead(
                        FluxParticleAttribute.Velocity,
                        FluxParticleStateReadSource.Working)
                });
        }
    }
}