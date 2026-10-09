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
            ToggleStrength();
        }

        public void ToggleStrength()
        {
            _isHighStrength = !_isHighStrength;

            var strength = _isHighStrength ? highStrength : lowStrength;
            particleSystem.SetVelocityFloat("_CustomLiftStrength", strength);
        }
    }
}