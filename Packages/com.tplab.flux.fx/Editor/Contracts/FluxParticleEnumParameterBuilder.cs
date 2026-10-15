using System;
using System.Collections.Generic;

namespace TpLab.Flux.FX.Editor.Contracts
{
    /// <summary>
    /// Enum Parameterの許可値を設定します。
    /// </summary>
    public sealed class FluxParticleEnumParameterBuilder<T> : FluxParticleParameterBuilder<T, FluxParticleEnumParameterBuilder<T>>
        where T : struct, Enum
    {
        T[] allowedValues;

        internal FluxParticleEnumParameterBuilder(string id) : base(id, FluxParticleParameterKind.Enum)
        {
        }

        /// <summary>
        /// 許可する宣言済みの値を指定します。省略時はすべての宣言済みの値を許可します。
        /// </summary>
        public FluxParticleEnumParameterBuilder<T> AllowedValues(params T[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            allowedValues = (T[])values.Clone();
            return this;
        }

        internal override FluxParticleParameterContract<T> CreateContract(string id, string apiName,
            FluxParticleParameterKind kind, FluxParticleParameterPolicy policy)
        {
            var values = new List<T>();
            var seen = new HashSet<T>();
            foreach (var value in allowedValues ?? (T[])Enum.GetValues(typeof(T)))
            {
                if (!Enum.IsDefined(typeof(T), value))
                {
                    throw new InvalidOperationException($"Parameter '{id}': undeclared Enum value '{value}'.");
                }
                if (!seen.Add(value))
                {
                    if (allowedValues == null) continue;
                    throw new InvalidOperationException($"Parameter '{id}': duplicate Enum value '{value}'.");
                }
                values.Add(value);
            }
            if (values.Count == 0) throw new InvalidOperationException($"Parameter '{id}': at least one Enum value is required.");
            return new FluxParticleParameterContract<T>(id, apiName, kind, policy, allowedValues: values.ToArray());
        }
    }
}
