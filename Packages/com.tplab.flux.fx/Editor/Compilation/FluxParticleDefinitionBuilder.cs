using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// GPUとRenderingで共用するModule定義の契約です。
    /// </summary>
    public interface IFluxParticleModuleDefinition
    {
        Type ModuleType { get; }
        string ModuleId { get; }
        void Build(FluxParticleModule module, FluxParticleDefinitionBuilder builder);
    }

    /// <summary>
    /// CoreとModuleが同じ形式の宣言を登録するための収集先です。
    /// </summary>
    public sealed class FluxParticleDefinitionBuilder
    {
        readonly Dictionary<string, FluxParticleValueDefinition> _values = new Dictionary<string, FluxParticleValueDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, FluxParticleValueProvider> _providers = new Dictionary<string, FluxParticleValueProvider>(StringComparer.Ordinal);
        readonly Dictionary<string, FluxParticleParameterDefinition> _parameters = new Dictionary<string, FluxParticleParameterDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, FluxParticleOperationDefinition> _operations = new Dictionary<string, FluxParticleOperationDefinition>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, FluxParticleValueDefinition> Values => _values;
        public IReadOnlyDictionary<string, FluxParticleValueProvider> Providers => _providers;
        public IReadOnlyDictionary<string, FluxParticleParameterDefinition> Parameters => _parameters;
        public IReadOnlyDictionary<string, FluxParticleOperationDefinition> Operations => _operations;

        /// <summary>
        /// 論理Valueを登録します。重複するIDは同一定義であっても禁止します。
        /// </summary>
        public void AddValue(FluxParticleValueDefinition value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_values.ContainsKey(value.Id)) throw new InvalidOperationException($"Duplicate Value ID: {value.Id}");
            _values.Add(value.Id, value);
        }

        /// <summary>
        /// Valueの供給方法を登録します。初期実装では各Valueに1つの非永続Providerのみ登録可能です。
        /// </summary>
        public void AddProvider(FluxParticleValueProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (_providers.ContainsKey(provider.ValueId)) throw new InvalidOperationException($"Duplicate provider: {provider.ValueId}");
            _providers.Add(provider.ValueId, provider);
        }

        /// <summary>
        /// Runtime変更契約を持つShader Parameterを登録します。
        /// </summary>
        public void AddParameter(FluxParticleParameterDefinition parameter)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));
            if (_parameters.ContainsKey(parameter.Id)) throw new InvalidOperationException($"Duplicate parameter: {parameter.Id}");
            _parameters.Add(parameter.Id, parameter);
        }

        /// <summary>
        /// 実装情報を含むOperationを登録します。
        /// </summary>
        public void AddOperation(FluxParticleOperationDefinition operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (_operations.ContainsKey(operation.Id)) throw new InvalidOperationException($"Duplicate Operation ID: {operation.Id}");
            _operations.Add(operation.Id, operation);
        }
    }
}
