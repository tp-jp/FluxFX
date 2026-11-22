using System.Linq;
using TpLab.Flux.FX.Editor.Core;
using TpLab.Flux.FX.Udon;
using UnityEngine;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Modules.DistanceFade
{
    /// <summary>
    /// 描画結果へ距離フェードを適用します。独自の色処理はこのModuleより先に登録してください。
    /// </summary>
    public sealed class FluxParticleDistanceFadeDefinition : FluxParticleModuleDefinition<FluxParticleDistanceFadeModule>
    {
        /// <summary>
        /// 距離フェードの描画Operation ID
        /// </summary>
        public const string RenderOperation = "render.distance-fade";

        /// <inheritdoc />
        public override string ModuleId => FluxParticleDistanceFadeContract.Module.Id;

        /// <inheritdoc />
        public override void Contribute(FluxParticleDistanceFadeModule module, FluxParticleProgramContribution context)
        {
            var contribution = new FluxParticleModuleContribution(context, FluxParticleDistanceFadeContract.Module, module.Enabled);
            if (module.DistanceMode != FluxParticleDistanceMode.FollowEffect && module.DistanceMode != FluxParticleDistanceMode.FixedWorld)
            {
                throw new System.InvalidOperationException("Unsupported distance mode.");
            }
            context.AddParameter(new FluxParticleParameterDefinition("distance-fade.distance-mode", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Static), new Vector4((int)module.DistanceMode, 0, 0, 0));
            var parameters = new[]
            {
                contribution.Bind(FluxParticleDistanceFadeContract.StartDistance, module.StartDistance),
                contribution.Bind(FluxParticleDistanceFadeContract.EndDistance, module.EndDistance),
                contribution.Bind(FluxParticleDistanceFadeContract.Target, module.Target),
                "distance-fade.distance-mode"
            };
            var predecessors = context.Operations.Values
                .Where(operation => operation.Phase == FluxParticlePhase.Render &&
                    operation.Writes.Any(write => write.ValueId == "render.color"))
                .Select(operation => operation.Id).Concat(new[] { "render.color" }).Distinct().ToArray();
            contribution.Operation(RenderOperation, FluxParticlePhase.Render,
                new[] { new FluxParticleValueRead("render.color", FluxParticleReadVersion.AfterOperation, "render.start"),
                    new FluxParticleValueRead(FluxParticleCoordinateContract.EffectScale, FluxParticleReadVersion.Provided) },
                new[] { new FluxParticleValueWrite("render.color", FluxParticleWriteKind.Modify) },
                "Packages/com.tplab.flux.fx/Editor/Modules/DistanceFade/DistanceFade.hlsl",
                "FluxFX_DistanceFadeRender", true, parameters, optionalDependencies: predecessors);
            contribution.Complete();
        }
    }
}
