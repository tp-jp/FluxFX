using System;

namespace TpLab.Flux.FX.Editor.Contracts
{
    /// <summary>
    /// 型付きParameterの公開APIと変更Policyを設定します。
    /// </summary>
    public abstract class FluxParticleParameterBuilder<T, TBuilder>
        where T : struct
        where TBuilder : FluxParticleParameterBuilder<T, TBuilder>
    {
        readonly string id;
        readonly FluxParticleParameterKind kind;
        string apiName;
        FluxParticleParameterPolicy? policy;

        internal FluxParticleParameterBuilder(string id, FluxParticleParameterKind kind)
        {
            this.id = id;
            this.kind = kind;
        }

        /// <summary>
        /// Get・Set・Hasに使用する公開APIの名前を設定します。
        /// </summary>
        public TBuilder ApiName(string name)
        {
            apiName = name;
            return (TBuilder)this;
        }

        /// <summary>
        /// 値の変更と反映のPolicyを設定します。
        /// </summary>
        public TBuilder Policy(FluxParticleParameterPolicy value)
        {
            policy = value;
            return (TBuilder)this;
        }

        /// <summary>
        /// 設定を検証し、Builderから独立した契約を作成します。
        /// </summary>
        public FluxParticleParameterContract<T> Build()
        {
            FluxParticleContractNames.RequireId(id, "Parameter");
            FluxParticleContractNames.RequireIdentifier(apiName, $"Parameter '{id}' API name");
            if (!policy.HasValue || !Enum.IsDefined(typeof(FluxParticleParameterPolicy), policy.Value))
            {
                throw new InvalidOperationException($"Parameter '{id}': a valid Policy is required.");
            }
            return CreateContract(id, apiName, kind, policy.Value);
        }

        internal abstract FluxParticleParameterContract<T> CreateContract(string id, string apiName,
            FluxParticleParameterKind kind, FluxParticleParameterPolicy policy);
    }
}
