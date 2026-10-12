using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// VelocityOverLifetime ModuleのGPUパラメータと実行Operationを定義します。
    /// Position Passで更新済みVelocityを参照し、粒子位置を補正します。
    /// </summary>
    public sealed class VelocityOverLifetimeGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleVelocityOverLifetimeModule>
    {
        /// <summary>
        /// VelocityOverLifetime Moduleの識別子です。
        /// </summary>
        public override string ModuleId => "tplab.fluxfx.velocity-over-lifetime";

        /// <summary>
        /// VelocityOverLifetimeのGPU実装を保持するHLSLファイルです。
        /// </summary>
        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/VelocityOverLifetime.hlsl";

        /// <summary>
        /// VelocityOverLifetimeのHLSL関数名です。
        /// </summary>
        public override string EntryPoint => "FluxFX_VelocityOverLifetime";

        /// <summary>
        /// Position Update内での実行優先順位です。
        /// </summary>
        public override int Order => 100;

        /// <summary>
        /// 速度変化、周回、中心位置、放射方向の設定をGPUパラメータとして登録します。
        /// </summary>
        /// <param name="module">VelocityOverLifetime設定を保持するModule。</param>
        /// <param name="collector">GPUパラメータの登録先。</param>
        public override void CollectParameters(FluxParticleVelocityOverLifetimeModule module, FluxParticleParameterCollector collector)
        {
            collector.AddVector("_VelocityOverLifetimeStart", new Vector4(module.Start.x, module.Start.y, module.Start.z, 0));
            collector.AddVector("_VelocityOverLifetimeEnd", new Vector4(module.End.x, module.End.y, module.End.z, 0));
            collector.AddVector("_VelocityOverLifetimeOrbital", new Vector4(module.Orbital.x, module.Orbital.y, module.Orbital.z, 0));
            collector.AddVector("_VelocityOverLifetimeOffset", new Vector4(module.Offset.x, module.Offset.y, module.Offset.z, 0));
            collector.AddFloat("_VelocityOverLifetimeRadial", module.Radial);
        }

        /// <summary>
        /// PositionのWorking状態とVelocityのUpdated状態を利用するUpdate Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるVelocityOverLifetime Module。</param>
        /// <returns>粒子位置の更新Operation。</returns>
        public override IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleVelocityOverLifetimeModule module)
        {
            yield return new FluxParticleOperation(
                ModuleId,
                "apply-position",
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Position |
                FluxParticleAttribute.Age |
                FluxParticleAttribute.Velocity |
                FluxParticleAttribute.Lifetime,
                FluxParticleAttribute.Position,
                Order,
                new[]
                {
                    new FluxParticleStateRead(
                        FluxParticleAttribute.Position,
                        FluxParticleStateReadSource.Working),

                    new FluxParticleStateRead(
                        FluxParticleAttribute.Age,
                        FluxParticleStateReadSource.Working),

                    new FluxParticleStateRead(
                        FluxParticleAttribute.Velocity,
                        FluxParticleStateReadSource.Updated),

                    new FluxParticleStateRead(
                        FluxParticleAttribute.Lifetime,
                        FluxParticleStateReadSource.Updated)
                });
        }
    }
}