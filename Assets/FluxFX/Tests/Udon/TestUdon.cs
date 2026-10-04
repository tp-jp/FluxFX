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
            fluxParticleSystem.Emission.SetRate(100f);
            fluxParticleSystem.Gravity.SetGravity(Vector3.up * 5f);
        }
    }
}
