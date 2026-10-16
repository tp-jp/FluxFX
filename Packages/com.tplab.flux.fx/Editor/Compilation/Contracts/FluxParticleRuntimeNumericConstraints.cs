using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation.Contracts
{
    /// <summary>
    /// 契約の数値制約をExecutorのParameterスロット順へ変換します。
    /// </summary>
    public sealed class FluxParticleRuntimeNumericConstraints
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

        public FluxParticleRuntimeNumericConstraints(IReadOnlyList<FluxParticleParameterDefinition> parameters,
            IEnumerable<FluxParticleParameterContract> validatedParameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (validatedParameters == null) throw new ArgumentNullException(nameof(validatedParameters));
            var selected = new HashSet<FluxParticleParameterContract>(validatedParameters);
            Flags = new int[parameters.Count];
            Minimum = new Vector4[parameters.Count];
            Maximum = new Vector4[parameters.Count];
            for (var slot = 0; slot < parameters.Count; slot++)
            {
                var contract = parameters[slot].Contract;
                if (contract == null || !selected.Contains(contract)) continue;
                var constraint = contract.NumericConstraint;
                if (constraint == null) throw new InvalidOperationException($"Parameter '{contract.Id}': a numeric contract is required.");
                if (constraint.ComponentCount != (int)parameters[slot].Type)
                {
                    throw new InvalidOperationException($"Parameter '{contract.Id}': numeric constraint and storage type disagree.");
                }
                var flags = FluxParticleNumericConstraintFlags.None;
                if (constraint.Minimum.Count != 0)
                {
                    flags |= FluxParticleNumericConstraintFlags.Minimum;
                    if (!constraint.IncludeMinimum) flags |= FluxParticleNumericConstraintFlags.ExclusiveMinimum;
                }
                if (constraint.Maximum.Count != 0)
                {
                    flags |= FluxParticleNumericConstraintFlags.Maximum;
                    if (!constraint.IncludeMaximum) flags |= FluxParticleNumericConstraintFlags.ExclusiveMaximum;
                }
                if (constraint.IntegerOnly) flags |= FluxParticleNumericConstraintFlags.Integer;
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
        }
    }
}
