﻿using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleEmitter : UdonSharpBehaviour
    {
        const int SpawnSeedPeriod = 1048576;
        const float MinDuration = 0.01f;

        float _emissionRate;
        float _emissionAccumulator;
        float _emissionTime;
        float[] _burstTimes;
        int[] _burstCounts;
        int _nextBurstIndex;
        int _particleCount;
        int _spawnCursor;
        int _spawnSeed;
        float _duration;
        bool _loop;
        bool _emissionCompleted;

        [PublicAPI]
        public int SpawnStart { get; private set; }

        [PublicAPI]
        public int SpawnSeedStart { get; private set; }

        [PublicAPI]
        public int SpawnCount { get; private set; }

        [PublicAPI]
        public void Initialize(
            float emissionRate,
            float[] burstTimes,
            int[] burstCounts,
            int particleCount,
            float duration,
            bool loop)
        {
            _emissionRate = emissionRate;
            _burstTimes = burstTimes;
            _burstCounts = burstCounts;
            _particleCount = particleCount;
            _duration = Mathf.Max(duration, MinDuration);
            _loop = loop;
        }

        [PublicAPI]
        public void UpdateEmission(float deltaTime)
        {
            SpawnStart = _spawnCursor;
            SpawnSeedStart = _spawnSeed;
            SpawnCount = 0;

            if (_emissionCompleted) return;

            var remainingTime = deltaTime;

            while (remainingTime > 0)
            {
                var cycleRemainingTime = _duration - _emissionTime;
                var stepTime = Mathf.Min(remainingTime, cycleRemainingTime);
                var previousTime = _emissionTime;

                _emissionTime += stepTime;
                _emissionAccumulator += stepTime * _emissionRate;

                var rateSpawnCount = Mathf.FloorToInt(_emissionAccumulator);

                if (rateSpawnCount > 0)
                {
                    _emissionAccumulator -= rateSpawnCount;
                }

                SpawnCount += rateSpawnCount;
                SpawnCount += GetBurstSpawnCount(previousTime, _emissionTime);

                remainingTime -= stepTime;

                if (_emissionTime < _duration)
                {
                    break;
                }

                if (!_loop)
                {
                    _emissionCompleted = true;
                    break;
                }

                _emissionTime = 0;
                _nextBurstIndex = 0;
            }

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