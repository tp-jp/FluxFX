using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// Gravity ModuleのGPUパラメータと実行Operationを定義します。
    /// Velocity PassのUpdate Phaseに重力計算を配置します。
    /// </summary>
    public sealed class GravityGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleGravityModule>
    {
        /// <summary>
        /// Gravity Moduleの識別子です。
        /// </summary>
        public override string ModuleId => "tplab.fluxfx.gravity";

        /// <summary>
        /// GravityのGPU実装を保持するHLSLファイルです。
        /// </summary>
        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Gravity.hlsl";

        /// <summary>
        /// GravityのHLSL関数名です。
        /// </summary>
        public override string EntryPoint => "FluxFX_Gravity";

        /// <summary>
        /// Velocity Update内での実行優先順位です。
        /// </summary>
        public override int Order => 100;

        /// <summary>
        /// 重力ベクトルをGPUパラメータとして登録します。
        /// </summary>
        /// <param name="module">Gravity設定を保持するModule。</param>
        /// <param name="collector">GPUパラメータの登録先。</param>
        public override void CollectParameters(FluxParticleGravityModule module, FluxParticleParameterCollector collector)
        {
            collector.AddVector("_Gravity", module.Gravity);
        }

        /// <summary>
        /// VelocityのWorking状態を読み書きするUpdate Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるGravity Module。</param>
        /// <returns>Gravityを適用するOperation。</returns>
        public override IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleGravityModule module)
        {
            yield return new FluxParticleOperation(
                ModuleId,
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Velocity,
                Order,
                new[]
                {
                    new FluxParticleStateRead(
                        FluxParticleAttribute.Velocity,
                        FluxParticleStateReadSource.Working)
                });
        }
    }
}