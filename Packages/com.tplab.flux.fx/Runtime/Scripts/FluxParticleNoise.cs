using UnityEngine;

namespace TpLab.Flux.FX.Scripts
{
    [DisallowMultipleComponent]
    public class FluxParticleNoise : MonoBehaviour
    {
        [SerializeField]
        [Min(0)]
        float strength = 1.0f;

        [SerializeField]
        [Min(0)]
        float scale = 1.0f;

        [SerializeField]
        [Min(0)]
        float speed = 1.0f;

        public float Strength => strength;

        public float Scale => scale;

        public float Speed => speed;
    }
}