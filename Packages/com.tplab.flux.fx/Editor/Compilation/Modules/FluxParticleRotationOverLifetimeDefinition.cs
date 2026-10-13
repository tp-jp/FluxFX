using System;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// Linear Rotation over LifetimeのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleRotationOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleRotationOverLifetimeModule);
        public string ModuleId => "tplab.fluxfx.rotation-over-lifetime";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var rotation = (FluxParticleRotationOverLifetimeModule)module;
            if (rotation.Mode != FluxParticleRotationOverLifetimeMode.Linear)
            {
                throw new InvalidOperationException("Program Backend currently requires: Linear rotation over lifetime.");
            }

            var end = rotation.EndRotation;
            if (end.x != 0 || end.y != 0 || float.IsNaN(end.z) || float.IsInfinity(end.z))
            {
                throw new InvalidOperationException("Program Backend currently requires: Billboard rotation over lifetime around Z only.");
            }

            context.AddParameter(new FluxParticleParameterDefinition("rotation.end", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live), end);
            context.AddOperation(new FluxParticleOperationDefinition("render.rotation-over-lifetime", ModuleId, FluxParticlePhase.Render,
                new[]
                {
                    new FluxParticleValueRead("render.rotation", FluxParticleReadVersion.AfterOperation, "render.rotation-start"),
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("render.rotation", FluxParticleWriteKind.Modify) },
                new[] { "render.rotation-start" },
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/AuthoringRotationOverLifetimeLinear.hlsl",
                "FluxFX_RenderRotationOverLifetime", new[] { "rotation.end" }));
        }
    }
}