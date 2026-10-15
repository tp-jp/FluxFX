using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// CoreとModuleが論理Programの定義とParameter初期値を登録するためのContextです。
    /// </summary>
    public sealed class FluxParticleProgramContribution
    {
        readonly FluxParticleDefinitionBuilder _definitions;
        readonly Dictionary<string, Vector4> _initialValues;
        readonly IReadOnlyDictionary<string, Vector4> _readOnlyInitialValues;
        readonly string _ownerId;

        /// <summary>
        /// ビルド対象の描画方式
        /// </summary>
        public FluxParticleRenderMode RenderMode { get; }

        /// <summary>
        /// 登録済みValue定義の読み取り専用一覧
        /// </summary>
        public IReadOnlyDictionary<string, FluxParticleValueDefinition> Values => _definitions.Values;

        /// <summary>
        /// 登録済みProvider定義の読み取り専用一覧
        /// </summary>
        public IReadOnlyDictionary<string, FluxParticleValueProvider> Providers => _definitions.Providers;

        /// <summary>
        /// 登録済みParameter定義の読み取り専用一覧
        /// </summary>
        public IReadOnlyDictionary<string, FluxParticleParameterDefinition> Parameters => _definitions.Parameters;

        /// <summary>
        /// 登録済みOperation定義の読み取り専用一覧
        /// </summary>
        public IReadOnlyDictionary<string, FluxParticleOperationDefinition> Operations => _definitions.Operations;

        /// <summary>
        /// Parameter IDに対応する初期値
        /// </summary>
        public IReadOnlyDictionary<string, Vector4> InitialValues => _readOnlyInitialValues;

        /// <summary>
        /// 指定した定義集合へContributionを収集します。
        /// </summary>
        /// <param name="definitions">CoreとModuleが共有する論理定義集合</param>
        /// <param name="renderMode">ビルド対象の描画方式</param>
        public FluxParticleProgramContribution(FluxParticleDefinitionBuilder definitions, FluxParticleRenderMode renderMode = FluxParticleRenderMode.Billboard)
            : this(definitions, new Dictionary<string, Vector4>(StringComparer.Ordinal), null, renderMode)
        {
        }

        FluxParticleProgramContribution(FluxParticleDefinitionBuilder definitions, Dictionary<string, Vector4> initialValues, string ownerId, FluxParticleRenderMode renderMode)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            _initialValues = initialValues;
            _readOnlyInitialValues = new ReadOnlyDictionary<string, Vector4>(_initialValues);
            _ownerId = ownerId;
            RenderMode = renderMode;
        }

        /// <summary>
        /// Moduleの所有権を持った登録Contextを作成します。
        /// </summary>
        internal FluxParticleProgramContribution ForModule(string moduleId)
        {
            if (string.IsNullOrWhiteSpace(moduleId))
            {
                throw new ArgumentException("Module ID is required.", nameof(moduleId));
            }
            return new FluxParticleProgramContribution(_definitions, _initialValues, moduleId, RenderMode);
        }

        /// <summary>
        /// 論理Valueを登録します。Moduleは自身が所有するValueのみ新規定義できます。
        /// </summary>
        public void AddValue(FluxParticleValueDefinition value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            RequireOwner(value.OwnerId);
            _definitions.AddValue(value);
        }

        /// <summary>
        /// 既存または新規ValueのProviderを登録します。
        /// </summary>
        public void AddProvider(FluxParticleValueProvider provider) => _definitions.AddProvider(provider);

        /// <summary>
        /// Parameter定義と初期値を同時に登録します。
        /// </summary>
        public void AddParameter(FluxParticleParameterDefinition parameter, Vector4 initialValue)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }
            for (var i = 0; i < 4; i++)
            {
                if (float.IsNaN(initialValue[i]) || float.IsInfinity(initialValue[i]))
                {
                    throw new InvalidOperationException($"Non-finite Parameter: {parameter.Id}");
                }
            }
            if (_initialValues.ContainsKey(parameter.Id))
            {
                throw new InvalidOperationException($"Duplicate Parameter initial value: {parameter.Id}");
            }
            _definitions.AddParameter(parameter);
            _initialValues.Add(parameter.Id, initialValue);
        }

        /// <summary>
        /// Core定義のParameterに初期値を設定します。Parameter定義は後から追加される場合があります。
        /// </summary>
        /// <param name="id">登録済みParameterのID</param>
        /// <param name="value">初期値</param>
        public void SetInitialValue(string id, Vector4 value)
        {
            if (_initialValues.ContainsKey(id)) throw new InvalidOperationException($"Duplicate Parameter initial value: {id}");
            for (var i = 0; i < 4; i++)
            {
                if (float.IsNaN(value[i]) || float.IsInfinity(value[i]))
                {
                    throw new InvalidOperationException($"Non-finite Parameter: {id}");
                }
            }
            _initialValues.Add(id, value);
        }

        /// <summary>
        /// Operationを登録します。Moduleは自身のOperationのみ新規定義できます。
        /// </summary>
        public void AddOperation(FluxParticleOperationDefinition operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }
            RequireOwner(operation.OwnerId);
            _definitions.AddOperation(operation);
        }

        /// <summary>
        /// 自身のOperationが、存在する場合だけ別のOperationの後に実行されることを宣言します。
        /// </summary>
        public void AddOptionalDependency(FluxParticleOptionalOperationDependency dependency)
        {
            if (dependency == null) throw new ArgumentNullException(nameof(dependency));
            if (_ownerId != null && (!_definitions.Operations.TryGetValue(dependency.OperationId, out var operation) ||
                operation.OwnerId != _ownerId))
            {
                throw new InvalidOperationException($"Module {_ownerId} cannot constrain Operation {dependency.OperationId}.");
            }
            _definitions.AddOptionalDependency(dependency);
        }

        void RequireOwner(string ownerId)
        {
            if (_ownerId != null && ownerId != _ownerId)
            {
                throw new InvalidOperationException($"Module {_ownerId} cannot register definition owned by {ownerId}.");
            }
        }
    }
}
