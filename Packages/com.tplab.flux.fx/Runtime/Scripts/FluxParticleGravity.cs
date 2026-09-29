using UnityEngine;

namespace TpLab.Flux.FX.Scripts
{
    [DisallowMultipleComponent]
    public class FluxParticleGravity : MonoBehaviour
    {
        [SerializeField]
        Vector3 gravity = new Vector3(0, -1.0f, 0);

        public Vector3 Gravity => gravity;
    }
}