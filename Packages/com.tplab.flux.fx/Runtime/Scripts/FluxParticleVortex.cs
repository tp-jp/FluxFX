using UnityEngine;

namespace TpLab.Flux.FX.Scripts
{
    [DisallowMultipleComponent]
    public class FluxParticleVortex : MonoBehaviour
    {
        [SerializeField]
        Vector3 center;

        [SerializeField]
        Vector3 axis = Vector3.up;

        [SerializeField]
        [Min(0)]
        float strength = 1.0f;

        public Vector3 Center => center;

        public Vector3 Axis => axis;

        public float Strength => strength;
    }
}