using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Modules.RotationOverLifetime
{
    /// <summary>
    /// Rotation over LifetimeのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleRotationOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Modules/RotationOverLifetime/";
        const int EndRotationSlot = 0;

        public Type ModuleType => typeof(FluxParticleRotationOverLifetimeModule);

        public string ModuleId => "tplab.fluxfx.rotation-over-lifetime";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var rotation = (FluxParticleRotationOverLifetimeModule)module;
            if (rotation.Mode != FluxParticleRotationOverLifetimeMode.Linear &&
                rotation.Mode != FluxParticleRotationOverLifetimeMode.Curve)
            {
                throw new InvalidOperationException("Unsupported rotation over lifetime mode.");
            }
            var end = rotation.EndRotation;
            if (context.RenderMode == FluxParticleRenderMode.Billboard && (end.x != 0 || end.y != 0))
            {
                throw new InvalidOperationException("Program Backend currently requires: Billboard rotation over lifetime around Z only.");
            }

            var contribution = new FluxParticleModuleContribution(context, ModuleId, "rotation.enabled", rotation.Enabled, 1);
            var endRotation = contribution.Parameter(EndRotationSlot, "rotation.end", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live, end);
            var start = new FluxParticleValueRead("render.rotation", FluxParticleReadVersion.AfterOperation, "render.rotation-start");
            var reads = new[]
            {
                start,
                new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided)
            };
            var hlsl = "AuthoringRotationOverLifetimeLinear.hlsl";
            if (rotation.Mode == FluxParticleRotationOverLifetimeMode.Curve)
            {
                var age = contribution.NormalizedAge("rotation.normalized-age");
                var curve = contribution.Curve("rotation.curve", age, rotation.Curve);
                reads = new[] { start, new FluxParticleValueRead(curve, FluxParticleReadVersion.Provided) };
                hlsl = "AuthoringRotationOverLifetimeCurve.hlsl";
            }
            contribution.Operation("render.rotation-over-lifetime", FluxParticlePhase.Render,
                reads: reads,
                writes: new[] { new FluxParticleValueWrite("render.rotation", FluxParticleWriteKind.Modify) },
                hlslPath: Hlsl + hlsl, entryPoint: "FluxFX_RenderRotationOverLifetime",
                switchable: true, parameters: new[] { endRotation });
            contribution.Complete();
        }
    }
}
