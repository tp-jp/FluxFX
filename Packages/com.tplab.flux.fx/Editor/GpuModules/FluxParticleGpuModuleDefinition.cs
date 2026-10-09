using System;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public abstract class FluxParticleGpuModuleDefinition
    {
        public abstract Type ModuleType { get; }

        public abstract string ModuleId { get; }

        public abstract string HlslPath { get; }

        public abstract string EntryPoint { get; }

        public abstract int Order { get; }

        public virtual void CollectParameters(FluxParticleModule module, FluxParticleParameterCollector collector)
        {
        }
    }
}