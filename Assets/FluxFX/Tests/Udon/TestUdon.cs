using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Udon
{
    public class TestUdon : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleSystem fluxParticleSystem;

        public void Test()
        {
            // fluxParticleSystem.Force.SetForce(new Vector3(0, 9.81f, 0));
            // fluxParticleSystem.Drag.SetDrag(0.1f);
            // fluxParticleSystem.Noise.SetStrength(0.5f);
            // fluxParticleSystem.Noise.SetScale(1.0f);
            // fluxParticleSystem.Noise.SetSpeed(1.0f);
            // fluxParticleSystem.Vortex.SetCenter(new Vector3(0, 0, 0));
            // fluxParticleSystem.Vortex.SetAxis(new Vector3(0, 1, 0));
            // fluxParticleSystem.Vortex.SetStrength(1.0f);
            // fluxParticleSystem.LimitVelocity.SetMaxSpeed(2.0f);
            // fluxParticleSystem.Shape.SetShapeType(FluxParticleShapeType.Sphere);
            // fluxParticleSystem.Shape.SetRadius(3.0f);
            // fluxParticleSystem.StartColor.SetMin(Color.red);
            // fluxParticleSystem.StartColor.SetMax(Color.red);
            // fluxParticleSystem.StartSize.SetMin(3.0f);
            // fluxParticleSystem.StartSize.SetMax(3.0f);
            // fluxParticleSystem.StartRotation.SetRotation(new Vector3(0, 45, 0));
            // fluxParticleSystem.ColorOverLifetime.SetEnabled(true);
            // fluxParticleSystem.ColorOverLifetime.SetEndColor(Color.red);
            // fluxParticleSystem.SizeOverLifetime.SetEnabled(true);
            // fluxParticleSystem.SizeOverLifetime.SetEndSize(3.0f);
            // fluxParticleSystem.RotationOverLifetime.SetEnabled(true);
            // fluxParticleSystem.RotationOverLifetime.SetEndRotation(360.0f);
            // fluxParticleSystem.VelocityOverLifetime.SetStart(Vector3.zero);
            // fluxParticleSystem.VelocityOverLifetime.SetEnd(Vector3.zero);
            // fluxParticleSystem.VelocityOverLifetime.SetOffset(Vector3.zero);
            // fluxParticleSystem.VelocityOverLifetime.SetRadial(0.0f);
            // fluxParticleSystem.VelocityOverLifetime.SetOrbital(new Vector3(0, Mathf.PI * 2.0f, 0));
            // fluxParticleSystem.VelocityOverLifetime.SetEnabled(true);
            fluxParticleSystem.StartRotation.SetRotation(new Vector3(0, 90, 0));
        }
    }
}