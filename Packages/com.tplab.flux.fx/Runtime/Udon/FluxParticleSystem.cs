using JetBrains.Annotations;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// GPU上でParticleのSimulationとRenderingを行うParticle Systemです。
    /// </summary>
    [PublicAPI]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleSystem : UdonSharpBehaviour
    {
        [SerializeField]
        [Min(1)]
        int particleCount = 64;

        [SerializeField]
        bool playOnAwake = true;

        [SerializeField]
        FluxParticleState particleState;

        [SerializeField]
        FluxParticleEmitter particleEmitter;

        [SerializeField]
        FluxParticleSimulation particleSimulation;

        [SerializeField]
        FluxParticleRenderer particleRenderer;

        [SerializeField]
        string compiledParameters;

        bool _isPlaying;
        object _emission;
        object _gravity;

        /// <summary>
        /// Particle Systemが使用するParticle数を取得します。
        /// </summary>
        [PublicAPI]
        public int ParticleCount => particleCount;

        /// <summary>
        /// EmissionのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleEmissionParameters Emission => (FluxParticleEmissionParameters)_emission;

        /// <summary>
        /// GravityのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleGravityParameters Gravity => (FluxParticleGravityParameters)_gravity;

        /// <summary>
        /// ParticleのEmissionを開始または再開します。
        /// </summary>
        [PublicAPI]
        public void Play()
        {
            _isPlaying = true;
        }

        /// <summary>
        /// ParticleのEmissionを停止します。既存のParticleはSimulationを継続します。
        /// </summary>
        [PublicAPI]
        public void Stop()
        {
            _isPlaying = false;
        }

        /// <summary>
        /// 既存のParticleを維持したままEmissionのタイムラインを先頭から再開します。
        /// </summary>
        [PublicAPI]
        public void Restart()
        {
            particleEmitter.ResetPlayback();
            _isPlaying = true;
        }

        /// <summary>
        /// 現在存在するParticleをすべて消去します。Playback状態には影響しません。
        /// </summary>
        [PublicAPI]
        public void Clear()
        {
            particleState.Clear();
        }

        void Start()
        {
            particleState.Initialize(particleCount);

            InitializeCompiledParameters();

            particleRenderer.Initialize(particleCount);
            particleRenderer.SetState(particleState);

            _isPlaying = playOnAwake;
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;

            particleEmitter.UpdateEmission(_isPlaying ? deltaTime : 0);
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
            var playback = parameters["playback"].DataDictionary;
            var startDelay = (float)playback["startDelay"].Double;
            var duration = (float)playback["duration"].Double;
            var loop = playback["loop"].Boolean;

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

            _emission = FluxParticleEmissionParameters.New(emissionRate);

            particleEmitter.Initialize(
                Emission,
                burstTimes,
                burstCounts,
                particleCount,
                startDelay,
                duration,
                loop);
        }

        void InitializeSimulation(DataDictionary parameters)
        {
            var simulationSpace = (int)parameters["simulationSpace"].Double;

            InitializeGravity(parameters);

            particleSimulation.Initialize(parameters, Gravity);
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
            var shapeAngle = (float)shape["angle"].Double;
            var shapeSize = new Vector3(
                (float)shape["sizeX"].Double,
                (float)shape["sizeY"].Double,
                (float)shape["sizeZ"].Double);

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
                initialSpeedMax = (float)initialVelocity["speedMax"].Double;
            }

            particleSimulation.SetSpawnParameters(
                lifetimeMin,
                lifetimeMax,
                shapeType,
                shapeRadius,
                shapeAngle,
                shapeSize,
                initialSpeedMin,
                initialSpeedMax);
        }

        void InitializeGravity(DataDictionary parameters)
        {
            var gravity = Vector3.zero;

            if (parameters.TryGetValue("gravity", out var token))
            {
                var data = token.DataDictionary;

                gravity = new Vector3(
                    (float)data["x"].Double,
                    (float)data["y"].Double,
                    (float)data["z"].Double);
            }

            _gravity = FluxParticleGravityParameters.New(gravity);
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
                    (float)render["startColorMaxR"].Double,
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