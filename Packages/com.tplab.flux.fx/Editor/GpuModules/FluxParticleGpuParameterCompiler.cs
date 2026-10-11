using System;
using Newtonsoft.Json.Linq;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class FluxParticleGpuParameterCompiler
    {
        readonly FluxParticleGpuModuleRegistry _registry;

        public FluxParticleGpuParameterCompiler(FluxParticleGpuModuleRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public JObject Compile(FluxParticleCompilePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var stages = new JObject();
            var velocity = new JObject();
            var collector = new FluxParticleParameterCollector();

            foreach (var module in plan.GetModules(FluxParticleExecutionStage.VelocityUpdate))
            {
                if (!_registry.TryGet(module.GetType(), out var definition)) continue;

                definition.CollectParameters(module, collector);

                var enabledName = FluxParticleGpuModuleEnabled.GetParameterName(definition.ModuleId);
                collector.AddFloat(enabledName, module.Enabled ? 1.0f : 0.0f);
            }

            foreach (var parameter in collector.Parameters)
            {
                switch (parameter.Type)
                {
                    case FluxParticleParameterType.Float:
                        velocity.Add(parameter.Name, new JObject
                        {
                            ["type"] = (int)FluxParticleParameterType.Float,
                            ["value"] = parameter.FloatValue
                        });
                        break;

                    case FluxParticleParameterType.Vector:
                        velocity.Add(parameter.Name, new JObject
                        {
                            ["type"] = (int)FluxParticleParameterType.Vector,
                            ["value"] = new JArray
                            {
                                parameter.VectorValue.x,
                                parameter.VectorValue.y,
                                parameter.VectorValue.z,
                                parameter.VectorValue.w
                            }
                        });
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported GPU parameter type: {parameter.Type}");
                }
            }

            stages["velocity"] = velocity;

            return stages;
        }
    }
}