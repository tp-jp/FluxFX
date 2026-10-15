using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Udon;
using UnityEngine;

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
        bool completed;

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
        /// Operationを登録し、AfterOperationの参照元を依存関係へ追加します。
        /// </summary>
        public void Operation(string id, FluxParticlePhase phase, IEnumerable<FluxParticleValueRead> reads,
            IEnumerable<FluxParticleValueWrite> writes, string hlslPath, string entryPoint, bool switchable,
            IEnumerable<string> parameters = null, IEnumerable<string> dependencies = null)
        {
            RequireOpen();
            var inputs = reads.ToArray();
            var predecessors = inputs.Where(read => read.Version == FluxParticleReadVersion.AfterOperation)
                .Select(read => read.ProducerOperationId).Concat(dependencies ?? Array.Empty<string>()).Distinct().ToArray();
            context.AddOperation(new FluxParticleOperationDefinition(id, moduleId, phase, inputs, writes,
                predecessors, hlslPath, entryPoint, parameters, switchable ? enabledParameterId : null));
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

        void RequireOpen()
        {
            if (completed) throw new InvalidOperationException($"Module contribution is already complete: {moduleId}");
        }
    }
}
