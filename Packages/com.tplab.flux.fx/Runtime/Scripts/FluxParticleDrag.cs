using UnityEngine;

namespace TpLab.Flux.FX.Scripts
{
    [DisallowMultipleComponent]
    public class FluxParticleDrag : MonoBehaviour
    {
        [SerializeField]
        [Min(0)]
        float drag = 0.5f;

        public float Drag => drag;
    }
}