using System;
using Newtonsoft.Json.Linq;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class FluxParticleGpuModuleBindingCompiler
    {
        readonly FluxParticleGpuModuleRegistry _registry;

        public FluxParticleGpuModuleBindingCompiler(FluxParticleGpuModuleRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public JObject Compile(FluxParticleCompilePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var bindings = new JObject();

            foreach (var module in plan.GetModules(FluxParticleExecutionStage.VelocityUpdate))
            {
                if (!_registry.TryGet(module.GetType(), out var definition)) continue;

                bindings.Add(definition.ModuleId, new JObject
                {
                    ["target"] = (int)FluxParticleKernelTarget.Velocity,
                    ["enabledParameter"] = FluxParticleGpuModuleEnabled.GetParameterName(definition.ModuleId)
                });
            }

            return bindings;
        }
    }
}