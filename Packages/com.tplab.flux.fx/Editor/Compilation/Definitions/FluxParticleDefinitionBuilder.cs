using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// CoreとModuleが同じ形式の宣言を登録するための収集先です。
    /// </summary>
    public sealed class FluxParticleDefinitionBuilder
    {
        readonly Dictionary<string, FluxParticleValueDefinition> _values = new Dictionary<string, FluxParticleValueDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, FluxParticleValueProvider> _providers = new Dictionary<string, FluxParticleValueProvider>(StringComparer.Ordinal);
        readonly Dictionary<string, FluxParticleParameterDefinition> _parameters = new Dictionary<string, FluxParticleParameterDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, FluxParticleOperationDefinition> _operations = new Dictionary<string, FluxParticleOperationDefinition>(StringComparer.Ordinal);
        readonly List<string> _operationOrder = new List<string>();
        readonly List<FluxParticleOptionalOperationDependency> _optionalDependencies = new List<FluxParticleOptionalOperationDependency>();

        /// <summary>
        /// 登録したValue定義をIDから参照する辞書
        /// </summary>
        public IReadOnlyDictionary<string, FluxParticleValueDefinition> Values => _values;

        /// <summary>
        /// Valueの非永続ProviderをIDから参照する辞書
        /// </summary>
        public IReadOnlyDictionary<string, FluxParticleValueProvider> Providers => _providers;

        /// <summary>
        /// Shader Parameterと変更契約の定義一覧
        /// </summary>
        public IReadOnlyDictionary<string, FluxParticleParameterDefinition> Parameters => _parameters;

        /// <summary>
        /// Operation定義をIDから参照する辞書
        /// </summary>
        public IReadOnlyDictionary<string, FluxParticleOperationDefinition> Operations => _operations;

        /// <summary>
        /// Operationの登録順
        /// </summary>
        public IReadOnlyList<string> OperationOrder => _operationOrder;

        /// <summary>
        /// 対象Operationが存在する場合だけ適用する順序制約
        /// </summary>
        public IReadOnlyList<FluxParticleOptionalOperationDependency> OptionalDependencies => _optionalDependencies;

        /// <summary>
        /// 論理Valueを登録します。重複するIDは同一定義であっても禁止します。
        /// </summary>
        /// <param name="value">登録するValue定義</param>
        public void AddValue(FluxParticleValueDefinition value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_values.ContainsKey(value.Id)) throw new InvalidOperationException($"Duplicate Value ID: {value.Id}");
            _values.Add(value.Id, value);
        }

        /// <summary>
        /// Valueの供給方法を登録します。各Valueに登録できるProviderは1つです。
        /// </summary>
        /// <param name="provider">登録するProvider定義</param>
        public void AddProvider(FluxParticleValueProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (_providers.ContainsKey(provider.ValueId)) throw new InvalidOperationException($"Duplicate provider: {provider.ValueId}");
            _providers.Add(provider.ValueId, provider);
        }

        /// <summary>
        /// Runtime変更契約を持つShader Parameterを登録します。
        /// </summary>
        /// <param name="parameter">登録するParameter定義</param>
        public void AddParameter(FluxParticleParameterDefinition parameter)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));
            if (_parameters.ContainsKey(parameter.Id)) throw new InvalidOperationException($"Duplicate parameter: {parameter.Id}");
            _parameters.Add(parameter.Id, parameter);
        }

        /// <summary>
        /// 実装情報を含むOperationを登録します。
        /// </summary>
        /// <param name="operation">登録するOperation定義</param>
        public void AddOperation(FluxParticleOperationDefinition operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (_operations.ContainsKey(operation.Id)) throw new InvalidOperationException($"Duplicate Operation ID: {operation.Id}");
            _operations.Add(operation.Id, operation);
            _operationOrder.Add(operation.Id);
        }

        /// <summary>
        /// Operationの存在を条件とする順序制約を登録します。
        /// </summary>
        public void AddOptionalDependency(FluxParticleOptionalOperationDependency dependency)
        {
            if (dependency == null) throw new ArgumentNullException(nameof(dependency));
            _optionalDependencies.Add(dependency);
        }

        /// <summary>
        /// 依存解決済みのOperation定義に置き換えます。新しいIDは登録できません。
        /// </summary>
        /// <param name="operation">同じIDを持つ解決済みOperation</param>
        public void ReplaceOperation(FluxParticleOperationDefinition operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (!_operations.ContainsKey(operation.Id)) throw new InvalidOperationException($"Unknown Operation: {operation.Id}");
            _operations[operation.Id] = operation;
        }
    }
}
