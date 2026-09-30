using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleEmitter : UdonSharpBehaviour
    {
        float _emissionRate;
        float _emissionAccumulator;
        int _spawnSequence;

        [PublicAPI]
        public int SpawnStart { get; private set; }

        [PublicAPI]
        public int SpawnCount { get; private set; }

        [PublicAPI]
        public void Initialize(float emissionRate)
        {
            _emissionRate = emissionRate;
        }

        [PublicAPI]
        public void UpdateEmission(float deltaTime)
        {
            _emissionAccumulator += deltaTime * _emissionRate;

            SpawnCount = Mathf.FloorToInt(_emissionAccumulator);

            if (SpawnCount > 0)
                _emissionAccumulator -= SpawnCount;

            SpawnStart = _spawnSequence;
            _spawnSequence += SpawnCount;
        }
    }
}