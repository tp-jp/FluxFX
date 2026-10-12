using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// Force ModuleのGPUパラメータと実行Operationを定義します。
    /// VelocityのWorking状態へ一定の力を適用します。
    /// </summary>
    public sealed class ForceGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleForceModule>
    {
        /// <summary>
        /// Force Moduleの識別子です。
        /// </summary>
        public override string ModuleId => "tplab.fluxfx.force";

        /// <summary>
        /// ForceのGPU実装を保持するHLSLファイルです。
        /// </summary>
        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Force.hlsl";

        /// <summary>
        /// ForceのHLSL関数名です。
        /// </summary>
        public override string EntryPoint => "FluxFX_Force";

        /// <summary>
        /// Velocity Update内での実行優先順位です。
        /// </summary>
        public override int Order => 200;

        /// <summary>
        /// 力のベクトルをGPUパラメータとして登録します。
        /// </summary>
        /// <param name="module">Force設定を保持するModule。</param>
        /// <param name="collector">GPUパラメータの登録先。</param>
        public override void CollectParameters(FluxParticleForceModule module, FluxParticleParameterCollector collector)
        {
            collector.AddVector("_Force", module.Force);
        }

        /// <summary>
        /// VelocityのWorking状態へ力を加えるUpdate Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるForce Module。</param>
        /// <returns>ForceのOperation一覧。</returns>
        public override IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleForceModule module)
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