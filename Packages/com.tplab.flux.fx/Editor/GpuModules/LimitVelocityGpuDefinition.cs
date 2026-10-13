using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// LimitVelocity ModuleのGPUパラメータと実行Operationを定義します。
    /// Velocity Updateの最終段階で粒子の速度上限を適用します。
    /// </summary>
    public sealed class LimitVelocityGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleLimitVelocityModule>
    {
        /// <summary>
        /// LimitVelocity Moduleの識別子です。
        /// </summary>
        public override string ModuleId => "tplab.fluxfx.limit-velocity";

        /// <summary>
        /// LimitVelocityのGPU実装を保持するHLSLファイルです。
        /// </summary>
        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/LimitVelocity.hlsl";

        /// <summary>
        /// LimitVelocityのHLSL関数名です。
        /// </summary>
        public override string EntryPoint => "FluxFX_LimitVelocity";

        /// <summary>
        /// 他のVelocity Moduleによる変更後に速度上限を適用するための実行優先順位です。
        /// </summary>
        public override int Order => 600;

        /// <summary>
        /// 速度の上限値をGPUパラメータとして登録します。
        /// </summary>
        /// <param name="module">LimitVelocity設定を保持するModule。</param>
        /// <param name="collector">GPUパラメータの登録先。</param>
        public override void CollectParameters(FluxParticleLimitVelocityModule module, FluxParticleParameterCollector collector)
        {
            collector.AddFloat("_MaxSpeed", module.MaxSpeed);
        }

        /// <summary>
        /// Working Velocityの大きさを制限するUpdate Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるLimitVelocity Module。</param>
        /// <returns>LimitVelocityのOperation一覧。</returns>
        public override IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleLimitVelocityModule module)
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