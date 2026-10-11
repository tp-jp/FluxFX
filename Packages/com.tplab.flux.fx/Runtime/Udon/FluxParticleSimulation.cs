using JetBrains.Annotations;
using TpLab.Flux.FX.Udon.Parameters;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleSimulation : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleState particleState;

        [SerializeField]
        FluxParticleEmitter particleEmitter;

        [SerializeField]
        FluxKernel velocityUpdateKernel;

        [SerializeField]
        FluxKernel positionUpdateKernel;

        [SerializeField]
        FluxKernel visualUpdateKernel;

        [SerializeField]
        FluxKernel rotationUpdateKernel;

        [SerializeField]
        FluxParticleGpuParameterStore velocityParameterStore;

        [SerializeField]
        FluxParticleGpuParameterStore positionParameterStore;

        float _simulationTime;
        object _lifetime;
        object _shape;
        object _initialVelocity;
        object _velocityOverLifetime;
        object _startColor;
        object _startSize;
        object _startRotation;

        FluxParticleLifetimeParameters Lifetime => (FluxParticleLifetimeParameters)_lifetime;

        FluxParticleShapeParameters Shape => (FluxParticleShapeParameters)_shape;

        FluxParticleInitialVelocityParameters InitialVelocity => (FluxParticleInitialVelocityParameters)_initialVelocity;

        FluxParticleVelocityOverLifetimeParameters VelocityOverLifetime => (FluxParticleVelocityOverLifetimeParameters)_velocityOverLifetime;

        FluxParticleStartColorParameters StartColor => (FluxParticleStartColorParameters)_startColor;

        FluxParticleStartSizeParameters StartSize => (FluxParticleStartSizeParameters)_startSize;

        FluxParticleStartRotationParameters StartRotation => (FluxParticleStartRotationParameters)_startRotation;

        [PublicAPI]
        public void SetFloat(FluxParticleKernelTarget target, string name, float value)
        {
            var store = GetGpuParameterStore(target);

            if (store != null && store.HasParameter(name))
            {
                store.SetFloat(name, value);
                return;
            }

            if (target == FluxParticleKernelTarget.Velocity)
            {
                Logger.LogError($"Velocity GPU parameter is not registered: {name}");
                return;
            }

            var kernel = GetKernel(target);
            if (kernel == null) return;

            kernel.SetFloat(name, value);
        }

        [PublicAPI]
        public void SetVector(FluxParticleKernelTarget target, string name, Vector4 value)
        {
            var store = GetGpuParameterStore(target);

            if (store != null && store.HasParameter(name))
            {
                store.SetVector(name, value);
                return;
            }

            if (target == FluxParticleKernelTarget.Velocity)
            {
                Logger.LogError($"Velocity GPU parameter is not registered: {name}");
                return;
            }

            var kernel = GetKernel(target);
            if (kernel == null) return;

            kernel.SetVector(name, value);
        }

        internal void Initialize(
            FluxParticleLifetimeParameters lifetime,
            FluxParticleShapeParameters shape,
            FluxParticleInitialVelocityParameters initialVelocity,
            FluxParticleVelocityOverLifetimeParameters velocityOverLifetime)
        {
            _lifetime = lifetime;
            _shape = shape;
            _initialVelocity = initialVelocity;
            _velocityOverLifetime = velocityOverLifetime;
        }

        internal FluxParticleGpuParameterStore GetVelocityParameterStore()
        {
            return velocityParameterStore;
        }

        internal void InitializeGpuParameters(DataDictionary gpuParameters)
        {
            InitializeGpuStageParameters(gpuParameters, "velocity", velocityParameterStore, velocityUpdateKernel);
            InitializeGpuStageParameters(gpuParameters, "position", positionParameterStore, positionUpdateKernel);
        }

        void InitializeGpuStageParameters(
            DataDictionary gpuParameters,
            string stageKey,
            FluxParticleGpuParameterStore store,
            FluxKernel kernel)
        {
            if (!gpuParameters.TryGetValue(stageKey, out var stageToken)) return;

            var parameters = stageToken.DataDictionary;

            if (store == null)
            {
                if (parameters.Count > 0)
                {
                    Logger.LogError($"[FluxFX] GPU Parameter Store is not assigned for stage: {stageKey}");
                }

                return;
            }

            store.Initialize(kernel);

            var names = parameters.GetKeys();

            for (var i = 0; i < names.Count; i++)
            {
                var name = names[i].String;
                var parameter = parameters[name].DataDictionary;
                var type = (FluxParticleParameterType)(int)parameter["type"].Double;
                var value = parameter["value"];

                switch (type)
                {
                    case FluxParticleParameterType.Float:
                        store.RegisterFloat(name, (float)value.Double);
                        break;

                    case FluxParticleParameterType.Vector:
                        var components = value.DataList;

                        if (components.Count != 4)
                        {
                            Logger.LogError($"[FluxFX] GPU Vector parameter requires four components: {name}");
                            break;
                        }

                        var vector = new Vector4(
                            (float)components[0].Double,
                            (float)components[1].Double,
                            (float)components[2].Double,
                            (float)components[3].Double);

                        store.RegisterVector(name, vector);
                        break;

                    default:
                        Logger.LogError($"[FluxFX] Unsupported GPU parameter type: {type} ({name})");
                        break;
                }
            }
        }

        internal void SetSimulationSpace(int simulationSpace)
        {
            velocityUpdateKernel.SetFloat("_SimulationSpace", simulationSpace);
            positionUpdateKernel.SetFloat("_SimulationSpace", simulationSpace);
        }

        internal void SetSystemTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var rotationVector = new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);

            velocityUpdateKernel.SetVector("_SystemRotation", rotationVector);

            positionUpdateKernel.SetVector("_SystemPosition", new Vector4(position.x, position.y, position.z, 0));
            positionUpdateKernel.SetVector("_SystemRotation", rotationVector);
            positionUpdateKernel.SetVector("_SystemScale", new Vector4(scale.x, scale.y, scale.z, 0));
        }

        internal void SetSpawnParameters(
            FluxParticleStartColorParameters startColor,
            FluxParticleStartSizeParameters startSize,
            FluxParticleStartRotationParameters startRotation)
        {
            _startColor = startColor;
            _startSize = startSize;
            _startRotation = startRotation;
        }

        internal void Simulate(float deltaTime)
        {
            _simulationTime += deltaTime;

            UpdateVelocity(deltaTime);
            UpdatePosition(deltaTime);

            if (particleEmitter.SpawnCount > 0)
            {
                var startColorMin = StartColor.GetMin();
                var startColorMax = StartColor.GetMax();

                UpdateVisual(startColorMin, startColorMax);
                UpdateRotation(startColorMin.a, startColorMax.a);

                particleState.SwapVisual();
            }

            particleState.SwapSimulation();
        }

        internal bool SetGpuParameterFloat(FluxParticleKernelTarget target, string name, float value)
        {
            var store = GetGpuParameterStore(target);
            if (store == null) return false;

            return store.SetFloat(name, value);
        }

        internal float GetGpuParameterFloat(FluxParticleKernelTarget target, string name)
        {
            var store = GetGpuParameterStore(target);
            if (store == null) return 0.0f;

            return store.GetFloat(name);
        }

        internal FluxParticleGpuParameterStore GetGpuParameterStore(FluxParticleKernelTarget target)
        {
            switch (target)
            {
                case FluxParticleKernelTarget.Velocity:
                    return velocityParameterStore;

                case FluxParticleKernelTarget.Position:
                    return positionParameterStore;

                default:
                    return null;
            }
        }

        FluxKernel GetKernel(FluxParticleKernelTarget target)
        {
            switch (target)
            {
                case FluxParticleKernelTarget.Velocity:
                    return velocityUpdateKernel;

                case FluxParticleKernelTarget.Position:
                    return positionUpdateKernel;

                case FluxParticleKernelTarget.Visual:
                    return visualUpdateKernel;

                case FluxParticleKernelTarget.Rotation:
                    return rotationUpdateKernel;

                default:
                    Debug.LogError($"Invalid FluxFX kernel target: {target}");
                    return null;
            }
        }

        void UpdateVelocity(float deltaTime)
        {
            if (velocityParameterStore != null)
            {
                velocityParameterStore.Flush();
            }

            if (particleEmitter.SpawnCount > 0)
            {
                velocityUpdateKernel.SetFloat("_LifetimeMin", Lifetime.GetMin());
                velocityUpdateKernel.SetFloat("_LifetimeMax", Lifetime.GetMax());
                velocityUpdateKernel.SetFloat("_InitialSpeedMin", InitialVelocity.GetMin());
                velocityUpdateKernel.SetFloat("_InitialSpeedMax", InitialVelocity.GetMax());
                velocityUpdateKernel.SetFloat("_ShapeType", Shape.GetShapeType().ToInt());
                velocityUpdateKernel.SetFloat("_ShapeAngle", Shape.GetAngle());
            }

            velocityUpdateKernel.SetFloat("_DeltaTime", deltaTime);
            velocityUpdateKernel.SetFloat("_FluxFXSimulationTime", _simulationTime);
            velocityUpdateKernel.SetFloat("_SpawnStart", particleEmitter.SpawnStart);
            velocityUpdateKernel.SetFloat("_SpawnSeedStart", particleEmitter.SpawnSeedStart);
            velocityUpdateKernel.SetFloat("_SpawnCount", particleEmitter.SpawnCount);
            velocityUpdateKernel.SetBuffer("_PositionTex", particleState.CurrentPosition);
            velocityUpdateKernel.Dispatch(particleState.CurrentVelocity, particleState.NextVelocity);
        }

        void UpdatePosition(float deltaTime)
        {
            if (positionParameterStore != null)
            {
                positionParameterStore.Flush();
            }

            if (particleEmitter.SpawnCount > 0)
            {
                var shapeSize = Shape.GetSize();

                positionUpdateKernel.SetFloat("_ShapeType", Shape.GetShapeType().ToInt());
                positionUpdateKernel.SetFloat("_ShapeRadius", Shape.GetRadius());
                positionUpdateKernel.SetVector("_ShapeSize", new Vector4(shapeSize.x, shapeSize.y, shapeSize.z, 0));
            }

            var velocityOverLifetimeStart = VelocityOverLifetime.GetStart();
            var velocityOverLifetimeEnd = VelocityOverLifetime.GetEnd();
            var velocityOverLifetimeOrbital = VelocityOverLifetime.GetOrbital();
            var velocityOverLifetimeOffset = VelocityOverLifetime.GetOffset();

            positionUpdateKernel.SetFloat("_VelocityOverLifetimeEnabled", VelocityOverLifetime.GetEnabled() ? 1.0f : 0.0f);
            positionUpdateKernel.SetVector("_VelocityOverLifetimeStart", new Vector4(velocityOverLifetimeStart.x, velocityOverLifetimeStart.y, velocityOverLifetimeStart.z, 0));
            positionUpdateKernel.SetVector("_VelocityOverLifetimeEnd", new Vector4(velocityOverLifetimeEnd.x, velocityOverLifetimeEnd.y, velocityOverLifetimeEnd.z, 0));
            positionUpdateKernel.SetVector("_VelocityOverLifetimeOrbital", new Vector4(velocityOverLifetimeOrbital.x, velocityOverLifetimeOrbital.y, velocityOverLifetimeOrbital.z, 0));
            positionUpdateKernel.SetVector("_VelocityOverLifetimeOffset", new Vector4(velocityOverLifetimeOffset.x, velocityOverLifetimeOffset.y, velocityOverLifetimeOffset.z, 0));
            positionUpdateKernel.SetFloat("_VelocityOverLifetimeRadial", VelocityOverLifetime.GetRadial());
            positionUpdateKernel.SetFloat("_DeltaTime", deltaTime);
            positionUpdateKernel.SetFloat("_FluxFXSimulationTime", _simulationTime);
            positionUpdateKernel.SetFloat("_SpawnStart", particleEmitter.SpawnStart);
            positionUpdateKernel.SetFloat("_SpawnSeedStart", particleEmitter.SpawnSeedStart);
            positionUpdateKernel.SetFloat("_SpawnCount", particleEmitter.SpawnCount);
            positionUpdateKernel.SetBuffer("_VelocityTex", particleState.NextVelocity);
            positionUpdateKernel.SetBuffer("_CurrentVelocityTex", particleState.CurrentVelocity);
            positionUpdateKernel.Dispatch(particleState.CurrentPosition, particleState.NextPosition);
        }

        void UpdateVisual(Color startColorMin, Color startColorMax)
        {
            visualUpdateKernel.SetVector("_StartColorMin", startColorMin);
            visualUpdateKernel.SetVector("_StartColorMax", startColorMax);
            visualUpdateKernel.SetFloat("_StartSizeMin", StartSize.GetMin());
            visualUpdateKernel.SetFloat("_StartSizeMax", StartSize.GetMax());
            visualUpdateKernel.SetFloat("_SpawnStart", particleEmitter.SpawnStart);
            visualUpdateKernel.SetFloat("_SpawnSeedStart", particleEmitter.SpawnSeedStart);
            visualUpdateKernel.SetFloat("_SpawnCount", particleEmitter.SpawnCount);
            visualUpdateKernel.SetBuffer("_CurrentVelocityTex", particleState.CurrentVelocity);
            visualUpdateKernel.SetBuffer("_VelocityTex", particleState.NextVelocity);
            visualUpdateKernel.Dispatch(particleState.CurrentVisual, particleState.NextVisual);
        }

        void UpdateRotation(float startAlphaMin, float startAlphaMax)
        {
            var rotation = StartRotation.GetRotation();

            rotationUpdateKernel.SetVector("_StartRotation", new Vector4(rotation.x, rotation.y, rotation.z, 0));
            rotationUpdateKernel.SetFloat("_StartAlphaMin", startAlphaMin);
            rotationUpdateKernel.SetFloat("_StartAlphaMax", startAlphaMax);
            rotationUpdateKernel.SetFloat("_SpawnStart", particleEmitter.SpawnStart);
            rotationUpdateKernel.SetFloat("_SpawnSeedStart", particleEmitter.SpawnSeedStart);
            rotationUpdateKernel.SetFloat("_SpawnCount", particleEmitter.SpawnCount);
            rotationUpdateKernel.SetBuffer("_CurrentVelocityTex", particleState.CurrentVelocity);
            rotationUpdateKernel.SetBuffer("_VelocityTex", particleState.NextVelocity);
            rotationUpdateKernel.Dispatch(particleState.CurrentRotation, particleState.NextRotation);
        }
    }
}