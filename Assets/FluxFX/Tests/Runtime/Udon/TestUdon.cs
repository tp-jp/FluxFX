using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Udon
{
    public class TestUdon : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleSystem particleSystem;

        public void Test()
        {
            var gravity = particleSystem.Gravity;
            if (gravity == null) return;

            var initial = gravity.GetGravity();

            gravity.SetGravity(new Vector3(0, -5, 0));

            var updated = gravity.GetGravity();
            Logger.Log($"Gravity changed: {initial} -> {updated}");
        }
    }
}