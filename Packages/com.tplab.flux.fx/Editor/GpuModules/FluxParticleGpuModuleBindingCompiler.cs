using System;
using System.Collections.Generic;
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

        public JObject Compile(FluxParticleCompilePlan plan, JObject gpuParameters)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (gpuParameters == null) throw new ArgumentNullException(nameof(gpuParameters));

            var velocity = gpuParameters["velocity"] as JObject;
            if (velocity == null) throw new InvalidOperationException("GPU Velocity parameters are missing.");

            var bindings = new JObject();
            var ownedNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var module in plan.GetModules(FluxParticleExecutionStage.VelocityUpdate))
            {
                if (!_registry.TryGet(module.GetType(), out var definition)) continue;

                var collector = new FluxParticleParameterCollector();
                definition.CollectParameters(module, collector);

                var moduleParameters = new JObject();

                foreach (var parameter in collector.Parameters)
                {
                    if (!ownedNames.Add(parameter.Name))
                    {
                        throw new InvalidOperationException(
                            $"GPU parameter is owned by multiple modules: {parameter.Name}");
                    }

                    ValidateParameter(definition.ModuleId, parameter.Name, parameter.Type, velocity);
                    moduleParameters.Add(parameter.Name, (int)parameter.Type);
                }

                var enabledName = FluxParticleGpuModuleEnabled.GetParameterName(definition.ModuleId);

                if (!ownedNames.Add(enabledName))
                {
                    throw new InvalidOperationException(
                        $"GPU Enabled parameter conflicts with another parameter: {enabledName}");
                }

                ValidateParameter(definition.ModuleId, enabledName, FluxParticleParameterType.Float, velocity);

                bindings.Add(definition.ModuleId, new JObject
                {
                    ["target"] = (int)FluxParticleKernelTarget.Velocity,
                    ["enabledParameter"] = enabledName,
                    ["parameters"] = moduleParameters
                });
            }

            if (ownedNames.Count != velocity.Count)
            {
                throw new InvalidOperationException(
                    "GPU Velocity parameters contain entries not owned by any registered module.");
            }

            return bindings;
        }

        static void ValidateParameter(string moduleId, string name, FluxParticleParameterType type, JObject parameters)
        {
            var entry = parameters[name] as JObject;

            if (entry == null)
            {
                throw new InvalidOperationException(
                    $"GPU parameter is missing: {moduleId} / {name}");
            }

            var actualType = entry["type"];
            if (actualType == null || (int)actualType != (int)type)
            {
                throw new InvalidOperationException(
                    $"GPU parameter type mismatch: {moduleId} / {name}. Expected: {type}.");
            }
        }
    }
}