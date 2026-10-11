using System;
using Newtonsoft.Json.Linq;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class FluxParticleGpuRuntimeDataWriter
    {
        public JObject WriteParameters(FluxParticleGpuCompilation compilation)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));

            var stages = new JObject();

            foreach (var stage in compilation.Stages)
            {
                var parameters = new JObject();

                foreach (var parameter in stage.Parameters)
                {
                    parameters.Add(parameter.Name, WriteParameter(parameter));
                }

                stages.Add(GetStageName(stage.Stage), parameters);
            }

            return stages;
        }

        public JObject WriteBindings(FluxParticleGpuCompilation compilation)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));

            var bindings = new JObject();

            foreach (var stage in compilation.Stages)
            {
                var target = GetKernelTarget(stage.Stage);

                foreach (var module in stage.Modules)
                {
                    var parameters = new JObject();

                    foreach (var parameter in module.Parameters)
                    {
                        parameters.Add(parameter.Name, (int)parameter.Type);
                    }

                    bindings.Add(module.ModuleId, new JObject
                    {
                        ["target"] = (int)target,
                        ["enabledParameter"] = module.EnabledParameter.Name,
                        ["parameters"] = parameters
                    });
                }
            }

            return bindings;
        }

        static JObject WriteParameter(FluxParticleGpuParameterCompilation parameter)
        {
            switch (parameter.Type)
            {
                case FluxParticleParameterType.Float:
                    return new JObject
                    {
                        ["type"] = (int)FluxParticleParameterType.Float,
                        ["value"] = parameter.FloatValue
                    };

                case FluxParticleParameterType.Vector:
                    var vector = parameter.VectorValue;

                    return new JObject
                    {
                        ["type"] = (int)FluxParticleParameterType.Vector,
                        ["value"] = new JArray
                        {
                            vector.x,
                            vector.y,
                            vector.z,
                            vector.w
                        }
                    };

                default:
                    throw new InvalidOperationException($"Unsupported GPU parameter type: {parameter.Type}");
            }
        }

        static string GetStageName(FluxParticleExecutionStage stage)
        {
            switch (stage)
            {
                case FluxParticleExecutionStage.VelocityUpdate:
                    return "velocity";

                default:
                    throw new InvalidOperationException($"GPU Runtime parameters are not supported for stage: {stage}");
            }
        }

        static FluxParticleKernelTarget GetKernelTarget(FluxParticleExecutionStage stage)
        {
            switch (stage)
            {
                case FluxParticleExecutionStage.VelocityUpdate:
                    return FluxParticleKernelTarget.Velocity;

                default:
                    throw new InvalidOperationException($"GPU Runtime binding is not supported for stage: {stage}");
            }
        }
    }
}