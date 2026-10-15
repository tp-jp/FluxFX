using System;
using System.Collections.Generic;

namespace TpLab.Flux.FX.Editor.Contracts
{
    /// <summary>
    /// Enumの型と許可値の変更不可の制約
    /// </summary>
    public sealed class FluxParticleEnumConstraint
    {
        /// <summary>
        /// 公開APIで扱うEnumの型
        /// </summary>
        public Type EnumType { get; }

        /// <summary>
        /// 宣言済みの許可値。数値が同じ別名は重複を除く
        /// </summary>
        public IReadOnlyList<Enum> AllowedValues { get; }

        internal FluxParticleEnumConstraint(Type enumType, IEnumerable<Enum> allowedValues)
        {
            EnumType = enumType;
            AllowedValues = new List<Enum>(allowedValues).AsReadOnly();
        }
    }
}
