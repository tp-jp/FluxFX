using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleEmitter : UdonSharpBehaviour
    {
        // Spawn Seedを長時間安定して扱うため、Shader側と同じ周期に制限する。
        const int SpawnSeedPeriod = 1048576;
        const float MinDuration = 0.01f;

        float _emissionAccumulator;
        float _emissionTime;
        float[] _burstTimes;
        int[] _burstCounts;
        int _nextBurstIndex;
        int _particleCount;
        int _spawnCursor;
        int _spawnSeed;
        float _startDelay;
        float _startDelayRemaining;
        float _duration;
        bool _loop;
        bool _emissionCompleted;
        object _parameters;

        FluxParticleEmissionParameters Parameters => (FluxParticleEmissionParameters)_parameters;

        public int SpawnStart { get; private set; }

        public int SpawnSeedStart { get; private set; }

        public int SpawnCount { get; private set; }

        internal void Initialize(
            FluxParticleEmissionParameters parameters,
            float[] burstTimes,
            int[] burstCounts,
            int particleCount,
            float startDelay,
            float duration,
            bool loop)
        {
            _parameters = parameters;
            _burstTimes = burstTimes;
            _burstCounts = burstCounts;
            _particleCount = particleCount;
            _startDelay = Mathf.Max(0, startDelay);
            _duration = Mathf.Max(duration, MinDuration);
            _loop = loop;

            ResetPlayback();
        }

        internal void UpdateEmission(float deltaTime)
        {
            SpawnStart = _spawnCursor;
            SpawnSeedStart = _spawnSeed;
            SpawnCount = 0;

            if (_emissionCompleted) return;

            var remainingTime = deltaTime;

            if (_startDelayRemaining > 0)
            {
                var delayStepTime = Mathf.Min(remainingTime, _startDelayRemaining);

                _startDelayRemaining -= delayStepTime;
                remainingTime -= delayStepTime;

                if (remainingTime <= 0) return;
            }

            while (remainingTime > 0)
            {
                var cycleRemainingTime = _duration - _emissionTime;
                var stepTime = Mathf.Min(remainingTime, cycleRemainingTime);
                var previousTime = _emissionTime;

                _emissionTime += stepTime;
                _emissionAccumulator += stepTime * Parameters.GetRate();

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

        internal void ResetPlayback()
        {
            _emissionAccumulator = 0;
            _emissionTime = 0;
            _startDelayRemaining = _startDelay;
            _nextBurstIndex = 0;
            _emissionCompleted = false;

            SpawnStart = _spawnCursor;
            SpawnSeedStart = _spawnSeed;
            SpawnCount = 0;
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