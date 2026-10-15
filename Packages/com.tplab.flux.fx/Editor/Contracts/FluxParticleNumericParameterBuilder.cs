namespace TpLab.Flux.FX.Editor.Contracts
{
    /// <summary>
    /// 数値Parameterの成分ごとの範囲と整数制約を設定します。
    /// </summary>
    public sealed class FluxParticleNumericParameterBuilder<T> : FluxParticleParameterBuilder<T, FluxParticleNumericParameterBuilder<T>>
        where T : struct
    {
        float[] minimum;
        float[] maximum;
        bool includeMinimum = true;
        bool includeMaximum = true;
        bool integerOnly;

        internal FluxParticleNumericParameterBuilder(string id, FluxParticleParameterKind kind) : base(id, kind)
        {
        }

        /// <summary>
        /// 各成分の下限を設定します。
        /// </summary>
        public FluxParticleNumericParameterBuilder<T> Minimum(T value, bool inclusive = true)
        {
            minimum = FluxParticleParameterComponents.Get(value);
            includeMinimum = inclusive;
            return this;
        }

        /// <summary>
        /// 各成分の上限を設定します。
        /// </summary>
        public FluxParticleNumericParameterBuilder<T> Maximum(T value, bool inclusive = true)
        {
            maximum = FluxParticleParameterComponents.Get(value);
            includeMaximum = inclusive;
            return this;
        }

        /// <summary>
        /// 各成分の許容範囲を設定します。
        /// </summary>
        public FluxParticleNumericParameterBuilder<T> Range(T min, T max, bool includeMin = true, bool includeMax = true)
        {
            return Minimum(min, includeMin).Maximum(max, includeMax);
        }

        /// <summary>
        /// 各成分を整数に限定します。
        /// </summary>
        public FluxParticleNumericParameterBuilder<T> Integer()
        {
            integerOnly = true;
            return this;
        }

        internal override FluxParticleParameterContract<T> CreateContract(string id, string apiName,
            FluxParticleParameterKind kind, FluxParticleParameterPolicy policy)
        {
            var components = FluxParticleParameterComponents.Get(default(T)).Length;
            var constraint = new FluxParticleNumericConstraint(id, components, minimum, maximum,
                includeMinimum, includeMaximum, integerOnly);
            return new FluxParticleParameterContract<T>(id, apiName, kind, policy, constraint);
        }
    }
}
