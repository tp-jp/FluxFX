using System;
using System.Collections.Generic;

namespace TpLab.Flux.FX.Editor.Contracts
{
    /// <summary>
    /// Parameterの識別、公開型、入力制約、変更Policyの契約
    /// </summary>
    public abstract class FluxParticleParameterContract
    {
        /// <summary>
        /// Parameterの識別子
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 公開APIの名前
        /// </summary>
        public string ApiName { get; }

        /// <summary>
        /// Parameterの意味上の型
        /// </summary>
        public FluxParticleParameterKind Kind { get; }

        /// <summary>
        /// 公開APIで扱うC#の型
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// 値の変更と反映のPolicy
        /// </summary>
        public FluxParticleParameterPolicy Policy { get; }

        /// <summary>
        /// 数値の入力制約。数値型以外はnull
        /// </summary>
        public FluxParticleNumericConstraint NumericConstraint { get; }

        /// <summary>
        /// Enumの入力制約。Enum以外はnull
        /// </summary>
        public FluxParticleEnumConstraint EnumConstraint { get; }

        internal FluxParticleParameterContract(string id, string apiName, FluxParticleParameterKind kind,
            Type valueType, FluxParticleParameterPolicy policy, FluxParticleNumericConstraint numericConstraint,
            FluxParticleEnumConstraint enumConstraint)
        {
            Id = id;
            ApiName = apiName;
            Kind = kind;
            ValueType = valueType;
            Policy = policy;
            NumericConstraint = numericConstraint;
            EnumConstraint = enumConstraint;
        }
    }

    /// <summary>
    /// 型付きParameterの変更不可の契約
    /// </summary>
    public sealed class FluxParticleParameterContract<T> : FluxParticleParameterContract where T : struct
    {
        /// <summary>
        /// Enumの許可値。Enum以外は空
        /// </summary>
        public IReadOnlyList<T> AllowedValues { get; }

        internal FluxParticleParameterContract(string id, string apiName, FluxParticleParameterKind kind,
            FluxParticleParameterPolicy policy, FluxParticleNumericConstraint numericConstraint = null, T[] allowedValues = null)
            : base(id, apiName, kind, typeof(T), policy, numericConstraint, CreateEnumConstraint(kind, allowedValues))
        {
            AllowedValues = Array.AsReadOnly(allowedValues == null ? Array.Empty<T>() : (T[])allowedValues.Clone());
        }

        static FluxParticleEnumConstraint CreateEnumConstraint(FluxParticleParameterKind kind, T[] values)
        {
            if (kind != FluxParticleParameterKind.Enum) return null;
            var enums = new Enum[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                enums[i] = (Enum)(object)values[i];
            }
            return new FluxParticleEnumConstraint(typeof(T), enums);
        }

        /// <summary>
        /// 値が入力制約を満たすかどうかを返します。
        /// </summary>
        /// <param name="value">検証する値</param>
        /// <returns>入力制約を満たす場合はtrue</returns>
        public bool IsValid(T value)
        {
            if (NumericConstraint != null) return NumericConstraint.Accepts(FluxParticleParameterComponents.Get(value));
            if (Kind != FluxParticleParameterKind.Enum) return true;
            for (var i = 0; i < AllowedValues.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(AllowedValues[i], value)) return true;
            }
            return false;
        }
    }
}
