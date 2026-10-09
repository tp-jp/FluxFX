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
            _isHighStrength = !_isHighStrength;

            var strength = _isHighStrength ? highStrength : lowStrength;
            var direction = _isHighStrength
                ? new Vector4(1, 0, 0, 0)
                : new Vector4(0, 1, 0, 0);

            particleSystem.SetFloat(FluxParticleKernelTarget.Velocity, "_CustomLiftStrength", strength);
            particleSystem.SetVector(FluxParticleKernelTarget.Velocity, "_CustomLiftDirection", direction);
        }
    }
}