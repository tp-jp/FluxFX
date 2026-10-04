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
        object _lifetime;
        object _shape;
        object _initialVelocity;
        object _velocityOverLifetime;
        object _gravity;
        object _force;
        object _drag;
        object _noise;
        object _vortex;
        object _limitVelocity;
        object _startColor;
        object _startSize;
        object _startRotation;
        object _colorOverLifetime;
        object _sizeOverLifetime;
        object _rotationOverLifetime;

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
        /// LifetimeのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleLifetimeParameters Lifetime => (FluxParticleLifetimeParameters)_lifetime;

        /// <summary>
        /// ShapeのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleShapeParameters Shape => (FluxParticleShapeParameters)_shape;

        /// <summary>
        /// Initial VelocityのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleInitialVelocityParameters InitialVelocity => (FluxParticleInitialVelocityParameters)_initialVelocity;

        /// <summary>
        /// Velocity over LifetimeのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleVelocityOverLifetimeParameters VelocityOverLifetime => (FluxParticleVelocityOverLifetimeParameters)_velocityOverLifetime;

        /// <summary>
        /// GravityのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleGravityParameters Gravity => (FluxParticleGravityParameters)_gravity;

        /// <summary>
        /// ForceのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleForceParameters Force => (FluxParticleForceParameters)_force;

        /// <summary>
        /// DragのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleDragParameters Drag => (FluxParticleDragParameters)_drag;

        /// <summary>
        /// NoiseのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleNoiseParameters Noise => (FluxParticleNoiseParameters)_noise;

        /// <summary>
        /// VortexのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleVortexParameters Vortex => (FluxParticleVortexParameters)_vortex;

        /// <summary>
        /// Limit VelocityのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleLimitVelocityParameters LimitVelocity => (FluxParticleLimitVelocityParameters)_limitVelocity;

        /// <summary>
        /// Start ColorのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleStartColorParameters StartColor => (FluxParticleStartColorParameters)_startColor;

        /// <summary>
        /// Start SizeのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleStartSizeParameters StartSize => (FluxParticleStartSizeParameters)_startSize;

        /// <summary>
        /// Start RotationのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleStartRotationParameters StartRotation => (FluxParticleStartRotationParameters)_startRotation;

        /// <summary>
        /// Color over LifetimeのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleColorOverLifetimeParameters ColorOverLifetime => (FluxParticleColorOverLifetimeParameters)_colorOverLifetime;

        /// <summary>
        /// Size over LifetimeのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleSizeOverLifetimeParameters SizeOverLifetime => (FluxParticleSizeOverLifetimeParameters)_sizeOverLifetime;

        /// <summary>
        /// Rotation over LifetimeのRuntimeパラメーターを取得します。
        /// </summary>
        [PublicAPI]
        public FluxParticleRotationOverLifetimeParameters RotationOverLifetime => (FluxParticleRotationOverLifetimeParameters)_rotationOverLifetime;

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

            InitializeLifetime(parameters);
            InitializeShape(parameters);
            InitializeInitialVelocity(parameters);
            InitializeVelocityOverLifetime(parameters);
            InitializeGravity(parameters);
            InitializeForce(parameters);
            InitializeDrag(parameters);
            InitializeNoise(parameters);
            InitializeVortex(parameters);
            InitializeLimitVelocity(parameters);

            particleSimulation.Initialize(
                Lifetime,
                Shape,
                InitialVelocity,
                VelocityOverLifetime,
                Gravity,
                Force,
                Drag,
                Noise,
                Vortex,
                LimitVelocity);

            particleSimulation.SetSimulationSpace(simulationSpace);
        }

        void InitializeLifetime(DataDictionary parameters)
        {
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

            _lifetime = FluxParticleLifetimeParameters.New(lifetimeMin, lifetimeMax);
        }

        void InitializeShape(DataDictionary parameters)
        {
            var shape = parameters["shape"].DataDictionary;

            var type = (FluxParticleShapeType)(int)shape["type"].Double;
            var radius = (float)shape["radius"].Double;
            var angle = (float)shape["angle"].Double;
            var size = new Vector3(
                (float)shape["sizeX"].Double,
                (float)shape["sizeY"].Double,
                (float)shape["sizeZ"].Double);

            _shape = FluxParticleShapeParameters.New(type, radius, angle, size);
        }

        void InitializeInitialVelocity(DataDictionary parameters)
        {
            var initialVelocity = parameters["initialVelocity"].DataDictionary;
            var speed = (float)initialVelocity["speed"].Double;
            var min = speed;
            var max = speed;

            if (initialVelocity.TryGetValue("speedMin", out var minToken))
            {
                min = (float)minToken.Double;
            }

            if (initialVelocity.TryGetValue("speedMax", out var maxToken))
            {
                max = (float)maxToken.Double;
            }

            _initialVelocity = FluxParticleInitialVelocityParameters.New(min, max);
        }

        void InitializeVelocityOverLifetime(DataDictionary parameters)
        {
            var enabled = false;
            var start = Vector3.zero;
            var end = Vector3.zero;
            var offset = Vector3.zero;
            var radial = 0.0f;

            if (parameters.TryGetValue("velocityOverLifetime", out var token))
            {
                var velocity = token.DataDictionary;

                enabled = true;
                start = new Vector3(
                    (float)velocity["startX"].Double,
                    (float)velocity["startY"].Double,
                    (float)velocity["startZ"].Double);
                end = new Vector3(
                    (float)velocity["endX"].Double,
                    (float)velocity["endY"].Double,
                    (float)velocity["endZ"].Double);

                if (velocity.TryGetValue("offsetX", out var offsetXToken))
                {
                    offset = new Vector3(
                        (float)offsetXToken.Double,
                        (float)velocity["offsetY"].Double,
                        (float)velocity["offsetZ"].Double);
                }

                if (velocity.TryGetValue("radial", out var radialToken))
                {
                    radial = (float)radialToken.Double;
                }
            }

            _velocityOverLifetime = FluxParticleVelocityOverLifetimeParameters.New(enabled, start, end);

            VelocityOverLifetime.SetOffset(offset);
            VelocityOverLifetime.SetRadial(radial);
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

        void InitializeForce(DataDictionary parameters)
        {
            var force = Vector3.zero;

            if (parameters.TryGetValue("force", out var token))
            {
                var data = token.DataDictionary;

                force = new Vector3(
                    (float)data["x"].Double,
                    (float)data["y"].Double,
                    (float)data["z"].Double);
            }

            _force = FluxParticleForceParameters.New(force);
        }

        void InitializeDrag(DataDictionary parameters)
        {
            var drag = 0.0f;

            if (parameters.TryGetValue("drag", out var token))
            {
                drag = (float)token.Double;
            }

            _drag = FluxParticleDragParameters.New(drag);
        }

        void InitializeNoise(DataDictionary parameters)
        {
            var strength = 0.0f;
            var scale = 0.0f;
            var speed = 0.0f;

            if (parameters.TryGetValue("noise", out var token))
            {
                var data = token.DataDictionary;

                strength = (float)data["strength"].Double;
                scale = (float)data["scale"].Double;
                speed = (float)data["speed"].Double;
            }

            _noise = FluxParticleNoiseParameters.New(strength, scale, speed);
        }

        void InitializeVortex(DataDictionary parameters)
        {
            var center = Vector3.zero;
            var axis = Vector3.zero;
            var strength = 0.0f;

            if (parameters.TryGetValue("vortex", out var token))
            {
                var data = token.DataDictionary;

                center = new Vector3(
                    (float)data["centerX"].Double,
                    (float)data["centerY"].Double,
                    (float)data["centerZ"].Double);

                axis = new Vector3(
                    (float)data["axisX"].Double,
                    (float)data["axisY"].Double,
                    (float)data["axisZ"].Double);

                strength = (float)data["strength"].Double;
            }

            _vortex = FluxParticleVortexParameters.New(center, axis, strength);
        }

        void InitializeLimitVelocity(DataDictionary parameters)
        {
            var maxSpeed = 0.0f;

            if (parameters.TryGetValue("limitVelocity", out var token))
            {
                maxSpeed = (float)token.Double;
            }

            _limitVelocity = FluxParticleLimitVelocityParameters.New(maxSpeed);
        }

        void InitializeRenderer(DataDictionary parameters)
        {
            var simulationSpace = (int)parameters["simulationSpace"].Double;
            var render = parameters["render"].DataDictionary;

            InitializeStartColor(render);
            InitializeStartSize(render);
            InitializeStartRotation(render);
            InitializeColorOverLifetime(render);
            InitializeSizeOverLifetime(render);
            InitializeRotationOverLifetime(render);

            particleSimulation.SetVisualSpawnParameters(StartColor, StartSize);
            particleRenderer.SetSimulationSpace(simulationSpace);
            particleRenderer.SetRenderParameters(render, StartRotation, ColorOverLifetime, SizeOverLifetime, RotationOverLifetime);
        }

        void InitializeStartColor(DataDictionary render)
        {
            var startColor = new Color(
                (float)render["startColorR"].Double,
                (float)render["startColorG"].Double,
                (float)render["startColorB"].Double);
            var min = startColor;
            var max = startColor;

            if (render.TryGetValue("startColorMinR", out var minToken))
            {
                min = new Color(
                    (float)minToken.Double,
                    (float)render["startColorMinG"].Double,
                    (float)render["startColorMinB"].Double);
            }

            if (render.TryGetValue("startColorMaxR", out var maxToken))
            {
                max = new Color(
                    (float)maxToken.Double,
                    (float)render["startColorMaxG"].Double,
                    (float)render["startColorMaxB"].Double);
            }

            _startColor = FluxParticleStartColorParameters.New(min, max);
        }

        void InitializeStartSize(DataDictionary render)
        {
            var startSize = (float)render["startSize"].Double;
            var min = startSize;
            var max = startSize;

            if (render.TryGetValue("startSizeMin", out var minToken))
            {
                min = (float)minToken.Double;
            }

            if (render.TryGetValue("startSizeMax", out var maxToken))
            {
                max = (float)maxToken.Double;
            }

            _startSize = FluxParticleStartSizeParameters.New(min, max);
        }

        void InitializeStartRotation(DataDictionary render)
        {
            var rotation = new Vector3(
                (float)render["startRotationX"].Double,
                (float)render["startRotationY"].Double,
                (float)render["startRotationZ"].Double);

            _startRotation = FluxParticleStartRotationParameters.New(rotation);
        }

        void InitializeColorOverLifetime(DataDictionary render)
        {
            var enabled = false;
            var endColor = Color.white;

            if (render.TryGetValue("colorOverLifetime", out var token))
            {
                var color = token.DataDictionary;

                enabled = true;
                endColor = new Color(
                    (float)color["endColorR"].Double,
                    (float)color["endColorG"].Double,
                    (float)color["endColorB"].Double);
            }

            _colorOverLifetime = FluxParticleColorOverLifetimeParameters.New(enabled, endColor);
        }

        void InitializeSizeOverLifetime(DataDictionary render)
        {
            var enabled = false;
            var endSize = 0.0f;

            if (render.TryGetValue("sizeOverLifetime", out var token))
            {
                var size = token.DataDictionary;

                enabled = true;
                endSize = (float)size["endSize"].Double;
            }

            _sizeOverLifetime = FluxParticleSizeOverLifetimeParameters.New(enabled, endSize);
        }

        void InitializeRotationOverLifetime(DataDictionary render)
        {
            var enabled = false;
            var endRotation = 0.0f;

            if (render.TryGetValue("rotationOverLifetime", out var token))
            {
                var rotation = token.DataDictionary;

                enabled = true;
                endRotation = (float)rotation["endRotation"].Double;
            }

            _rotationOverLifetime = FluxParticleRotationOverLifetimeParameters.New(enabled, endRotation);
        }
    }
}