using JetBrains.Annotations;
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

        [SerializeField]
        Vector3 gravity;

        [SerializeField]
        float drag;

        [SerializeField]
        float noiseStrength;

        [SerializeField]
        float noiseScale;

        [SerializeField]
        float noiseSpeed;

        [SerializeField]
        Vector3 vortexCenter;

        [SerializeField]
        Vector3 vortexAxis;

        [SerializeField]
        float vortexStrength;

        float _simulationTime;

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

        void UpdateVelocity(float deltaTime)
        {
            velocityUpdateKernel.SetFloat("_DeltaTime", deltaTime);
            velocityUpdateKernel.SetFloat("_SpawnStart", particleEmitter.SpawnStart);
            velocityUpdateKernel.SetFloat("_SpawnCount", particleEmitter.SpawnCount);
            velocityUpdateKernel.SetVector("_Gravity", new Vector4(gravity.x, gravity.y, gravity.z, 0));
            velocityUpdateKernel.SetFloat("_Drag", drag);
            velocityUpdateKernel.SetFloat("_NoiseStrength", noiseStrength);
            velocityUpdateKernel.SetFloat("_NoiseScale", noiseScale);
            velocityUpdateKernel.SetFloat("_NoiseTime", _simulationTime * noiseSpeed);
            velocityUpdateKernel.SetVector("_VortexCenter", new Vector4(vortexCenter.x, vortexCenter.y, vortexCenter.z, 0));
            velocityUpdateKernel.SetVector("_VortexAxis", new Vector4(vortexAxis.x, vortexAxis.y, vortexAxis.z, 0));
            velocityUpdateKernel.SetFloat("_VortexStrength", vortexStrength);
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
            visualUpdateKernel.SetBuffer("_CurrentVelocityTex", particleState.CurrentVelocity);
            visualUpdateKernel.SetBuffer("_VelocityTex", particleState.NextVelocity);
            visualUpdateKernel.Dispatch(particleState.CurrentVisual, particleState.NextVisual);
        }
    }
}