using TpLab.Flux.FX.Udon;
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
            var executor = fluxParticleSystem.Executor;
            _isHighStrength = !_isHighStrength;
            executor.SetFloat("custom-lift.strength", _isHighStrength ? highStrength : lowStrength);
            executor.SetVector("custom-lift.direction", _isHighStrength ? Vector3.right : Vector3.up);
            executor.SetFloat("custom-lift.enabled", 1);
        }
    }
}