using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// Vortex ModuleのGPUパラメータと実行Operationを定義します。
    /// 前フレームのPositionから旋回方向を計算し、VelocityのWorking状態へ加えます。
    /// </summary>
    public sealed class VortexGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleVortexModule>
    {
        /// <summary>
        /// Vortex Moduleの識別子です。
        /// </summary>
        public override string ModuleId => "tplab.fluxfx.vortex";

        /// <summary>
        /// VortexのGPU実装を保持するHLSLファイルです。
        /// </summary>
        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Vortex.hlsl";

        /// <summary>
        /// VortexのHLSL関数名です。
        /// </summary>
        public override string EntryPoint => "FluxFX_Vortex";

        /// <summary>
        /// Velocity Update内での実行優先順位です。
        /// </summary>
        public override int Order => 400;

        /// <summary>
        /// 渦の中心、正規化した軸、強度をGPUパラメータとして登録します。
        /// </summary>
        /// <param name="module">Vortex設定を保持するModule。</param>
        /// <param name="collector">GPUパラメータの登録先。</param>
        public override void CollectParameters(FluxParticleVortexModule module, FluxParticleParameterCollector collector)
        {
            collector.AddVector("_VortexCenter", module.Center);
            collector.AddVector("_VortexAxis", module.Axis.normalized);
            collector.AddFloat("_VortexStrength", module.Strength);
        }

        /// <summary>
        /// Previous PositionとWorking Velocityを使うUpdate Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるVortex Module。</param>
        /// <returns>VortexのOperation一覧。</returns>
        public override IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleVortexModule module)
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