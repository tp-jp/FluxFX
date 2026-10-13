using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// GPU Moduleのパラメータ名・型・初期値を保持します。
    /// </summary>
    public sealed class FluxParticleGpuParameterCompilation
    {
        public string Name { get; }
        public FluxParticleParameterType Type { get; }
        public float FloatValue { get; }
        public Vector4 VectorValue { get; }

        internal FluxParticleGpuParameterCompilation(FluxParticleParameterCollector.Parameter parameter)
        {
            Name = parameter.Name;
            Type = parameter.Type;
            FloatValue = parameter.FloatValue;
            VectorValue = parameter.VectorValue;
        }

        internal FluxParticleGpuParameterCompilation(string name, float value)
        {
            Name = name;
            Type = FluxParticleParameterType.Float;
            FloatValue = value;
        }
    }
}