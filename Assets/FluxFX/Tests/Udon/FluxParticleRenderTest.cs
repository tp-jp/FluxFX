using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Udon
{
    public class FluxParticleRenderTest : UdonSharpBehaviour
    {
        const int ParticleCount = 64;

        [SerializeField]
        FluxParticleState particleState;

        [SerializeField]
        FluxParticleEmitter particleEmitter;

        [SerializeField]
        FluxUpload positionUpload;

        [SerializeField]
        FluxUpload velocityUpload;

        [SerializeField]
        FluxUpload visualUpload;

        [SerializeField]
        FluxKernel velocityUpdateKernel;

        [SerializeField]
        FluxKernel positionUpdateKernel;

        [SerializeField]
        FluxParticleRenderer particleRenderer;

        void Start()
        {
            particleState.Initialize(ParticleCount);

            var positions = new Vector4[ParticleCount];
            var velocities = new Vector4[ParticleCount];
            var visuals = new Vector4[ParticleCount];

            for (var i = 0; i < ParticleCount; i++)
            {
                positions[i] = Vector4.zero;
                velocities[i] = Vector4.zero;

                var t = (float)i / (ParticleCount - 1);
                var size = 0.5f + t * 1.5f;

                visuals[i] = new Vector4(
                    1.0f - t,
                    t,
                    0.25f,
                    size
                );
            }

            positionUpload.Upload(positions, particleState.CurrentPosition);
            velocityUpload.Upload(velocities, particleState.CurrentVelocity);
            visualUpload.Upload(visuals, particleState.Visual);

            particleRenderer.Initialize(ParticleCount);
            particleRenderer.SetState(particleState);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;

            particleEmitter.UpdateEmission(deltaTime);

            velocityUpdateKernel.SetFloat("_DeltaTime", deltaTime);
            velocityUpdateKernel.SetFloat("_SpawnStart", particleEmitter.SpawnStart);
            velocityUpdateKernel.SetFloat("_SpawnCount", particleEmitter.SpawnCount);
            velocityUpdateKernel.SetVector("_Gravity", new Vector4(0, -1.0f, 0, 0));
            velocityUpdateKernel.SetBuffer("_PositionTex", particleState.CurrentPosition);
            velocityUpdateKernel.Dispatch(particleState.CurrentVelocity, particleState.NextVelocity);

            positionUpdateKernel.SetFloat("_DeltaTime", deltaTime);
            positionUpdateKernel.SetBuffer("_VelocityTex", particleState.NextVelocity);
            positionUpdateKernel.SetBuffer("_CurrentVelocityTex", particleState.CurrentVelocity);
            positionUpdateKernel.Dispatch(particleState.CurrentPosition, particleState.NextPosition);

            particleState.Swap();

            particleRenderer.SetState(particleState);
        }
    }
}