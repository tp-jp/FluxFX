using System;
using TpLab.Flux.FX.Editor.Localization;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Inspector
{
    /// <summary>
    /// Particle ModuleのInspector表示情報を保持します。
    /// </summary>
    public sealed class FluxParticleInspectorModuleMetadata
    {
        readonly IFluxParticleModuleLocalization _localization;

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
            bool isCustom = false,
            IFluxParticleModuleLocalization localization = null)
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
            _localization = localization;

            var module = (FluxParticleModule)Activator.CreateInstance(moduleType);
            Stage = module.Stage;
        }

        /// <summary>
        /// 現在のInspector言語でModule固有の翻訳を解決します。
        /// </summary>
        /// <param name="text">翻訳前の表示文言</param>
        /// <returns>Moduleの翻訳、標準の翻訳、または元の文言</returns>
        public string Translate(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var translated = _localization?.Translate(text, L10n.Language);
            return string.IsNullOrEmpty(translated) ? L10n.Tr(text) : translated;
        }
    }
}
