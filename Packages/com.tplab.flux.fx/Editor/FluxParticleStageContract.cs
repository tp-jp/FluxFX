using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor
{
    public readonly struct FluxParticleStageContract
    {
        public FluxParticleExecutionStage Stage { get; }

        public FluxParticleAttribute AvailableAttributes { get; }

        public FluxParticleAttribute WritableAttributes { get; }

        public FluxParticleAttribute ProducedAttributes { get; }

        public bool HasExecutionPath { get; }

        FluxParticleStageContract(
            FluxParticleExecutionStage stage,
            FluxParticleAttribute availableAttributes,
            FluxParticleAttribute writableAttributes,
            FluxParticleAttribute producedAttributes,
            bool hasExecutionPath)
        {
            Stage = stage;
            AvailableAttributes = availableAttributes;
            WritableAttributes = writableAttributes;
            ProducedAttributes = producedAttributes;
            HasExecutionPath = hasExecutionPath;
        }

        public static bool TryGet(
            FluxParticleExecutionStage stage,
            out FluxParticleStageContract contract)
        {
            const FluxParticleAttribute motion =
                FluxParticleAttribute.Position |
                FluxParticleAttribute.Age |
                FluxParticleAttribute.Velocity |
                FluxParticleAttribute.Lifetime;

            const FluxParticleAttribute visual =
                FluxParticleAttribute.StartColor |
                FluxParticleAttribute.StartSize |
                FluxParticleAttribute.StartRotation;

            switch (stage)
            {
                case FluxParticleExecutionStage.Spawn:
                    contract = new FluxParticleStageContract(
                        stage,
                        motion,
                        motion,
                        motion,
                        true);
                    return true;

                case FluxParticleExecutionStage.VelocityUpdate:
                    contract = new FluxParticleStageContract(
                        stage,
                        motion,
                        FluxParticleAttribute.Velocity,
                        FluxParticleAttribute.Velocity |
                        FluxParticleAttribute.Lifetime,
                        true);
                    return true;

                case FluxParticleExecutionStage.PositionUpdate:
                    contract = new FluxParticleStageContract(
                        stage,
                        motion,
                        FluxParticleAttribute.Position,
                        FluxParticleAttribute.Position |
                        FluxParticleAttribute.Age,
                        true);
                    return true;

                case FluxParticleExecutionStage.SpawnAttributes:
                    contract = new FluxParticleStageContract(
                        stage,
                        motion | visual,
                        visual,
                        visual,
                        true);
                    return true;

                case FluxParticleExecutionStage.Rendering:
                    contract = new FluxParticleStageContract(
                        stage,
                        motion | visual,
                        FluxParticleAttribute.None,
                        FluxParticleAttribute.None,
                        true);
                    return true;

                default:
                    contract = default;
                    return false;
            }
        }
    }
}
