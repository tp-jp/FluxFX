using System;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Inspector
{
    /// <summary>
    /// Particle ModuleのInspector表示情報を保持します。
    /// </summary>
    public sealed class FluxParticleInspectorModuleMetadata
    {
        public Type ModuleType { get; }

        public string DisplayName { get; }

        public int DisplayOrder { get; }

        public string ExpandedKey { get; }

        public bool IsCustom { get; }

        public FluxParticleExecutionStage Stage { get; }

        public FluxParticleInspectorModuleMetadata(
            Type moduleType,
            string displayName,
            int displayOrder,
            string expandedKey,
            bool isCustom = false)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));

            if (moduleType.IsAbstract || moduleType.ContainsGenericParameters || !typeof(FluxParticleModule).IsAssignableFrom(moduleType))
            {
                throw new ArgumentException($"Invalid Particle Module type: {moduleType.FullName}", nameof(moduleType));
            }

            if (moduleType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException($"Particle Module requires a public parameterless constructor: {moduleType.FullName}", nameof(moduleType));
            }

            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(expandedKey)) throw new ArgumentException("Expanded key is required.", nameof(expandedKey));

            ModuleType = moduleType;
            DisplayName = displayName;
            DisplayOrder = displayOrder;
            ExpandedKey = expandedKey;
            IsCustom = isCustom;

            var module = (FluxParticleModule)Activator.CreateInstance(moduleType);
            Stage = module.Stage;
        }
    }
}