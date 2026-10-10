using System;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public abstract class FluxParticleGpuModuleDefinition<TModule> : IFluxParticleGpuModuleDefinition
        where TModule : FluxParticleModule
    {
        public Type ModuleType => typeof(TModule);

        public abstract string ModuleId { get; }

        public abstract string HlslPath { get; }

        public abstract string EntryPoint { get; }

        public abstract int Order { get; }

        void IFluxParticleGpuModuleDefinition.CollectParameters(FluxParticleModule module, FluxParticleParameterCollector collector)
        {
            CollectParameters((TModule)module, collector);
        }

        public virtual void CollectParameters(TModule module, FluxParticleParameterCollector collector)
        {
        }
    }
}