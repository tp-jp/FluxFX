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

        DataDictionary _gpuModuleBindings;
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

        [PublicAPI]
        public int ParticleCount => particleCount;

        [PublicAPI]
        public FluxParticleEmissionParameters Emission => (FluxParticleEmissionParameters)_emission;

        [PublicAPI]
        public FluxParticleLifetimeParameters Lifetime => (FluxParticleLifetimeParameters)_lifetime;

        [PublicAPI]
        public FluxParticleShapeParameters Shape => (FluxParticleShapeParameters)_shape;

        [PublicAPI]
        public FluxParticleInitialVelocityParameters InitialVelocity => (FluxParticleInitialVelocityParameters)_initialVelocity;

        [PublicAPI]
        public FluxParticleVelocityOverLifetimeParameters VelocityOverLifetime => (FluxParticleVelocityOverLifetimeParameters)_velocityOverLifetime;

        [PublicAPI]
        public FluxParticleGravityParameters Gravity => (FluxParticleGravityParameters)_gravity;

        [PublicAPI]
        public FluxParticleForceParameters Force => (FluxParticleForceParameters)_force;

        [PublicAPI]
        public FluxParticleDragParameters Drag => (FluxParticleDragParameters)_drag;

        [PublicAPI]
        public FluxParticleNoiseParameters Noise => (FluxParticleNoiseParameters)_noise;

        [PublicAPI]
        public FluxParticleVortexParameters Vortex => (FluxParticleVortexParameters)_vortex;

        [PublicAPI]
        public FluxParticleLimitVelocityParameters LimitVelocity => (FluxParticleLimitVelocityParameters)_limitVelocity;

        [PublicAPI]
        public FluxParticleStartColorParameters StartColor => (FluxParticleStartColorParameters)_startColor;

        [PublicAPI]
        public FluxParticleStartSizeParameters StartSize => (FluxParticleStartSizeParameters)_startSize;

        [PublicAPI]
        public FluxParticleStartRotationParameters StartRotation => (FluxParticleStartRotationParameters)_startRotation;

        [PublicAPI]
        public FluxParticleColorOverLifetimeParameters ColorOverLifetime => (FluxParticleColorOverLifetimeParameters)_colorOverLifetime;

        [PublicAPI]
        public FluxParticleSizeOverLifetimeParameters SizeOverLifetime => (FluxParticleSizeOverLifetimeParameters)_sizeOverLifetime;

        [PublicAPI]
        public FluxParticleRotationOverLifetimeParameters RotationOverLifetime => (FluxParticleRotationOverLifetimeParameters)_rotationOverLifetime;

        [PublicAPI]
        public void Play()
        {
            _isPlaying = true;
        }

        [PublicAPI]
        public void Stop()
        {
            _isPlaying = false;
        }

        [PublicAPI]
        public void Restart()
        {
            particleEmitter.ResetPlayback();
            _isPlaying = true;
        }

        [PublicAPI]
        public void Clear()
        {
            particleState.Clear();
        }

        [PublicAPI]
        public void SetFloat(FluxParticleKernelTarget target, string name, float value)
        {
            particleSimulation.SetFloat(target, name, value);
        }

        [PublicAPI]
        public void SetVector(FluxParticleKernelTarget target, string name, Vector4 value)
        {
            particleSimulation.SetVector(target, name, value);
        }

        [PublicAPI]
        public bool SetModuleEnabled(string moduleId, bool enabled)
        {
            if (!TryGetModuleBinding(moduleId, out var target, out var parameterName))
            {
                Logger.LogError($"GPU Module is not registered: {moduleId}");
                return false;
            }

            return particleSimulation.SetGpuParameterFloat(target, parameterName, enabled ? 1.0f : 0.0f);
        }

        [PublicAPI]
        public bool GetModuleEnabled(string moduleId)
        {
            if (!TryGetModuleBinding(moduleId, out var target, out var parameterName))
            {
                Logger.LogError($"GPU Module is not registered: {moduleId}");
                return false;
            }

            return particleSimulation.GetGpuParameterFloat(target, parameterName) > 0.5f;
        }

        [PublicAPI]
        public DataList GetGpuModuleBinding(string moduleId)
        {
            return TryGetGpuModuleBinding(moduleId);
        }

        [PublicAPI]
        public DataList TryGetGpuModuleBinding(string moduleId)
        {
            if (!TryGetModuleBinding(moduleId, out var target, out var enabledParameter)) return null;

            var store = particleSimulation.GetGpuParameterStore(target);
            if (store == null) return null;

            if (!store.HasParameter(enabledParameter))
            {
                Logger.LogError($"GPU Module Enabled parameter is not registered: {moduleId}");
                return null;
            }

            var data = new DataToken[2];
            data[0] = new DataToken(store);
            data[1] = enabledParameter;

            return new DataList(data);
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

            if (parameters.TryGetValue("gpuParameters", out var gpuParameters))
            {
                particleSimulation.InitializeGpuParameters(gpuParameters.DataDictionary);
            }

            InitializeGpuModuleBindings(parameters);
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

        void InitializeGpuModuleBindings(DataDictionary parameters)
        {
            _gpuModuleBindings = new DataDictionary();

            if (!parameters.TryGetValue("gpuModuleBindings", out var token)) return;

            _gpuModuleBindings = token.DataDictionary;
        }

        void InitializeSimulation(DataDictionary parameters)
        {
            var simulationSpace = (int)parameters["simulationSpace"].Double;

            InitializeLifetime(parameters);
            InitializeShape(parameters);
            InitializeInitialVelocity(parameters);
            InitializeVelocityOverLifetime();
            InitializeGravity();
            InitializeForce();
            InitializeDrag();
            InitializeNoise();
            InitializeVortex();
            InitializeLimitVelocity();

            particleSimulation.Initialize(
                Lifetime,
                Shape,
                InitialVelocity);

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

        void InitializeVelocityOverLifetime()
        {
            _velocityOverLifetime = FluxParticleVelocityOverLifetimeParameters.New(TryGetGpuModuleBinding("tplab.fluxfx.velocity-over-lifetime"));
        }

        void InitializeGravity()
        {
            _gravity = FluxParticleGravityParameters.New(TryGetGpuModuleBinding("tplab.fluxfx.gravity"));
        }

        void InitializeForce()
        {
            _force = FluxParticleForceParameters.New(TryGetGpuModuleBinding("tplab.fluxfx.force"));
        }

        void InitializeNoise()
        {
            _noise = FluxParticleNoiseParameters.New(TryGetGpuModuleBinding("tplab.fluxfx.noise"));
        }

        void InitializeVortex()
        {
            _vortex = FluxParticleVortexParameters.New(TryGetGpuModuleBinding("tplab.fluxfx.vortex"));
        }

        void InitializeDrag()
        {
            _drag = FluxParticleDragParameters.New(TryGetGpuModuleBinding("tplab.fluxfx.drag"));
        }

        void InitializeLimitVelocity()
        {
            _limitVelocity = FluxParticleLimitVelocityParameters.New(TryGetGpuModuleBinding("tplab.fluxfx.limit-velocity"));
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

            particleSimulation.SetSpawnParameters(StartColor, StartSize, StartRotation);
            particleRenderer.SetSimulationSpace(simulationSpace);
            particleRenderer.SetRenderParameters(render, ColorOverLifetime, SizeOverLifetime, RotationOverLifetime);
        }

        void InitializeStartColor(DataDictionary render)
        {
            var startColor = new Color(
                (float)render["startColorR"].Double,
                (float)render["startColorG"].Double,
                (float)render["startColorB"].Double,
                (float)render["startColorA"].Double);

            var min = startColor;
            var max = startColor;

            if (render.TryGetValue("startColorMinR", out var minToken))
            {
                min = new Color(
                    (float)minToken.Double,
                    (float)render["startColorMinG"].Double,
                    (float)render["startColorMinB"].Double,
                    (float)render["startColorMinA"].Double);
            }

            if (render.TryGetValue("startColorMaxR", out var maxToken))
            {
                max = new Color(
                    (float)maxToken.Double,
                    (float)render["startColorMaxG"].Double,
                    (float)render["startColorMaxB"].Double,
                    (float)render["startColorMaxA"].Double);
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
                    (float)color["endColorB"].Double,
                    (float)color["endColorA"].Double);
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
            var endRotation = Vector3.zero;

            if (render.TryGetValue("rotationOverLifetime", out var token))
            {
                var rotation = token.DataDictionary;

                enabled = true;

                endRotation = new Vector3(
                    (float)rotation["endRotationX"].Double,
                    (float)rotation["endRotationY"].Double,
                    (float)rotation["endRotationZ"].Double);
            }

            _rotationOverLifetime = FluxParticleRotationOverLifetimeParameters.New(enabled, endRotation);
        }

        bool TryGetModuleBinding(string moduleId, out FluxParticleKernelTarget target, out string parameterName)
        {
            target = FluxParticleKernelTarget.Velocity;
            parameterName = null;

            if (_gpuModuleBindings == null || string.IsNullOrEmpty(moduleId) || !_gpuModuleBindings.TryGetValue(moduleId, out var token)) return false;

            var binding = token.DataDictionary;

            target = (FluxParticleKernelTarget)(int)binding["target"].Double;
            parameterName = binding["enabledParameter"].String;

            return true;
        }
    }
}