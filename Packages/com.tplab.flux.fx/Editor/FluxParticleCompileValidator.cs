using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor
{
    public sealed class FluxParticleCompileValidator
    {
        const FluxParticleExecutionStage DefinedStages =
            FluxParticleExecutionStage.Spawn |
            FluxParticleExecutionStage.VelocityUpdate |
            FluxParticleExecutionStage.PositionUpdate |
            FluxParticleExecutionStage.SpawnAttributes |
            FluxParticleExecutionStage.Rendering;

        const FluxParticleAttribute DefinedAttributes =
            FluxParticleAttribute.Position |
            FluxParticleAttribute.Age |
            FluxParticleAttribute.Velocity |
            FluxParticleAttribute.Lifetime |
            FluxParticleAttribute.StartColor |
            FluxParticleAttribute.StartSize |
            FluxParticleAttribute.StartRotation;

        public FluxParticleValidationResult Validate(FluxParticleCompilePlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var result = new FluxParticleValidationResult();
            var registeredTypes = new HashSet<Type>();

            foreach (var module in plan.Modules)
            {
                var moduleType = module.GetType();
                var stage = module.Stage;

                if (!registeredTypes.Add(moduleType))
                {
                    result.Add(
                        "V004",
                        "A module of the same type is already registered.",
                        moduleType,
                        stage,
                        FluxParticleValidationSeverity.Error);
                }

                if (!IsSingleDefinedStage(stage))
                {
                    result.Add(
                        "V001",
                        $"Invalid execution stage: {stage}. A module must declare exactly one defined stage.",
                        moduleType,
                        stage,
                        FluxParticleValidationSeverity.Error);
                    continue;
                }

                if (!FluxParticleStageContract.TryGet(stage, out var contract) ||
                    !contract.HasExecutionPath)
                {
                    result.Add(
                        "V005",
                        $"No GPU execution path is registered for stage {stage}.",
                        moduleType,
                        stage,
                        FluxParticleValidationSeverity.Error);
                    continue;
                }

                var invalidReads =
                    module.ReadAttributes & ~contract.AvailableAttributes;

                var unknownReads =
                    module.ReadAttributes & ~DefinedAttributes;

                invalidReads |= unknownReads;

                if (invalidReads != FluxParticleAttribute.None)
                {
                    result.Add(
                        "V002",
                        $"Attributes cannot be read at stage {stage}: {invalidReads}.",
                        moduleType,
                        stage,
                        FluxParticleValidationSeverity.Error);
                }

                var invalidWrites =
                    module.WriteAttributes & ~contract.WritableAttributes;

                var unknownWrites =
                    module.WriteAttributes & ~DefinedAttributes;

                invalidWrites |= unknownWrites;

                if (invalidWrites != FluxParticleAttribute.None)
                {
                    result.Add(
                        "V003",
                        $"Attributes cannot be written at stage {stage}: {invalidWrites}.",
                        moduleType,
                        stage,
                        FluxParticleValidationSeverity.Error);
                }
            }

            return result;
        }

        static bool IsSingleDefinedStage(FluxParticleExecutionStage stage)
        {
            if (stage == FluxParticleExecutionStage.None) return false;

            if ((stage & ~DefinedStages) != 0) return false;

            var bits = (int)stage;

            return (bits & (bits - 1)) == 0;
        }
    }
}
