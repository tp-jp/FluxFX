using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Editor.Modules;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.Inspector
{
    /// <summary>
    /// Inspectorで使用するParticle Moduleの表示情報を管理します。
    /// </summary>
    public sealed class FluxParticleInspectorModuleRegistry
    {
        const string SessionStatePrefix = "TpLab.FluxFX.ParticleSystemInspector.";

        readonly Dictionary<Type, FluxParticleInspectorModuleMetadata> _metadata = new Dictionary<Type, FluxParticleInspectorModuleMetadata>();
        readonly Dictionary<FluxParticleExecutionStage, IReadOnlyList<FluxParticleInspectorModuleMetadata>> _stages =
            new Dictionary<FluxParticleExecutionStage, IReadOnlyList<FluxParticleInspectorModuleMetadata>>();

        readonly IReadOnlyList<FluxParticleInspectorModuleMetadata> _all;

        public int Count => _metadata.Count;

        public FluxParticleInspectorModuleRegistry()
            : this(new FluxParticleModuleRegistry())
        {
        }

        public FluxParticleInspectorModuleRegistry(FluxParticleModuleRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            RegisterStandardModules();
            foreach (var type in _metadata.Keys.Where(type => !registry.TryGet(type, out _)).ToArray())
            {
                _metadata.Remove(type);
            }
            RegisterCustomModules(registry);

            var all = new List<FluxParticleInspectorModuleMetadata>(_metadata.Values);
            all.Sort(CompareMetadata);

            _all = all.AsReadOnly();

            var stages = new Dictionary<FluxParticleExecutionStage, List<FluxParticleInspectorModuleMetadata>>();

            foreach (var metadata in all)
            {
                if (!stages.TryGetValue(metadata.Stage, out var modules))
                {
                    modules = new List<FluxParticleInspectorModuleMetadata>();
                    stages.Add(metadata.Stage, modules);
                }

                modules.Add(metadata);
            }

            foreach (var entry in stages)
            {
                _stages.Add(entry.Key, entry.Value.AsReadOnly());
            }
        }

        public bool TryGet(Type moduleType, out FluxParticleInspectorModuleMetadata metadata)
        {
            if (moduleType == null)
            {
                metadata = null;
                return false;
            }

            return _metadata.TryGetValue(moduleType, out metadata);
        }

        public IReadOnlyList<FluxParticleInspectorModuleMetadata> GetAll()
        {
            return _all;
        }

        public IReadOnlyList<FluxParticleInspectorModuleMetadata> GetModules(FluxParticleExecutionStage stage)
        {
            if (_stages.TryGetValue(stage, out var modules)) return modules;

            return Array.Empty<FluxParticleInspectorModuleMetadata>();
        }

        void RegisterStandardModules()
        {
            RegisterStandard<FluxParticleGravityModule>("Gravity", 100, "GravityExpanded");
            RegisterStandard<FluxParticleForceModule>("Force", 200, "ForceExpanded");
            RegisterStandard<FluxParticleNoiseModule>("Noise", 300, "NoiseExpanded");
            RegisterStandard<FluxParticleVortexModule>("Vortex", 400, "VortexExpanded");
            RegisterStandard<FluxParticleDragModule>("Drag", 500, "DragExpanded");
            RegisterStandard<FluxParticleLimitVelocityModule>("Limit Velocity", 600, "LimitVelocityExpanded");

            RegisterStandard<FluxParticleVelocityOverLifetimeModule>("Velocity over Lifetime", 100, "VelocityOverLifetimeExpanded");

            RegisterStandard<FluxParticleColorOverLifetimeModule>("Color over Lifetime", 100, "ColorOverLifetimeExpanded");
            RegisterStandard<FluxParticleSizeOverLifetimeModule>("Size over Lifetime", 200, "SizeOverLifetimeExpanded");
            RegisterStandard<FluxParticleRotationOverLifetimeModule>("Rotation over Lifetime", 300, "RotationOverLifetimeExpanded");
            RegisterStandard<FluxParticleBlinkModule>("Blink", 500, "BlinkExpanded");
            RegisterStandard<FluxParticleTextureSheetAnimationModule>("Texture Sheet Animation", 400, "TextureSheetAnimationExpanded");
        }

        void RegisterStandard<T>(string displayName, int displayOrder, string expandedSuffix)
            where T : FluxParticleModule, new()
        {
            Register(new FluxParticleInspectorModuleMetadata(
                typeof(T),
                displayName,
                displayOrder,
                SessionStatePrefix + expandedSuffix));
        }

        void RegisterCustomModules(FluxParticleModuleRegistry registry)
        {
            foreach (var definition in registry.GetAll())
            {
                var type = definition.ModuleType;

                if (_metadata.ContainsKey(type)) continue;
                if (type.IsAbstract || type.ContainsGenericParameters || !type.IsSerializable) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                var metadata = new FluxParticleInspectorModuleMetadata(
                    type,
                    ObjectNames.NicifyVariableName(type.Name),
                    1000,
                    SessionStatePrefix + "CustomModule." + type.FullName,
                    true);


                Register(metadata);
            }
        }

        void Register(FluxParticleInspectorModuleMetadata metadata)
        {
            if (_metadata.ContainsKey(metadata.ModuleType))
            {
                throw new InvalidOperationException($"Duplicate Inspector Module metadata: {metadata.ModuleType.FullName}");
            }

            _metadata.Add(metadata.ModuleType, metadata);
        }

        static int CompareMetadata(
            FluxParticleInspectorModuleMetadata a,
            FluxParticleInspectorModuleMetadata b)
        {
            var stageOrder = ((int)a.Stage).CompareTo((int)b.Stage);
            if (stageOrder != 0) return stageOrder;

            var displayOrder = a.DisplayOrder.CompareTo(b.DisplayOrder);
            if (displayOrder != 0) return displayOrder;

            return string.CompareOrdinal(a.ModuleType.FullName, b.ModuleType.FullName);
        }
    }
}