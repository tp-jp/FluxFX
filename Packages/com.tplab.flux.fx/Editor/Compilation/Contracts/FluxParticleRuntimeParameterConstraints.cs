using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation.Contracts
{
    /// <summary>
    /// 契約の入力制約をExecutorのParameterスロット順へ変換します。
    /// </summary>
    public sealed class FluxParticleRuntimeParameterConstraints
    {
        /// <summary>
        /// Parameterスロット順の制約フラグ
        /// </summary>
        public int[] Flags { get; }

        /// <summary>
        /// Parameterスロット順の下限
        /// </summary>
        public Vector4[] Minimum { get; }

        /// <summary>
        /// Parameterスロット順の上限
        /// </summary>
        public Vector4[] Maximum { get; }

        /// <summary>
        /// Parameterスロット順の成分積の上限
        /// </summary>
        public float[] MaximumProduct { get; }

        /// <summary>
        /// Parameterごとの許可値の開始位置と終端
        /// </summary>
        public int[] AllowedValueOffsets { get; }

        /// <summary>
        /// Enum・Boolの許可値
        /// </summary>
        public float[] AllowedValues { get; }

        public FluxParticleRuntimeParameterConstraints(IReadOnlyList<FluxParticleParameterDefinition> parameters,
            IEnumerable<FluxParticleParameterContract> validatedParameters = null)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            var selected = validatedParameters == null ? null : new HashSet<FluxParticleParameterContract>(validatedParameters);
            Flags = new int[parameters.Count];
            Minimum = new Vector4[parameters.Count];
            Maximum = new Vector4[parameters.Count];
            MaximumProduct = new float[parameters.Count];
            AllowedValueOffsets = new int[parameters.Count + 1];
            var allowedValues = new List<float>();
            for (var slot = 0; slot < parameters.Count; slot++)
            {
                var contract = parameters[slot].Contract;
                AllowedValueOffsets[slot] = allowedValues.Count;
                if (contract == null || selected != null && !selected.Contains(contract)) continue;
                if (contract.EnumConstraint != null || contract.Kind == FluxParticleParameterKind.Bool)
                {
                    FluxParticleParameterContractCompiler.Compile(contract);
                    Flags[slot] = (int)FluxParticleParameterConstraintFlags.AllowedValues;
                    if (contract.Kind == FluxParticleParameterKind.Bool)
                    {
                        allowedValues.Add(0);
                        allowedValues.Add(1);
                    }
                    else
                    {
                        foreach (var value in contract.EnumConstraint.AllowedValues)
                        {
                            allowedValues.Add(Convert.ToSingle(value));
                        }
                    }
                    continue;
                }
                var constraint = contract.NumericConstraint;
                if (constraint == null) throw new InvalidOperationException($"Parameter '{contract.Id}': a numeric contract is required.");
                if (constraint.ComponentCount != (int)parameters[slot].Type)
                {
                    throw new InvalidOperationException($"Parameter '{contract.Id}': numeric constraint and storage type disagree.");
                }
                var flags = FluxParticleParameterConstraintFlags.None;
                if (constraint.Minimum.Count != 0)
                {
                    flags |= FluxParticleParameterConstraintFlags.Minimum;
                    if (!constraint.IncludeMinimum) flags |= FluxParticleParameterConstraintFlags.ExclusiveMinimum;
                }
                if (constraint.Maximum.Count != 0)
                {
                    flags |= FluxParticleParameterConstraintFlags.Maximum;
                    if (!constraint.IncludeMaximum) flags |= FluxParticleParameterConstraintFlags.ExclusiveMaximum;
                }
                if (constraint.IntegerOnly) flags |= FluxParticleParameterConstraintFlags.Integer;
                if (constraint.MaximumProduct.HasValue)
                {
                    flags |= FluxParticleParameterConstraintFlags.MaximumProduct;
                    MaximumProduct[slot] = constraint.MaximumProduct.Value;
                }
                var minimum = Vector4.zero;
                var maximum = Vector4.zero;
                for (var component = 0; component < constraint.ComponentCount; component++)
                {
                    if (constraint.Minimum.Count != 0) minimum[component] = constraint.Minimum[component];
                    if (constraint.Maximum.Count != 0) maximum[component] = constraint.Maximum[component];
                }
                Flags[slot] = (int)flags;
                Minimum[slot] = minimum;
                Maximum[slot] = maximum;
            }
            AllowedValueOffsets[parameters.Count] = allowedValues.Count;
            AllowedValues = allowedValues.ToArray();
        }
    }
}
