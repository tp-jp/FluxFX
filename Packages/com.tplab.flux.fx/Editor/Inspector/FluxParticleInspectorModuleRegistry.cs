using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.GpuModules;
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
        readonly Dictionary<FluxParticleExecutionStage, IReadOnlyList<FluxParticleInspectorModuleMetadata>> _stages = new Dictionary<FluxParticleExecutionStage, IReadOnlyList<FluxParticleInspectorModuleMetadata>>();
        readonly IReadOnlyList<FluxParticleInspectorModuleMetadata> _all;

        /// <summary>
        /// 登録されているModuleの種類数を取得します。
        /// </summary>
        public int Count => _metadata.Count;

        /// <summary>
        /// 標準ModuleとCustom GPU Moduleを検出してRegistryを構築します。
        /// </summary>
        public FluxParticleInspectorModuleRegistry()
        {
            RegisterStandardModules();
            RegisterCustomModules(new FluxParticleGpuModuleRegistry());

            var all = new List<FluxParticleInspectorModuleMetadata>(_metadata.Values);
            all.Sort(CompareMetadata);

            _all = all.AsReadOnly();

            foreach (var entry in all)
            {
                var stage = entry.Stage;

                if (!_stages.TryGetValue(stage, out var existing))
                {
                    _stages.Add(stage, new List<FluxParticleInspectorModuleMetadata>());
                }

                ((List<FluxParticleInspectorModuleMetadata>)_stages[stage]).Add(entry);
            }
        }

        /// <summary>
        /// 指定した型のModule表示情報の取得を試みます。
        /// </summary>
        /// <param name="moduleType">Moduleの型。</param>
        /// <param name="metadata">取得した表示情報。存在しない場合はnull。</param>
        /// <returns>取得できた場合はtrue、それ以外はfalse。</returns>
        public bool TryGet(Type moduleType, out FluxParticleInspectorModuleMetadata metadata)
        {
            if (moduleType == null)
            {
                metadata = null;
                return false;
            }

            return _metadata.TryGetValue(moduleType, out metadata);
        }

        /// <summary>
        /// 登録されているすべてのModule表示情報を取得します。
        /// </summary>
        /// <returns>Module表示情報の一覧。</returns>
        public IReadOnlyList<FluxParticleInspectorModuleMetadata> GetAll()
        {
            return _all;
        }

        /// <summary>
        /// 指定したExecution StageのModule表示情報を取得します。
        /// </summary>
        /// <param name="stage">取得対象のExecution Stage。</param>
        /// <returns>表示順序で並べられたModule表示情報の一覧。</returns>
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

        void RegisterCustomModules(FluxParticleGpuModuleRegistry registry)
        {
            foreach (var definition in registry.GetAll())
            {
                var type = definition.ModuleType;

                if (_metadata.ContainsKey(type)) continue;
                if (type.IsAbstract || type.ContainsGenericParameters) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                var module = (FluxParticleModule)Activator.CreateInstance(type);

                // 現在Custom GPU Moduleを実行できるのはVelocityUpdateのみ。
                if (module.Stage != FluxParticleExecutionStage.VelocityUpdate) continue;

                Register(new FluxParticleInspectorModuleMetadata(
                    type,
                    ObjectNames.NicifyVariableName(type.Name),
                    1000 + definition.Order,
                    SessionStatePrefix + "CustomModule." + type.FullName,
                    true));
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