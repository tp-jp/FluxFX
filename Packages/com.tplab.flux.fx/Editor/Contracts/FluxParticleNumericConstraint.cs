using System;
using System.Collections.Generic;

namespace TpLab.Flux.FX.Editor.Contracts
{
    /// <summary>
    /// 数値Parameterの成分ごとの入力制約
    /// </summary>
    public sealed class FluxParticleNumericConstraint
    {
        /// <summary>
        /// 成分数
        /// </summary>
        public int ComponentCount { get; }

        /// <summary>
        /// 各成分の下限。指定がない場合は空
        /// </summary>
        public IReadOnlyList<float> Minimum { get; }

        /// <summary>
        /// 各成分の上限。指定がない場合は空
        /// </summary>
        public IReadOnlyList<float> Maximum { get; }

        /// <summary>
        /// 下限値を含むかどうか
        /// </summary>
        public bool IncludeMinimum { get; }

        /// <summary>
        /// 上限値を含むかどうか
        /// </summary>
        public bool IncludeMaximum { get; }

        /// <summary>
        /// 各成分を整数に限定するかどうか
        /// </summary>
        public bool IntegerOnly { get; }

        /// <summary>
        /// 全成分の積の上限。指定がない場合はnull
        /// </summary>
        public float? MaximumProduct { get; }

        /// <summary>
        /// 成分の昇順制約の有無
        /// </summary>
        public bool OrderedComponents { get; }

        internal FluxParticleNumericConstraint(string id, int components, float[] minimum, float[] maximum,
            bool includeMinimum, bool includeMaximum, bool integerOnly, float? maximumProduct = null, bool orderedComponents = false)
        {
            if (orderedComponents && components < 2)
            {
                throw new InvalidOperationException($"Parameter '{id}': ordered components require multiple components.");
            }
            OrderedComponents = orderedComponents;
            ComponentCount = components;
            Minimum = Array.AsReadOnly(minimum == null ? Array.Empty<float>() : (float[])minimum.Clone());
            Maximum = Array.AsReadOnly(maximum == null ? Array.Empty<float>() : (float[])maximum.Clone());
            IncludeMinimum = includeMinimum;
            IncludeMaximum = includeMaximum;
            IntegerOnly = integerOnly;
            if (maximumProduct.HasValue && (components < 2 || !Finite(maximumProduct.Value) || maximumProduct.Value <= 0))
            {
                throw new InvalidOperationException($"Parameter '{id}': product limit requires multiple components and a positive finite maximum.");
            }
            MaximumProduct = maximumProduct;
            var previousMinimum = -float.MaxValue;
            for (var i = 0; i < components; i++)
            {
                var min = Minimum.Count == 0 ? -float.MaxValue : Minimum[i];
                var max = Maximum.Count == 0 ? float.MaxValue : Maximum[i];
                if (!Finite(min) || !Finite(max))
                {
                    throw new InvalidOperationException($"Parameter '{id}': bounds must be finite (component {i}).");
                }
                if (Minimum.Count != 0 && !includeMinimum)
                {
                    min = Next(min, true);
                }
                if (Maximum.Count != 0 && !includeMaximum)
                {
                    max = Next(max, false);
                }
                if (orderedComponents)
                {
                    min = Math.Max(min, previousMinimum);
                }
                previousMinimum = integerOnly ? (float)Math.Ceiling(min) : min;
                if (!Finite(min) || !Finite(max) || min > max || integerOnly && Math.Ceiling(min) > Math.Floor(max))
                {
                    throw new InvalidOperationException($"Parameter '{id}': the range admits no value (component {i}).");
                }
            }
        }

        internal bool Accepts(float[] value)
        {
            if (value.Length != ComponentCount) return false;
            for (var i = 0; i < value.Length; i++)
            {
                var component = value[i];
                if (OrderedComponents && i > 0 && value[i - 1] > component) return false;
                if (!Finite(component) || IntegerOnly && Math.Truncate(component) != component) return false;
                if (Minimum.Count != 0 && (component < Minimum[i] || !IncludeMinimum && component == Minimum[i])) return false;
                if (Maximum.Count != 0 && (component > Maximum[i] || !IncludeMaximum && component == Maximum[i])) return false;
            }
            if (MaximumProduct.HasValue)
            {
                double product = 1;
                foreach (var component in value)
                {
                    product *= component;
                }
                if (product > MaximumProduct.Value) return false;
            }
            return true;
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        // 開区間にも表現可能なfloatが残ることを確認します。
        static float Next(float value, bool up)
        {
            if (value == 0) return up ? float.Epsilon : -float.Epsilon;
            var bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
            bits += (value > 0) == up ? 1 : -1;
            return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        }
    }
}
