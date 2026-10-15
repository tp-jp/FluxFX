namespace TpLab.Flux.FX.Editor.Contracts
{
    /// <summary>
    /// Bool Parameterの契約を組み立てます。
    /// </summary>
    public sealed class FluxParticleBooleanParameterBuilder : FluxParticleParameterBuilder<bool, FluxParticleBooleanParameterBuilder>
    {
        internal FluxParticleBooleanParameterBuilder(string id) : base(id, FluxParticleParameterKind.Bool)
        {
        }

        internal override FluxParticleParameterContract<bool> CreateContract(string id, string apiName,
            FluxParticleParameterKind kind, FluxParticleParameterPolicy policy)
        {
            return new FluxParticleParameterContract<bool>(id, apiName, kind, policy);
        }
    }
}
