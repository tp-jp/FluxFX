using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleVelocityOverLifetimeTest : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleSystem fluxParticleSystem;

        [SerializeField]
        float interval = 5.0f;

        int _step;

        // // Unity Events
        //
        // void Start()
        // {
        //     SendCustomEventDelayedSeconds(nameof(RunNextStep), 1.0f);
        // }
        //
        // // Udon Events
        //
        // public void RunNextStep()
        // {
        //     var parameters = fluxParticleSystem.VelocityOverLifetime;
        //
        //     if (parameters == null)
        //     {
        //         Logger.LogError("Velocity over Lifetime Module is not registered.");
        //         return;
        //     }
        //
        //     switch (_step)
        //     {
        //         case 0:
        //             TestLinear(parameters);
        //             break;
        //
        //         case 1:
        //             TestOrbital(parameters);
        //             break;
        //
        //         case 2:
        //             TestRadial(parameters);
        //             break;
        //
        //         case 3:
        //             TestOffset(parameters);
        //             break;
        //
        //         case 4:
        //             TestDisabled(parameters);
        //             break;
        //
        //         case 5:
        //             TestEnabled(parameters);
        //             break;
        //
        //         default:
        //             Logger.Log("Velocity over Lifetime test completed.");
        //             return;
        //     }
        //
        //     ValidateParameters(parameters);
        //
        //     _step++;
        //     SendCustomEventDelayedSeconds(nameof(RunNextStep), interval);
        // }
        //
        // // Private Methods
        //
        // void TestLinear(FluxParticleVelocityOverLifetimeParameters parameters)
        // {
        //     parameters.SetEnabled(true);
        //     parameters.SetStart(new Vector3(0, 2, 0));
        //     parameters.SetEnd(new Vector3(0, -2, 0));
        //     parameters.SetOrbital(Vector3.zero);
        //     parameters.SetOffset(Vector3.zero);
        //     parameters.SetRadial(0);
        //
        //     Logger.Log("Step 1: Linear - Up to Down");
        // }
        //
        // void TestOrbital(FluxParticleVelocityOverLifetimeParameters parameters)
        // {
        //     parameters.SetStart(Vector3.zero);
        //     parameters.SetEnd(Vector3.zero);
        //     parameters.SetOrbital(new Vector3(0, 2, 0));
        //     parameters.SetOffset(Vector3.zero);
        //     parameters.SetRadial(0);
        //
        //     Logger.Log("Step 2: Orbital - Y Axis");
        // }
        //
        // void TestRadial(FluxParticleVelocityOverLifetimeParameters parameters)
        // {
        //     parameters.SetStart(Vector3.zero);
        //     parameters.SetEnd(Vector3.zero);
        //     parameters.SetOrbital(Vector3.zero);
        //     parameters.SetOffset(Vector3.zero);
        //     parameters.SetRadial(2.0f);
        //
        //     Logger.Log("Step 3: Radial - Outward");
        // }
        //
        // void TestOffset(FluxParticleVelocityOverLifetimeParameters parameters)
        // {
        //     parameters.SetOrbital(new Vector3(0, 2, 0));
        //     parameters.SetOffset(new Vector3(2, 0, 0));
        //     parameters.SetRadial(1.0f);
        //
        //     Logger.Log("Step 4: Orbital and Radial with Offset");
        // }
        //
        // void TestDisabled(FluxParticleVelocityOverLifetimeParameters parameters)
        // {
        //     parameters.SetEnabled(false);
        //
        //     Logger.Log("Step 5: Module Disabled");
        // }
        //
        // void TestEnabled(FluxParticleVelocityOverLifetimeParameters parameters)
        // {
        //     parameters.SetEnabled(true);
        //
        //     Logger.Log("Step 6: Module Enabled");
        // }
        //
        // void ValidateParameters(FluxParticleVelocityOverLifetimeParameters parameters)
        // {
        //     var start = parameters.GetStart();
        //     var end = parameters.GetEnd();
        //     var orbital = parameters.GetOrbital();
        //     var offset = parameters.GetOffset();
        //     var radial = parameters.GetRadial();
        //     var enabled = parameters.GetEnabled();
        //
        //     Logger.Log(
        //         $"Step {_step + 1} Values: " +
        //         $"Enabled={enabled}, " +
        //         $"Start={start}, " +
        //         $"End={end}, " +
        //         $"Orbital={orbital}, " +
        //         $"Offset={offset}, " +
        //         $"Radial={radial}");
        // }
    }
}
