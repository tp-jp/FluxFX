using TpLab.Flux.FX.Udon.Parameters;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

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
        object _lifetime;
        object _shape;
        object _initialVelocity;
        object _gravity;
        object _force;
        object _drag;
        object _noise;
        object _vortex;
        object _limitVelocity;
        object _startColor;
        object _startSize;

        FluxParticleLifetimeParameters Lifetime => (FluxParticleLifetimeParameters)_lifetime;

        FluxParticleShapeParameters Shape => (FluxParticleShapeParameters)_shape;

        FluxParticleInitialVelocityParameters InitialVelocity => (FluxParticleInitialVelocityParameters)_initialVelocity;

        FluxParticleGravityParameters Gravity => (FluxParticleGravityParameters)_gravity;

        FluxParticleForceParameters Force => (FluxParticleForceParameters)_force;

        FluxParticleDragParameters Drag => (FluxParticleDragParameters)_drag;

        FluxParticleNoiseParameters Noise => (FluxParticleNoiseParameters)_noise;

        FluxParticleVortexParameters Vortex => (FluxParticleVortexParameters)_vortex;

        FluxParticleLimitVelocityParameters LimitVelocity => (FluxParticleLimitVelocityParameters)_limitVelocity;

        FluxParticleStartColorParameters StartColor => (FluxParticleStartColorParameters)_startColor;

        FluxParticleStartSizeParameters StartSize => (FluxParticleStartSizeParameters)_startSize;

        internal void Initialize(
            FluxParticleLifetimeParameters lifetime,
            FluxParticleShapeParameters shape,
            FluxParticleInitialVelocityParameters initialVelocity,
            FluxParticleGravityParameters gravity,
            FluxParticleForceParameters force,
            FluxParticleDragParameters drag,
            FluxParticleNoiseParameters noise,
            FluxParticleVortexParameters vortex,
            FluxParticleLimitVelocityParameters limitVelocity)
        {
            _lifetime = lifetime;
            _shape = shape;
            _initialVelocity = initialVelocity;
            _gravity = gravity;
            _force = force;
            _drag = drag;
            _noise = noise;
            _vortex = vortex;
            _limitVelocity = limitVelocity;
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

        internal void SetVisualSpawnParameters(FluxParticleStartColorParameters startColor, FluxParticleStartSizeParameters startSize)
        {
            _startColor = startColor;
            _startSize = startSize;
        }

        internal void Simulate(float deltaTime)
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

        void UpdateVelocity(float deltaTime)
        {
            var gravity = Gravity.GetGravity();
            var force = Force.GetForce();
            var vortexCenter = Vortex.GetCenter();
            var vortexAxis = Vortex.GetAxis();

            if (particleEmitter.SpawnCount > 0)
            {
                velocityUpdateKernel.SetFloat("_LifetimeMin", Lifetime.GetMin());
                velocityUpdateKernel.SetFloat("_LifetimeMax", Lifetime.GetMax());
                velocityUpdateKernel.SetFloat("_InitialSpeedMin", InitialVelocity.GetMin());
                velocityUpdateKernel.SetFloat("_InitialSpeedMax", InitialVelocity.GetMax());
                velocityUpdateKernel.SetFloat("_ShapeType", Shape.GetShapeType().ToInt());
                velocityUpdateKernel.SetFloat("_ShapeAngle", Shape.GetAngle());
            }

            velocityUpdateKernel.SetVector("_Gravity", new Vector4(gravity.x, gravity.y, gravity.z, 0));
            velocityUpdateKernel.SetVector("_Force", new Vector4(force.x, force.y, force.z, 0));
            velocityUpdateKernel.SetFloat("_Drag", Drag.GetDrag());
            velocityUpdateKernel.SetFloat("_NoiseStrength", Noise.GetStrength());
            velocityUpdateKernel.SetFloat("_NoiseScale", Noise.GetScale());
            velocityUpdateKernel.SetFloat("_NoiseTime", _simulationTime * Noise.GetSpeed());
            velocityUpdateKernel.SetVector("_VortexCenter", new Vector4(vortexCenter.x, vortexCenter.y, vortexCenter.z, 0));
            velocityUpdateKernel.SetVector("_VortexAxis", new Vector4(vortexAxis.x, vortexAxis.y, vortexAxis.z, 0));
            velocityUpdateKernel.SetFloat("_VortexStrength", Vortex.GetStrength());
            velocityUpdateKernel.SetFloat("_MaxSpeed", LimitVelocity.GetMaxSpeed());
            velocityUpdateKernel.SetFloat("_DeltaTime", deltaTime);
            velocityUpdateKernel.SetFloat("_SpawnStart", particleEmitter.SpawnStart);
            velocityUpdateKernel.SetFloat("_SpawnSeedStart", particleEmitter.SpawnSeedStart);
            velocityUpdateKernel.SetFloat("_SpawnCount", particleEmitter.SpawnCount);
            velocityUpdateKernel.SetBuffer("_PositionTex", particleState.CurrentPosition);
            velocityUpdateKernel.Dispatch(particleState.CurrentVelocity, particleState.NextVelocity);
        }

        void UpdatePosition(float deltaTime)
        {
            if (particleEmitter.SpawnCount > 0)
            {
                var shapeSize = Shape.GetSize();

                positionUpdateKernel.SetFloat("_ShapeType", Shape.GetShapeType().ToInt());
                positionUpdateKernel.SetFloat("_ShapeRadius", Shape.GetRadius());
                positionUpdateKernel.SetVector("_ShapeSize", new Vector4(shapeSize.x, shapeSize.y, shapeSize.z, 0));
            }

            positionUpdateKernel.SetFloat("_DeltaTime", deltaTime);
            positionUpdateKernel.SetFloat("_SpawnStart", particleEmitter.SpawnStart);
            positionUpdateKernel.SetFloat("_SpawnSeedStart", particleEmitter.SpawnSeedStart);
            positionUpdateKernel.SetFloat("_SpawnCount", particleEmitter.SpawnCount);
            positionUpdateKernel.SetBuffer("_VelocityTex", particleState.NextVelocity);
            positionUpdateKernel.SetBuffer("_CurrentVelocityTex", particleState.CurrentVelocity);
            positionUpdateKernel.Dispatch(particleState.CurrentPosition, particleState.NextPosition);
        }

        void UpdateVisual()
        {
            visualUpdateKernel.SetVector("_StartColorMin", StartColor.GetMin());
            visualUpdateKernel.SetVector("_StartColorMax", StartColor.GetMax());
            visualUpdateKernel.SetFloat("_StartSizeMin", StartSize.GetMin());
            visualUpdateKernel.SetFloat("_StartSizeMax", StartSize.GetMax());
            visualUpdateKernel.SetFloat("_SpawnStart", particleEmitter.SpawnStart);
            visualUpdateKernel.SetFloat("_SpawnSeedStart", particleEmitter.SpawnSeedStart);
            visualUpdateKernel.SetFloat("_SpawnCount", particleEmitter.SpawnCount);
            visualUpdateKernel.SetBuffer("_CurrentVelocityTex", particleState.CurrentVelocity);
            visualUpdateKernel.SetBuffer("_VelocityTex", particleState.NextVelocity);
            visualUpdateKernel.Dispatch(particleState.CurrentVisual, particleState.NextVisual);
        }
    }
}