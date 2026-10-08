using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor
{
    public sealed class FluxParticleCompilePlan
    {
        readonly List<FluxParticleModule> _modules = new List<FluxParticleModule>();
        readonly IReadOnlyList<FluxParticleModule> _readOnlyModules;

        public IReadOnlyList<FluxParticleModule> Modules => _readOnlyModules;

        public FluxParticleExecutionStage ActiveStages { get; private set; }

        public FluxParticleAttribute RequiredAttributes { get; private set; }

        public FluxParticleAttribute WrittenAttributes { get; private set; }

        public int ModuleCount => _modules.Count;

        FluxParticleCompilePlan(IReadOnlyList<FluxParticleModule> modules)
        {
            _readOnlyModules = _modules.AsReadOnly();

            foreach (var module in modules)
            {
                if (module == null || !module.Enabled) continue;

                _modules.Add(module);

                ActiveStages |= module.Stage;
                RequiredAttributes |= module.ReadAttributes;
                WrittenAttributes |= module.WriteAttributes;
            }
        }

        public static FluxParticleCompilePlan Create(FluxParticleAuthoring authoring)
        {
            if (authoring == null)
            {
                throw new ArgumentNullException(nameof(authoring));
            }

            return new FluxParticleCompilePlan(authoring.Modules);
        }

        public bool HasStage(FluxParticleExecutionStage stage)
        {
            if (stage == FluxParticleExecutionStage.None) return false;

            return (ActiveStages & stage) == stage;
        }

        public IReadOnlyList<FluxParticleModule> GetModules(FluxParticleExecutionStage stage)
        {
            var result = new List<FluxParticleModule>();

            if (stage == FluxParticleExecutionStage.None)
            {
                return result;
            }

            foreach (var module in _modules)
            {
                if ((module.Stage & stage) != stage) continue;

                result.Add(module);
            }

            return result;
        }

        public T GetModule<T>() where T : FluxParticleModule
        {
            foreach (var module in _modules)
            {
                if (module is T typedModule)
                {
                    return typedModule;
                }
            }

            return null;
        }
    }
}
