using System;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// GravityのParameterとVelocity更新Operationを提供します。
    /// </summary>
    public sealed class FluxParticleGravityDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleGravityModule);
        public string ModuleId => "fluxfx.gravity";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var gravity = (FluxParticleGravityModule)module;
            context.SetInitialValue("gravity", gravity.Gravity);
            context.SetInitialValue("gravity.enabled", new Vector4(1, 0, 0, 0));
            context.AddOperation(new FluxParticleOperationDefinition("gravity", ModuleId, FluxParticlePhase.Update,
                new[] { new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided) },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/Gravity.hlsl", "FluxFX_Gravity",
                new[] { "gravity" }, "gravity.enabled"));
        }
    }

    /// <summary>
    /// DragのParameterとVelocity更新Operationを提供します。
    /// </summary>
    public sealed class FluxParticleDragDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleDragModule);
        public string ModuleId => "fluxfx.drag";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var drag = (FluxParticleDragModule)module;
            context.SetInitialValue("drag", new Vector4(drag.Drag, 0, 0, 0));
            context.SetInitialValue("drag.enabled", new Vector4(1, 0, 0, 0));
            context.AddOperation(new FluxParticleOperationDefinition("drag", ModuleId, FluxParticlePhase.Update,
                new[] { new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided) },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/Drag.hlsl", "FluxFX_Drag",
                new[] { "drag" }, "drag.enabled"));
        }
    }

    /// <summary>
    /// Color over LifetimeのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleColorOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleColorOverLifetimeModule);
        public string ModuleId => "fluxfx.color-over-lifetime";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var color = (FluxParticleColorOverLifetimeModule)module;
            if (color.Mode != FluxParticleColorOverLifetimeMode.Linear)
            {
                throw new InvalidOperationException("Program Backend currently requires: Linear color over lifetime.");
            }
            context.SetInitialValue("color.end", color.EndColor);
            context.AddOperation(new FluxParticleOperationDefinition("render.color", ModuleId, FluxParticlePhase.Render,
                new[] { new FluxParticleValueRead("render.color", FluxParticleReadVersion.AfterOperation, "render.start"),
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided) },
                new[] { new FluxParticleValueWrite("render.color", FluxParticleWriteKind.Modify) },
                new[] { "render.start" },
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/RenderColor.hlsl", "FluxFX_RenderColor",
                new[] { "color.end" }));
        }
    }

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
                new[] { new FluxParticleValueRead("render.rotation", FluxParticleReadVersion.AfterOperation, "render.rotation-start"),
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided) },
                new[] { new FluxParticleValueWrite("render.rotation", FluxParticleWriteKind.Modify) },
                new[] { "render.rotation-start" },
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/AuthoringRotationOverLifetimeLinear.hlsl",
                "FluxFX_RenderRotationOverLifetime", new[] { "rotation.end" }));
        }
    }
}
