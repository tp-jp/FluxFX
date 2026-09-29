using JetBrains.Annotations;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleSystem : UdonSharpBehaviour
    {
        [SerializeField]
        [Min(1)]
        int particleCount = 64;

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

        [SerializeField]
        string compiledParameters;

        [PublicAPI]
        public int ParticleCount => particleCount;

        void Start()
        {
            particleState.Initialize(particleCount);

            var positions = new Vector4[particleCount];
            var velocities = new Vector4[particleCount];

            upload.Upload(positions, particleState.CurrentPosition);
            upload.Upload(velocities, particleState.CurrentVelocity);

            InitializeCompiledParameters();

            particleRenderer.Initialize(particleCount);
            particleRenderer.SetState(particleState);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;

            particleEmitter.UpdateEmission(deltaTime);
            particleSimulation.Simulate(deltaTime);
            particleRenderer.SetState(particleState);
        }

        void InitializeCompiledParameters()
        {
            if (!VRCJson.TryDeserializeFromJson(compiledParameters, out var result)) return;

            particleSimulation.Initialize(result.DataDictionary);
        }
    }
}