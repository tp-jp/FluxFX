using System;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class FluxParticleGpuMaterialBinder
    {
        public void Apply(FluxParticleGpuStageCompilation stage, Material material)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            if (material == null) throw new ArgumentNullException(nameof(material));

            foreach (var parameter in stage.Parameters)
            {
                switch (parameter.Type)
                {
                    case FluxParticleParameterType.Float:
                        material.SetFloat(parameter.Name, parameter.FloatValue);
                        break;

                    case FluxParticleParameterType.Vector:
                        material.SetVector(parameter.Name, parameter.VectorValue);
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported GPU parameter type: {parameter.Type}");
                }
            }
        }
    }
}