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
            var bursts = emission["bursts"].DataList;
            var burstTimes = new float[bursts.Count];
            var burstCounts = new int[bursts.Count];

            for (var i = 0; i < bursts.Count; i++)
            {
                var burst = bursts[i].DataDictionary;

                burstTimes[i] = (float)burst["time"].Double;
                burstCounts[i] = (int)burst["count"].Double;
            }

            particleEmitter.Initialize(emissionRate, burstTimes, burstCounts);
        }

        void InitializeSimulation(DataDictionary parameters)
        {
            var simulationSpace = (int)parameters["simulationSpace"].Double;

            particleSimulation.Initialize(parameters);
            particleSimulation.SetSimulationSpace(simulationSpace);

            var lifetime = (float)parameters["lifetime"].Double;
            var lifetimeMin = lifetime;
            var lifetimeMax = lifetime;

            if (parameters.TryGetValue("lifetimeMin", out var lifetimeMinToken))
            {
                lifetimeMin = (float)lifetimeMinToken.Double;
            }

            if (parameters.TryGetValue("lifetimeMax", out var lifetimeMaxToken))
            {
                lifetimeMax = (float)lifetimeMaxToken.Double;
            }

            var shape = parameters["shape"].DataDictionary;
            var shapeType = (int)shape["type"].Double;
            var shapeRadius = (float)shape["radius"].Double;

            var initialVelocity = parameters["initialVelocity"].DataDictionary;
            var initialSpeed = (float)initialVelocity["speed"].Double;
            var initialSpeedMin = initialSpeed;
            var initialSpeedMax = initialSpeed;

            if (initialVelocity.TryGetValue("speedMin", out var initialSpeedMinToken))
            {
                initialSpeedMin = (float)initialSpeedMinToken.Double;
            }

            if (initialVelocity.TryGetValue("speedMax", out var initialSpeedMaxToken))
            {
                initialSpeedMax = (float)initialSpeedMaxToken.Double;
            }

            particleSimulation.SetSpawnParameters(
                lifetimeMin,
                lifetimeMax,
                shapeType,
                shapeRadius,
                initialSpeedMin,
                initialSpeedMax);
        }

        void InitializeRenderer(DataDictionary parameters)
        {
            var simulationSpace = (int)parameters["simulationSpace"].Double;
            var render = parameters["render"].DataDictionary;

            var startColor = new Color(
                (float)render["startColorR"].Double,
                (float)render["startColorG"].Double,
                (float)render["startColorB"].Double);
            var startColorMin = startColor;
            var startColorMax = startColor;

            if (render.TryGetValue("startColorMinR", out var startColorMinRToken))
            {
                startColorMin = new Color(
                    (float)startColorMinRToken.Double,
                    (float)render["startColorMinG"].Double,
                    (float)render["startColorMinB"].Double);
            }

            if (render.TryGetValue("startColorMaxR", out var startColorMaxRToken))
            {
                startColorMax = new Color(
                    (float)startColorMaxRToken.Double,
                    (float)render["startColorMaxG"].Double,
                    (float)render["startColorMaxB"].Double);
            }

            var startSize = (float)render["startSize"].Double;
            var startSizeMin = startSize;
            var startSizeMax = startSize;

            if (render.TryGetValue("startSizeMin", out var startSizeMinToken))
            {
                startSizeMin = (float)startSizeMinToken.Double;
            }

            if (render.TryGetValue("startSizeMax", out var startSizeMaxToken))
            {
                startSizeMax = (float)startSizeMaxToken.Double;
            }

            particleSimulation.SetVisualSpawnParameters(
                startColorMin,
                startColorMax,
                startSizeMin,
                startSizeMax);
            particleRenderer.SetSimulationSpace(simulationSpace);
            particleRenderer.SetRenderParameters(render);
        }
    }
}