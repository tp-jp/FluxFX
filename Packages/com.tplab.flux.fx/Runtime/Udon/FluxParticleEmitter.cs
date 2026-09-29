using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleEmitter : UdonSharpBehaviour
    {
        [SerializeField]
        [Min(0)]
        float emissionRate = 8.0f;

        float _emissionAccumulator;
        int _spawnSequence;

        [PublicAPI]
        public int SpawnStart { get; private set; }

        [PublicAPI]
        public int SpawnCount { get; private set; }

        [PublicAPI]
        public void UpdateEmission(float deltaTime)
        {
            _emissionAccumulator += deltaTime * emissionRate;

            SpawnCount = Mathf.FloorToInt(_emissionAccumulator);

            if (SpawnCount > 0)
                _emissionAccumulator -= SpawnCount;

            SpawnStart = _spawnSequence;
            _spawnSequence += SpawnCount;
        }
    }
}