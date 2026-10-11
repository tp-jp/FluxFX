using System;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Inspector
{
    /// <summary>
    /// Particle ModuleのInspector表示情報を保持します。
    /// </summary>
    public sealed class FluxParticleInspectorModuleMetadata
    {
        /// <summary>
        /// Moduleの型を取得します。
        /// </summary>
        public Type ModuleType { get; }

        /// <summary>
        /// Inspectorの表示名を取得します。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Inspector内の表示順序を取得します。
        /// </summary>
        public int DisplayOrder { get; }

        /// <summary>
        /// Inspectorの展開状態を保存するキーを取得します。
        /// </summary>
        public string ExpandedKey { get; }

        /// <summary>
        /// Custom GPU Moduleかどうかを取得します。
        /// </summary>
        public bool IsCustom { get; }

        /// <summary>
        /// Moduleが実行されるExecution Stageを取得します。
        /// </summary>
        public FluxParticleExecutionStage Stage => ((FluxParticleModule)Activator.CreateInstance(ModuleType)).Stage;

        /// <summary>
        /// ModuleのInspector表示情報を作成します。
        /// </summary>
        /// <param name="moduleType">Moduleの型。</param>
        /// <param name="displayName">Inspector表示名。</param>
        /// <param name="displayOrder">Inspector表示順序。</param>
        /// <param name="expandedKey">展開状態の保存キー。</param>
        /// <param name="isCustom">Custom GPU Moduleかどうか。</param>
        public FluxParticleInspectorModuleMetadata(
            Type moduleType,
            string displayName,
            int displayOrder,
            string expandedKey,
            bool isCustom = false)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
            if (moduleType.IsAbstract || !typeof(FluxParticleModule).IsAssignableFrom(moduleType))
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
        }
    }
}