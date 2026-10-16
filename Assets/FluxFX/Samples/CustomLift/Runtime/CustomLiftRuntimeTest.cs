using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.CustomModules
{
    public class CustomLiftRuntimeTest : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleSystem fluxParticleSystem;
        [SerializeField]
        float lowStrength = 1;
        [SerializeField]
        float highStrength = 5;

        bool _isHighStrength;

        public override void Interact()
        {
            ToggleParameters();
        }

        public void ToggleParameters()
        {
            var lift = fluxParticleSystem.Modules.CustomLift();
            if (lift == null) return;
            _isHighStrength = !_isHighStrength;
            lift.SetStrength(_isHighStrength ? highStrength : lowStrength);
            lift.SetDirection(_isHighStrength ? Vector3.right : Vector3.up);
            lift.SetEnabled(true);
        }
    }
}
