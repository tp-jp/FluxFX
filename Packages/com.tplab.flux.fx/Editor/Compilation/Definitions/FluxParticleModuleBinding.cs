using System;
using System.Collections.Generic;

namespace TpLab.Flux.FX.Editor.Compilation.Definitions
{
    /// <summary>
    /// Compilation時に確定したModuleのRuntime Binding
    /// </summary>
    public sealed class FluxParticleModuleBinding
    {
        /// <summary>
        /// Moduleの識別子
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// 有効状態を保持するParameterのID
        /// </summary>
        public string EnabledParameterId { get; }

        /// <summary>
        /// Runtimeハンドルへの対応の有無
        /// </summary>
        public bool HasRuntimeBinding { get; }

        /// <summary>
        /// ハンドル番号順のParameter。未対応の番号はnull
        /// </summary>
        public IReadOnlyList<FluxParticleParameterDefinition> Parameters { get; }

        public FluxParticleModuleBinding(string moduleId, string enabledParameterId,
            IEnumerable<FluxParticleParameterDefinition> parameters, bool hasRuntimeBinding = true)
        {
            if (string.IsNullOrWhiteSpace(moduleId)) throw new ArgumentException("Module ID is required.", nameof(moduleId));
            ModuleId = moduleId;
            HasRuntimeBinding = hasRuntimeBinding;
            EnabledParameterId = enabledParameterId;
            Parameters = new List<FluxParticleParameterDefinition>(parameters ?? Array.Empty<FluxParticleParameterDefinition>()).AsReadOnly();
        }
    }
}