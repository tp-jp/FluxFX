
using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class FluxParticleGpuModuleRegistry
    {
        readonly Dictionary<Type, IFluxParticleGpuModuleDefinition> _definitions = new Dictionary<Type, IFluxParticleGpuModuleDefinition>();
        readonly HashSet<string> _moduleIds = new HashSet<string>(StringComparer.Ordinal);

        public int Count => _definitions.Count;

        public FluxParticleGpuModuleRegistry()
        {
            var types = TypeCache.GetTypesDerivedFrom<IFluxParticleGpuModuleDefinition>();

            foreach (var type in types)
            {
                if (type.IsAbstract || type.ContainsGenericParameters) continue;

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new InvalidOperationException($"GPU Module Definition requires a public parameterless constructor: {type.FullName}");
                }

                var definition = (IFluxParticleGpuModuleDefinition)Activator.CreateInstance(type);

                Register(definition);
            }
        }

        public bool TryGet(Type moduleType, out IFluxParticleGpuModuleDefinition definition)
        {
            return _definitions.TryGetValue(moduleType, out definition);
        }

        public IFluxParticleGpuModuleDefinition Get(Type moduleType)
        {
            if (TryGet(moduleType, out var definition)) return definition;

            throw new InvalidOperationException($"GPU Module Definition is not registered: {moduleType?.FullName ?? "null"}");
        }

        public IFluxParticleGpuModuleDefinition Get(FluxParticleModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            return Get(module.GetType());
        }

        public IReadOnlyList<IFluxParticleGpuModuleDefinition> GetAll()
        {
            var result = new List<IFluxParticleGpuModuleDefinition>(_definitions.Values);
            result.Sort((a, b) =>
            {
                var order = a.Order.CompareTo(b.Order);
                return order != 0 ? order : string.CompareOrdinal(a.ModuleId, b.ModuleId);
            });

            return result;
        }

        void Register(IFluxParticleGpuModuleDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var moduleType = definition.ModuleType;

            if (moduleType == null || moduleType.IsAbstract || !typeof(FluxParticleModule).IsAssignableFrom(moduleType))
                throw new InvalidOperationException($"Invalid GPU Module type: {definition.GetType().FullName}");

            if (string.IsNullOrWhiteSpace(definition.ModuleId))
                throw new InvalidOperationException($"GPU Module ID is empty: {moduleType.FullName}");

            if (string.IsNullOrWhiteSpace(definition.HlslPath))
                throw new InvalidOperationException($"GPU Module HLSL path is empty: {moduleType.FullName}");

            if (string.IsNullOrWhiteSpace(definition.EntryPoint))
                throw new InvalidOperationException($"GPU Module entry point is empty: {moduleType.FullName}");

            if (_definitions.ContainsKey(moduleType))
                throw new InvalidOperationException($"Duplicate GPU Module type: {moduleType.FullName}");

            if (!_moduleIds.Add(definition.ModuleId))
                throw new InvalidOperationException($"Duplicate GPU Module ID: {definition.ModuleId}");

            _definitions.Add(moduleType, definition);
        }
    }
}
