using TpLab.Flux.FX.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.CustomModules
{
    public class CustomLiftRuntimeTest : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleSystem particleSystem;

        [SerializeField]
        float lowStrength = 1.0f;

        [SerializeField]
        float highStrength = 5.0f;

        bool _isHighStrength;

        public override void Interact()
        {
            ToggleParameters();
        }

        public void ToggleParameters()
        {
            var lift = particleSystem.GetCustomLift();
            if (lift == null) return;

            _isHighStrength = !_isHighStrength;

            lift.SetStrength(_isHighStrength ? highStrength : lowStrength);
            lift.SetDirection(_isHighStrength ? Vector3.right : Vector3.up);
            lift.SetEnabled(true);
        }
    }
}