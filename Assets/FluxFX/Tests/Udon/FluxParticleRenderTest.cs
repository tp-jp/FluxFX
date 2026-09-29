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
        FluxParticleSimulation particleSimulation;

        [SerializeField]
        FluxUpload upload;

        [SerializeField]
        FluxParticleRenderer particleRenderer;

        void Start()
        {
            particleState.Initialize(ParticleCount);

            var positions = new Vector4[ParticleCount];
            var velocities = new Vector4[ParticleCount];

            upload.Upload(positions, particleState.CurrentPosition);
            upload.Upload(velocities, particleState.CurrentVelocity);

            particleRenderer.Initialize(ParticleCount);
            particleRenderer.SetState(particleState);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;

            particleEmitter.UpdateEmission(deltaTime);
            particleSimulation.Simulate(deltaTime);
            particleRenderer.SetState(particleState);
        }
    }
}