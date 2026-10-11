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
            var gravity = fluxParticleSystem.Gravity;
            if (gravity == null) return;

            var initial = gravity.GetGravity();

            gravity.SetGravity(new Vector3(0, -5, 0));

            var updated = gravity.GetGravity();
            Logger.Log($"Gravity changed: {initial} -> {updated}");
        }
        
        public void TestGpuModuleParameters()
        {
            var force = fluxParticleSystem.Force;
            var drag = fluxParticleSystem.Drag;
            var limitVelocity = fluxParticleSystem.LimitVelocity;
            var noise = fluxParticleSystem.Noise;

            if (force == null || drag == null || limitVelocity == null || noise == null)
            {
                Logger.LogError("GPU Module Parameters: Required module is missing.");
                return;
            }

            force.SetForce(new Vector3(1, 2, 3));
            drag.SetDrag(0.75f);
            limitVelocity.SetMaxSpeed(5.0f);
            noise.SetStrength(1.5f);
            noise.SetScale(2.0f);
            noise.SetSpeed(0.5f);

            if (force.GetForce() != new Vector3(1, 2, 3) ||
                drag.GetDrag() != 0.75f ||
                limitVelocity.GetMaxSpeed() != 5.0f ||
                noise.GetStrength() != 1.5f ||
                noise.GetScale() != 2.0f ||
                noise.GetSpeed() != 0.5f)
            {
                Logger.LogError("GPU Module Parameters: Value mismatch.");
                return;
            }

            Logger.Log("GPU Module Parameters: PASS");
        }
    }
}