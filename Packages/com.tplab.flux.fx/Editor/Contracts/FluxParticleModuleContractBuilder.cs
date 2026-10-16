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
        readonly List<int> parameterSlots = new List<int>();
        Type parameterKeyType;
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
            if (parameterKeyType != null) throw new InvalidOperationException($"Module '{id}': keyed and sequential Parameters cannot be mixed.");
            parameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// Runtimeと共有するenumの番号へParameter契約を対応付けます。
        /// </summary>
        public FluxParticleModuleContractBuilder Parameter<TKey>(TKey key, FluxParticleParameterContract parameter)
            where TKey : struct, Enum
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));
            var type = typeof(TKey);
            if (parameterKeyType == null && parameters.Count != 0)
            {
                throw new InvalidOperationException($"Module '{id}': keyed and sequential Parameters cannot be mixed.");
            }
            if (parameterKeyType != null && parameterKeyType != type)
            {
                throw new InvalidOperationException($"Module '{id}': all Parameter keys must use the same enum.");
            }
            var values = Enum.GetValues(type);
            var slots = new HashSet<int>();
            foreach (var value in values)
            {
                var number = Convert.ToDecimal(value);
                if (number < 0 || number >= values.Length || !slots.Add((int)number))
                {
                    throw new InvalidOperationException($"Module '{id}': Parameter keys must be unique and contiguous from zero.");
                }
            }
            if (!Enum.IsDefined(type, key)) throw new InvalidOperationException($"Module '{id}': undefined Parameter key '{key}'.");
            var slot = Convert.ToInt32(key);
            if (parameterSlots.Contains(slot)) throw new InvalidOperationException($"Module '{id}': duplicate Parameter key '{key}'.");
            parameterKeyType = type;
            parameterSlots.Add(slot);
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
            if (parameterKeyType == null)
            {
                return new FluxParticleModuleContract(id, handleName, accessorName, enabled, parameters);
            }
            if (parameters.Count != Enum.GetValues(parameterKeyType).Length)
            {
                throw new InvalidOperationException($"Module '{id}': every Parameter key requires a contract.");
            }
            var ordered = new FluxParticleParameterContract[parameters.Count];
            for (var i = 0; i < parameters.Count; i++)
            {
                ordered[parameterSlots[i]] = parameters[i];
            }
            return new FluxParticleModuleContract(id, handleName, accessorName, enabled, ordered);
        }
    }
}
