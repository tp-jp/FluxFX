using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Editor.Compilation.Contracts;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon;
using UnityEngine;
using FluxParticleParameterPolicy = TpLab.Flux.FX.Editor.Compilation.Definitions.FluxParticleParameterPolicy;

namespace TpLab.Flux.FX.Editor.Modules.Shared
{
    /// <summary>
    /// ModuleのParameter・Binding・評価処理をまとめて登録します。
    /// </summary>
    public sealed class FluxParticleModuleContribution
    {
        readonly FluxParticleProgramContribution context;
        readonly string moduleId;
        readonly string enabledParameterId;
        readonly FluxParticleParameterDefinition[] handleParameters;
        readonly bool[] assigned;
        readonly FluxParticleModuleContract contract;
        bool completed;

        /// <summary>
        /// Module契約からParameterの対応を準備し、Enabledの初期値を登録します。
        /// </summary>
        public FluxParticleModuleContribution(FluxParticleProgramContribution context, FluxParticleModuleContract contract, bool enabled)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.contract = contract ?? throw new ArgumentNullException(nameof(contract));
            moduleId = contract.Id;
            enabledParameterId = contract.Enabled?.Id;
            handleParameters = new FluxParticleParameterDefinition[contract.Parameters.Count];
            assigned = new bool[contract.Parameters.Count];
            if (contract.Enabled != null)
            {
                context.AddParameter(FluxParticleParameterContractCompiler.Compile(contract.Enabled),
                    FluxParticleParameterContractCompiler.Encode(contract.Enabled, enabled));
            }
            else if (!enabled)
            {
                throw new InvalidOperationException($"Module '{moduleId}': Enabled is required to start disabled.");
            }
        }

        public FluxParticleModuleContribution(FluxParticleProgramContribution context, string moduleId,
            string enabledParameterId, bool enabled, int handleParameterCount)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            if (handleParameterCount < 0) throw new ArgumentOutOfRangeException(nameof(handleParameterCount));
            this.moduleId = moduleId;
            this.enabledParameterId = enabledParameterId;
            handleParameters = new FluxParticleParameterDefinition[handleParameterCount];
            assigned = new bool[handleParameterCount];
            context.AddParameter(new FluxParticleParameterDefinition(enabledParameterId, FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(enabled ? 1 : 0, 0, 0, 0));
        }

        /// <summary>
        /// Parameterと初期値を登録し、Runtimeハンドルの番号へ結び付けます。
        /// </summary>
        public string Parameter(int slot, string id, FluxParticleValueType type, FluxParticleParameterPolicy policy, Vector4 value)
        {
            if (contract != null) throw new InvalidOperationException($"Module '{moduleId}': use Bind to register a contracted Parameter.");
            RequireSlot(slot);
            var parameter = new FluxParticleParameterDefinition(id, type, policy);
            context.AddParameter(parameter, value);
            handleParameters[slot] = parameter;
            assigned[slot] = true;
            return id;
        }

        /// <summary>
        /// 現在のモードでは使用しないハンドルの番号を予約します。
        /// </summary>
        public void UnavailableParameter(int slot)
        {
            if (contract != null) throw new InvalidOperationException($"Module '{moduleId}': specify the unavailable Parameter contract.");
            RequireSlot(slot);
            assigned[slot] = true;
        }

        /// <summary>
        /// 契約に型付きの初期値を割り当て、契約のハンドル番号へ登録します。
        /// </summary>
        public string Bind<T>(FluxParticleParameterContract<T> parameter, T value) where T : struct
        {
            var slot = ContractSlot(parameter);
            RequireSlot(slot);
            var definition = FluxParticleParameterContractCompiler.Compile(parameter);
            var initialValue = FluxParticleParameterContractCompiler.Encode(parameter, value);
            context.AddParameter(definition, initialValue);
            handleParameters[slot] = definition;
            assigned[slot] = true;
            return parameter.Id;
        }

        /// <summary>
        /// 現在の構成で使用しないParameter契約を指定します。
        /// </summary>
        public void UnavailableParameter(FluxParticleParameterContract parameter)
        {
            var slot = ContractSlot(parameter);
            RequireSlot(slot);
            assigned[slot] = true;
        }

        /// <summary>
        /// 正規化Ageを供給するValueとProviderを登録します。
        /// </summary>
        public string NormalizedAge(string id)
        {
            RequireOpen();
            context.AddValue(new FluxParticleValueDefinition(id, FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly, moduleId));
            context.AddProvider(new FluxParticleValueProvider(id, FluxParticleProviderKind.Derived,
                new[] { "particle.age", "particle.lifetime" },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/Shared/NormalizedAge.hlsl", entryPoint: "FluxFX_NormalizedAge"));
            return id;
        }

        /// <summary>
        /// 指定した入力で評価するCurveのValueとLookup Providerを登録します。
        /// </summary>
        public string Curve(string id, string input, AnimationCurve curve)
        {
            RequireOpen();
            var samples = FluxParticleCurveBaker.Bake(curve);
            context.AddValue(new FluxParticleValueDefinition(id, FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly, moduleId));
            context.AddProvider(new FluxParticleValueProvider(id, FluxParticleProviderKind.LookupTable,
                new[] { input }, lookupSamples: samples));
            return id;
        }

        /// <summary>
        /// 指定した入力で評価するGradientのValueとLookup Providerを登録します。
        /// </summary>
        public string Gradient(string id, string input, Gradient gradient)
        {
            RequireOpen();
            var samples = FluxParticleGradientBaker.Bake(gradient, out var positions);
            context.AddValue(new FluxParticleValueDefinition(id, FluxParticleValueType.Float4, FluxParticleWritePolicy.ReadOnly, moduleId));
            context.AddProvider(new FluxParticleValueProvider(id, FluxParticleProviderKind.LookupTable,
                new[] { input }, lookupVectorSamples: samples, lookupPositions: positions));
            return id;
        }

        /// <summary>
        /// Operationを登録し、AfterOperationの参照元を依存関係へ追加します。
        /// </summary>
        public void Operation(string id, FluxParticlePhase phase, IEnumerable<FluxParticleValueRead> reads,
            IEnumerable<FluxParticleValueWrite> writes, string hlslPath, string entryPoint, bool switchable,
            IEnumerable<string> parameters = null, IEnumerable<string> dependencies = null,
            IEnumerable<string> optionalDependencies = null)
        {
            RequireOpen();
            if (switchable && enabledParameterId == null) throw new InvalidOperationException($"Module '{moduleId}': a switchable Operation requires Enabled.");
            var inputs = reads.ToArray();
            var predecessors = inputs.Where(read => read.Version == FluxParticleReadVersion.AfterOperation)
                .Select(read => read.ProducerOperationId).Concat(dependencies ?? Array.Empty<string>()).Distinct().ToArray();
            context.AddOperation(new FluxParticleOperationDefinition(id, moduleId, phase, inputs, writes,
                predecessors, hlslPath, entryPoint, parameters, switchable ? enabledParameterId : null));
            foreach (var predecessor in optionalDependencies ?? Array.Empty<string>())
            {
                context.AddOptionalDependency(new FluxParticleOptionalOperationDependency(id, predecessor));
            }
        }

        /// <summary>
        /// すべてのハンドル番号の対応を確認し、Bindingを確定します。
        /// </summary>
        public void Complete()
        {
            RequireOpen();
            if (assigned.Any(value => !value)) throw new InvalidOperationException($"Unassigned Module handle slot: {moduleId}");
            context.AddModuleBinding(new FluxParticleModuleBinding(moduleId, enabledParameterId, handleParameters));
            completed = true;
        }

        void RequireSlot(int slot)
        {
            RequireOpen();
            if (slot < 0 || slot >= assigned.Length) throw new ArgumentOutOfRangeException(nameof(slot));
            if (assigned[slot]) throw new InvalidOperationException($"Duplicate Module handle slot: {moduleId}/{slot}");
        }

        int ContractSlot(FluxParticleParameterContract parameter)
        {
            RequireOpen();
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));
            if (contract == null) throw new InvalidOperationException($"Module '{moduleId}': a Module contract is required.");
            for (var i = 0; i < contract.Parameters.Count; i++)
            {
                if (ReferenceEquals(contract.Parameters[i], parameter)) return i;
            }
            throw new InvalidOperationException($"Module '{moduleId}': Parameter '{parameter.Id}' does not belong to this contract.");
        }

        void RequireOpen()
        {
            if (completed) throw new InvalidOperationException($"Module contribution is already complete: {moduleId}");
        }
    }
}
