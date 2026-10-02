using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleEmitter : UdonSharpBehaviour
    {
        const int SpawnSeedPeriod = 1048576;

        float _emissionRate;
        float _emissionAccumulator;
        float _emissionTime;
        float[] _burstTimes;
        int[] _burstCounts;
        int _nextBurstIndex;
        int _particleCount;
        int _spawnCursor;
        int _spawnSeed;

        [PublicAPI]
        public int SpawnStart { get; private set; }

        [PublicAPI]
        public int SpawnSeedStart { get; private set; }

        [PublicAPI]
        public int SpawnCount { get; private set; }

        [PublicAPI]
        public void Initialize(float emissionRate, float[] burstTimes, int[] burstCounts, int particleCount)
        {
            _emissionRate = emissionRate;
            _burstTimes = burstTimes;
            _burstCounts = burstCounts;
            _particleCount = particleCount;
        }

        [PublicAPI]
        public void UpdateEmission(float deltaTime)
        {
            var previousTime = _emissionTime;
            _emissionTime += deltaTime;

            _emissionAccumulator += deltaTime * _emissionRate;

            var rateSpawnCount = Mathf.FloorToInt(_emissionAccumulator);

            if (rateSpawnCount > 0)
            {
                _emissionAccumulator -= rateSpawnCount;
            }

            var burstSpawnCount = GetBurstSpawnCount(previousTime, _emissionTime);

            SpawnStart = _spawnCursor;
            SpawnSeedStart = _spawnSeed;
            SpawnCount = rateSpawnCount + burstSpawnCount;

            _spawnCursor = (_spawnCursor + SpawnCount) % _particleCount;
            _spawnSeed = (_spawnSeed + SpawnCount) % SpawnSeedPeriod;
        }

        int GetBurstSpawnCount(float previousTime, float currentTime)
        {
            var spawnCount = 0;

            while (_nextBurstIndex < _burstTimes.Length)
            {
                var burstTime = _burstTimes[_nextBurstIndex];

                if (burstTime > currentTime)
                {
                    break;
                }

                if (burstTime >= previousTime)
                {
                    spawnCount += _burstCounts[_nextBurstIndex];
                }

                _nextBurstIndex++;
            }

            return spawnCount;
        }
    }
}