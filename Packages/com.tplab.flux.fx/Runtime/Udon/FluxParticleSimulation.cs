using JetBrains.Annotations;
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

        float _simulationTime;
        float _noiseSpeed;

        [PublicAPI]
        public void Initialize(DataDictionary parameters)
        {
            ApplyVelocityParameters(parameters);
        }

        [PublicAPI]
        public void SetSimulationSpace(int simulationSpace)
        {
            velocityUpdateKernel.SetFloat("_SimulationSpace", simulationSpace);
            positionUpdateKernel.SetFloat("_SimulationSpace", simulationSpace);
        }

        [PublicAPI]
        public void SetSystemTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var rotationVector = new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);

            velocityUpdateKernel.SetVector("_SystemRotation", rotationVector);

            positionUpdateKernel.SetVector("_SystemPosition", new Vector4(position.x, position.y, position.z, 0));
            positionUpdateKernel.SetVector("_SystemRotation", rotationVector);
            positionUpdateKernel.SetVector("_SystemScale", new Vector4(scale.x, scale.y, scale.z, 0));
        }

        [PublicAPI]
        public void SetSpawnParameters(
            float lifetimeMin,
            float lifetimeMax,
            int shapeType,
            float shapeRadius,
            float initialSpeedMin,
            float initialSpeedMax)
        {
            velocityUpdateKernel.SetFloat("_LifetimeMin", lifetimeMin);
            velocityUpdateKernel.SetFloat("_LifetimeMax", lifetimeMax);
            velocityUpdateKernel.SetFloat("_InitialSpeedMin", initialSpeedMin);
            velocityUpdateKernel.SetFloat("_InitialSpeedMax", initialSpeedMax);

            positionUpdateKernel.SetFloat("_ShapeType", shapeType);
            positionUpdateKernel.SetFloat("_ShapeRadius", shapeRadius);
        }

        [PublicAPI]
        public void SetVisualSpawnParameters(
            Color startColorMin,
            Color startColorMax,
            float startSizeMin,
            float startSizeMax)
        {
            visualUpdateKernel.SetVector("_StartColorMin", startColorMin);
            visualUpdateKernel.SetVector("_StartColorMax", startColorMax);
            visualUpdateKernel.SetFloat("_StartSizeMin", startSizeMin);
            visualUpdateKernel.SetFloat("_StartSizeMax", startSizeMax);
        }

        [PublicAPI]
        public void Simulate(float deltaTime)
        {
            _simulationTime += deltaTime;

            UpdateVelocity(deltaTime);
            UpdatePosition(deltaTime);

            if (particleEmitter.SpawnCount > 0)
            {
                UpdateVisual();
                particleState.SwapVisual();
            }

            particleState.SwapSimulation();
        }

        void ApplyVelocityParameters(DataDictionary parameters)
        {
            ApplyGravity(parameters);
            ApplyDrag(parameters);
            ApplyNoise(parameters);
            ApplyVortex(parameters);
        }

        void ApplyGravity(DataDictionary parameters)
        {
            if (!parameters.TryGetValue("gravity", out var token))
            {
                velocityUpdateKernel.SetVector("_Gravity", Vector4.zero);
                return;
            }

            var gravity = token.DataDictionary;

            velocityUpdateKernel.SetVector("_Gravity", new Vector4(
                (float)gravity["x"].Double,
                (float)gravity["y"].Double,
                (float)gravity["z"].Double,
                0));
        }

        void ApplyDrag(DataDictionary parameters)
        {
            if (!parameters.TryGetValue("drag", out var token))
            {
                velocityUpdateKernel.SetFloat("_Drag", 0);
                return;
            }

            velocityUpdateKernel.SetFloat("_Drag", (float)token.Double);
        }

        void ApplyNoise(DataDictionary parameters)
        {
            if (!parameters.TryGetValue("noise", out var token))
            {
                velocityUpdateKernel.SetFloat("_NoiseStrength", 0);
                velocityUpdateKernel.SetFloat("_NoiseScale", 0);
                _noiseSpeed = 0;
                return;
            }

            var noise = token.DataDictionary;

            velocityUpdateKernel.SetFloat("_NoiseStrength", (float)noise["strength"].Double);
            velocityUpdateKernel.SetFloat("_NoiseScale", (float)noise["scale"].Double);

            _noiseSpeed = (float)noise["speed"].Double;
        }

        void ApplyVortex(DataDictionary parameters)
        {
            if (!parameters.TryGetValue("vortex", out var token))
            {
                velocityUpdateKernel.SetVector("_VortexCenter", Vector4.zero);
                velocityUpdateKernel.SetVector("_VortexAxis", Vector4.zero);
                velocityUpdateKernel.SetFloat("_VortexStrength", 0);
                return;
            }

            var vortex = token.DataDictionary;

            velocityUpdateKernel.SetVector("_VortexCenter", new Vector4(
                (float)vortex["centerX"].Double,
                (float)vortex["centerY"].Double,
                (float)vortex["centerZ"].Double,
                0));

            velocityUpdateKernel.SetVector("_VortexAxis", new Vector4(
                (float)vortex["axisX"].Double,
                (float)vortex["axisY"].Double,
                (float)vortex["axisZ"].Double,
                0));

            velocityUpdateKernel.SetFloat("_VortexStrength", (float)vortex["strength"].Double);
        }

        void UpdateVelocity(float deltaTime)
        {
            velocityUpdateKernel.SetFloat("_DeltaTime", deltaTime);
            velocityUpdateKernel.SetFloat("_SpawnStart", particleEmitter.SpawnStart);
            velocityUpdateKernel.SetFloat("_SpawnCount", particleEmitter.SpawnCount);
            velocityUpdateKernel.SetFloat("_NoiseTime", _simulationTime * _noiseSpeed);
            velocityUpdateKernel.SetBuffer("_PositionTex", particleState.CurrentPosition);
            velocityUpdateKernel.Dispatch(particleState.CurrentVelocity, particleState.NextVelocity);
        }

        void UpdatePosition(float deltaTime)
        {
            positionUpdateKernel.SetFloat("_DeltaTime", deltaTime);
            positionUpdateKernel.SetBuffer("_VelocityTex", particleState.NextVelocity);
            positionUpdateKernel.SetBuffer("_CurrentVelocityTex", particleState.CurrentVelocity);
            positionUpdateKernel.Dispatch(particleState.CurrentPosition, particleState.NextPosition);
        }

        void UpdateVisual()
        {
            visualUpdateKernel.SetFloat("_SpawnStart", particleEmitter.SpawnStart);
            visualUpdateKernel.SetFloat("_SpawnCount", particleEmitter.SpawnCount);
            visualUpdateKernel.SetBuffer("_CurrentVelocityTex", particleState.CurrentVelocity);
            visualUpdateKernel.SetBuffer("_VelocityTex", particleState.NextVelocity);
            visualUpdateKernel.Dispatch(particleState.CurrentVisual, particleState.NextVisual);
        }
    }
}