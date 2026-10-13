using TpLab.Flux.FX.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Udon
{
    public class TestUdon : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleSystem fluxParticleSystem;

        // public void Test()
        // {
        //     var gravity = fluxParticleSystem.Gravity;
        //     if (gravity == null) return;
        //
        //     var initial = gravity.GetGravity();
        //
        //     gravity.SetGravity(new Vector3(0, -5, 0));
        //
        //     var updated = gravity.GetGravity();
        //     Logger.Log($"Gravity changed: {initial} -> {updated}");
        // }
        //
        // public void TestGpuModuleParameters()
        // {
        //     var force = fluxParticleSystem.Force;
        //     var drag = fluxParticleSystem.Drag;
        //     var limitVelocity = fluxParticleSystem.LimitVelocity;
        //     var noise = fluxParticleSystem.Noise;
        //
        //     if (force == null || drag == null || limitVelocity == null || noise == null)
        //     {
        //         Logger.LogError("GPU Module Parameters: Required module is missing.");
        //         return;
        //     }
        //
        //     force.SetForce(new Vector3(1, 2, 3));
        //     drag.SetDrag(0.75f);
        //     limitVelocity.SetMaxSpeed(5.0f);
        //     noise.SetStrength(1.5f);
        //     noise.SetScale(2.0f);
        //     noise.SetSpeed(0.5f);
        //
        //     if (force.GetForce() != new Vector3(1, 2, 3) ||
        //         drag.GetDrag() != 0.75f ||
        //         limitVelocity.GetMaxSpeed() != 5.0f ||
        //         noise.GetStrength() != 1.5f ||
        //         noise.GetScale() != 2.0f ||
        //         noise.GetSpeed() != 0.5f)
        //     {
        //         Logger.LogError("GPU Module Parameters: Value mismatch.");
        //         return;
        //     }
        //
        //     var vortex = fluxParticleSystem.Vortex;
        //     if (vortex == null) return;
        //
        //     vortex.SetCenter(new Vector3(0, 1, 0));
        //     vortex.SetAxis(Vector3.up);
        //     vortex.SetStrength(3.0f);
        //
        //     Logger.Log($"Vortex Center: {vortex.GetCenter()}");
        //     Logger.Log($"Vortex Axis: {vortex.GetAxis()}");
        //     Logger.Log($"Vortex Strength: {vortex.GetStrength()}");
        //
        //     Logger.Log("GPU Module Parameters: PASS");
        // }
        //
        // public void TestModuleEnabled()
        // {
        //     var gravityId = "tplab.fluxfx.gravity";
        //     var noiseId = "tplab.fluxfx.noise";
        //
        //     if (!fluxParticleSystem.SetModuleEnabled(gravityId, false))
        //     {
        //         Logger.LogError("Failed to disable Gravity.");
        //         return;
        //     }
        //
        //     if (fluxParticleSystem.GetModuleEnabled(gravityId))
        //     {
        //         Logger.LogError("Gravity should be disabled.");
        //         return;
        //     }
        //
        //     if (!fluxParticleSystem.SetModuleEnabled(noiseId, true))
        //     {
        //         Logger.LogError("Failed to enable Noise.");
        //         return;
        //     }
        //
        //     if (!fluxParticleSystem.GetModuleEnabled(noiseId))
        //     {
        //         Logger.LogError("Noise should be enabled.");
        //         return;
        //     }
        //
        //     Logger.Log("GPU Module Enabled Binding: PASS");
        // }
        //
        // public void TestTypedModuleEnabled()
        // {
        //     var gravity = fluxParticleSystem.Gravity;
        //     var force = fluxParticleSystem.Force;
        //     var noise = fluxParticleSystem.Noise;
        //     var vortex = fluxParticleSystem.Vortex;
        //     var drag = fluxParticleSystem.Drag;
        //     var limitVelocity = fluxParticleSystem.LimitVelocity;
        //
        //     if (gravity == null || force == null || noise == null || vortex == null || drag == null || limitVelocity == null)
        //     {
        //         Logger.LogError("Typed Module Enabled: Required module is missing.");
        //         return;
        //     }
        //
        //     var initialGravity = gravity.GetEnabled();
        //     var initialForce = force.GetEnabled();
        //     var initialNoise = noise.GetEnabled();
        //     var initialVortex = vortex.GetEnabled();
        //     var initialDrag = drag.GetEnabled();
        //     var initialLimitVelocity = limitVelocity.GetEnabled();
        //
        //     gravity.SetEnabled(!initialGravity);
        //     force.SetEnabled(!initialForce);
        //     noise.SetEnabled(!initialNoise);
        //     vortex.SetEnabled(!initialVortex);
        //     drag.SetEnabled(!initialDrag);
        //     limitVelocity.SetEnabled(!initialLimitVelocity);
        //
        //     if (gravity.GetEnabled() == initialGravity ||
        //         force.GetEnabled() == initialForce ||
        //         noise.GetEnabled() == initialNoise ||
        //         vortex.GetEnabled() == initialVortex ||
        //         drag.GetEnabled() == initialDrag ||
        //         limitVelocity.GetEnabled() == initialLimitVelocity)
        //     {
        //         Logger.LogError("Typed Module Enabled: State mismatch.");
        //         return;
        //     }
        //
        //     if (fluxParticleSystem.GetModuleEnabled("tplab.fluxfx.gravity") != gravity.GetEnabled() ||
        //         fluxParticleSystem.GetModuleEnabled("tplab.fluxfx.noise") != noise.GetEnabled())
        //     {
        //         Logger.LogError("Typed Module Enabled: Generic API mismatch.");
        //         return;
        //     }
        //
        //     gravity.SetEnabled(initialGravity);
        //     force.SetEnabled(initialForce);
        //     noise.SetEnabled(initialNoise);
        //     vortex.SetEnabled(initialVortex);
        //     drag.SetEnabled(initialDrag);
        //     limitVelocity.SetEnabled(initialLimitVelocity);
        //
        //     Logger.Log("Typed Module Enabled: PASS");
        // }
    }
}