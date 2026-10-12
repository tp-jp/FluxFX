using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// Drag ModuleのGPUパラメータと実行Operationを定義します。
    /// VelocityのWorking状態に対して減衰を適用します。
    /// </summary>
    public sealed class DragGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleDragModule>
    {
        /// <summary>
        /// Drag Moduleの識別子です。
        /// </summary>
        public override string ModuleId => "tplab.fluxfx.drag";

        /// <summary>
        /// DragのGPU実装を保持するHLSLファイルです。
        /// </summary>
        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Drag.hlsl";

        /// <summary>
        /// DragのHLSL関数名です。
        /// </summary>
        public override string EntryPoint => "FluxFX_Drag";

        /// <summary>
        /// Velocity Update内での実行優先順位です。
        /// </summary>
        public override int Order => 500;

        /// <summary>
        /// 減衰係数をGPUパラメータとして登録します。
        /// </summary>
        /// <param name="module">Drag設定を保持するModule。</param>
        /// <param name="collector">GPUパラメータの登録先。</param>
        public override void CollectParameters(FluxParticleDragModule module, FluxParticleParameterCollector collector)
        {
            collector.AddFloat("_Drag", module.Drag);
        }

        /// <summary>
        /// VelocityのWorking状態へ減衰を適用するUpdate Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるDrag Module。</param>
        /// <returns>DragのOperation一覧。</returns>
        public override IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleDragModule module)
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