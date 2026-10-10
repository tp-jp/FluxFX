using System;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public interface IFluxParticleGpuModuleDefinition
    {
        Type ModuleType { get; }

        string ModuleId { get; }

        string HlslPath { get; }

        string EntryPoint { get; }

        int Order { get; }

        void CollectParameters(FluxParticleModule module, FluxParticleParameterCollector collector);
    }
}