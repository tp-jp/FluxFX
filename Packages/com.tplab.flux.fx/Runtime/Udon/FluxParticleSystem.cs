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
            particleSimulation.SetSystemTransform(transform.position, transform.rotation, transform.lossyScale);
            particleSimulation.Simulate(deltaTime);
            particleRenderer.SetState(particleState);
        }

        void InitializeCompiledParameters()
        {
            if (!VRCJson.TryDeserializeFromJson(compiledParameters, out var result)) return;

            var parameters = result.DataDictionary;

            InitializeEmitter(parameters);
            InitializeSimulation(parameters);
            InitializeRenderer(parameters);
        }

        void InitializeEmitter(DataDictionary parameters)
        {
            var emission = parameters["emission"].DataDictionary;
            var emissionRate = (float)emission["rate"].Double;

            particleEmitter.Initialize(emissionRate);
        }

        void InitializeSimulation(DataDictionary parameters)
        {
            var simulationSpace = (int)parameters["simulationSpace"].Double;

            particleSimulation.Initialize(parameters);
            particleSimulation.SetSimulationSpace(simulationSpace);

            var lifetime = (float)parameters["lifetime"].Double;

            var shape = parameters["shape"].DataDictionary;
            var shapeType = (int)shape["type"].Double;
            var shapeRadius = (float)shape["radius"].Double;

            var initialVelocity = parameters["initialVelocity"].DataDictionary;
            var initialSpeed = (float)initialVelocity["speed"].Double;

            particleSimulation.SetSpawnParameters(
                lifetime,
                shapeType,
                shapeRadius,
                initialSpeed);
        }

        void InitializeRenderer(DataDictionary parameters)
        {
            var simulationSpace = (int)parameters["simulationSpace"].Double;
            var render = parameters["render"].DataDictionary;

            var startColor = new Color(
                (float)render["startColorR"].Double,
                (float)render["startColorG"].Double,
                (float)render["startColorB"].Double);

            var startSize = (float)render["startSize"].Double;

            particleSimulation.SetVisualSpawnParameters(startColor, startSize);
            particleRenderer.SetSimulationSpace(simulationSpace);
            particleRenderer.SetRenderParameters(render);
        }
    }
}