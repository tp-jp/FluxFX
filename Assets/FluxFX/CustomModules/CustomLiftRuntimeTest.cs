using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.CustomModules
{
    public class CustomLiftRuntimeTest : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleSystem fluxParticleSystem;

        [SerializeField]
        float lowStrength = 1.0f;

        [SerializeField]
        float highStrength = 5.0f;

        bool _isHighStrength;

        // public override void Interact()
        // {
        //     ToggleParameters();
        // }
        //
        // public void ToggleParameters()
        // {
        //     var lift = fluxParticleSystem.GetCustomLift();
        //     if (lift == null) return;
        //
        //     _isHighStrength = !_isHighStrength;
        //
        //     lift.SetStrength(_isHighStrength ? highStrength : lowStrength);
        //     lift.SetDirection(_isHighStrength ? Vector3.right : Vector3.up);
        //     lift.SetEnabled(true);
        // }
        //
        // public void TestModuleBinding()
        // {
        //     var gravity = fluxParticleSystem.Gravity;
        //     if (gravity == null)
        //     {
        //         Debug.LogError("Gravity binding is missing.");
        //         return;
        //     }
        //
        //     var binding = fluxParticleSystem.TryGetGpuModuleBinding("tplab.fluxfx.gravity");
        //     if (binding == null)
        //     {
        //         Debug.LogError("Gravity generic binding is missing.");
        //         return;
        //     }
        //
        //     var initialEnabled = gravity.GetEnabled();
        //
        //     gravity.SetEnabled(!initialEnabled);
        //
        //     if (fluxParticleSystem.GetModuleEnabled("tplab.fluxfx.gravity") == initialEnabled)
        //     {
        //         Debug.LogError("Gravity binding state mismatch.");
        //         return;
        //     }
        //
        //     gravity.SetEnabled(initialEnabled);
        //
        //     var customLift = fluxParticleSystem.GetCustomLift();
        //     if (customLift == null)
        //     {
        //         Debug.LogError("CustomLift binding is missing.");
        //         return;
        //     }
        //
        //     var missing = fluxParticleSystem.TryGetGpuModuleBinding("example.not-installed");
        //     if (missing != null)
        //     {
        //         Debug.LogError("Missing module unexpectedly returned a binding.");
        //         return;
        //     }
        //
        //     Debug.Log("Module Binding Integration: PASS");
        // }
        //
        // public void TestModuleParameterInheritance()
        // {
        //     var gravity = fluxParticleSystem.Gravity;
        //     var lift = fluxParticleSystem.GetCustomLift();
        //
        //     if (gravity == null || lift == null)
        //     {
        //         Debug.LogError("Module Parameter Inheritance: Missing module.");
        //         return;
        //     }
        //
        //     var gravityEnabled = gravity.GetEnabled();
        //     var liftEnabled = lift.GetEnabled();
        //
        //     gravity.SetEnabled(!gravityEnabled);
        //     lift.SetEnabled(!liftEnabled);
        //
        //     if (gravity.GetEnabled() == gravityEnabled || lift.GetEnabled() == liftEnabled)
        //     {
        //         Debug.LogError("Module Parameter Inheritance: Enabled state mismatch.");
        //         return;
        //     }
        //
        //     var gravityValue = gravity.GetGravity();
        //     var liftStrength = lift.GetStrength();
        //
        //     gravity.SetGravity(new Vector3(0, -3, 0));
        //     lift.SetStrength(4.0f);
        //
        //     if (gravity.GetGravity() != new Vector3(0, -3, 0) || lift.GetStrength() != 4.0f)
        //     {
        //         Debug.LogError("Module Parameter Inheritance: Parameter mismatch.");
        //         return;
        //     }
        //
        //     gravity.SetGravity(gravityValue);
        //     lift.SetStrength(liftStrength);
        //     gravity.SetEnabled(gravityEnabled);
        //     lift.SetEnabled(liftEnabled);
        //
        //     Debug.Log("Module Parameter Inheritance: PASS");
        // }
    }
}