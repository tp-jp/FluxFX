using System;
using System.Collections.Generic;

namespace TpLab.Flux.FX.Editor.Contracts
{
    /// <summary>
    /// Moduleの公開APIとParameterの契約を組み立てます。
    /// </summary>
    public sealed class FluxParticleModuleContractBuilder
    {
        readonly string id;
        readonly List<FluxParticleParameterContract> parameters = new List<FluxParticleParameterContract>();
        string handleName;
        string accessorName;
        string enabledId;
        bool hasEnabled;

        /// <summary>
        /// 指定したIDのModule契約を組み立てます。
        /// </summary>
        public FluxParticleModuleContractBuilder(string id)
        {
            this.id = id;
        }

        /// <summary>
        /// ハンドルのクラス名を設定します。
        /// </summary>
        public FluxParticleModuleContractBuilder Handle(string name)
        {
            handleName = name;
            return this;
        }

        /// <summary>
        /// ハンドル取得APIの名前を設定します。
        /// </summary>
        public FluxParticleModuleContractBuilder Accessor(string name)
        {
            accessorName = name;
            return this;
        }

        /// <summary>
        /// Live変更可能なEnabled Parameterを宣言します。
        /// </summary>
        public FluxParticleModuleContractBuilder Enabled(string parameterId)
        {
            enabledId = parameterId;
            hasEnabled = true;
            return this;
        }

        /// <summary>
        /// 確定済みのParameter契約を追加します。
        /// </summary>
        public FluxParticleModuleContractBuilder Parameter(FluxParticleParameterContract parameter)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));
            parameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// IDと公開名の重複を検証し、Builderから独立した契約を作成します。
        /// </summary>
        public FluxParticleModuleContract Build()
        {
            FluxParticleContractNames.RequireId(id, "Module");
            FluxParticleContractNames.RequireIdentifier(handleName, $"Module '{id}' handle name");
            FluxParticleContractNames.RequireIdentifier(accessorName, $"Module '{id}' accessor name");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal) { "Enabled" };
            FluxParticleParameterContract<bool> enabled = null;
            if (hasEnabled)
            {
                FluxParticleContractNames.RequireId(enabledId, $"Module '{id}' Enabled Parameter");
                enabled = FluxParticleParameter.Bool(enabledId).ApiName("Enabled").Policy(FluxParticleParameterPolicy.Live).Build();
                ids.Add(enabled.Id);
            }
            foreach (var parameter in parameters)
            {
                if (!ids.Add(parameter.Id)) throw new InvalidOperationException($"Module '{id}': duplicate Parameter ID '{parameter.Id}'.");
                if (!names.Add(parameter.ApiName))
                {
                    throw new InvalidOperationException($"Module '{id}', Parameter '{parameter.Id}': duplicate or reserved API name '{parameter.ApiName}'.");
                }
            }
            return new FluxParticleModuleContract(id, handleName, accessorName, enabled, parameters);
        }
    }
}
