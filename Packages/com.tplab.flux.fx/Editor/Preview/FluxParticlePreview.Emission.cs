using UnityEngine;

namespace TpLab.Flux.FX.Editor.Preview
{
    public sealed partial class FluxParticlePreview
    {
        int UpdateEmission(float deltaTime)
        {
            if (_emissionCompleted) return 0;

            var remainingTime = deltaTime;

            if (_startDelayRemaining > 0)
            {
                var delayStepTime = Mathf.Min(remainingTime, _startDelayRemaining);

                _startDelayRemaining -= delayStepTime;
                remainingTime -= delayStepTime;

                if (remainingTime <= 0) return 0;
            }

            var duration = Mathf.Max(_authoring.Playback.Duration, MinDuration);
            var spawnCount = 0;

            while (remainingTime > 0)
            {
                var cycleRemainingTime = duration - _emissionTime;
                var stepTime = Mathf.Min(remainingTime, cycleRemainingTime);
                var previousTime = _emissionTime;

                _emissionTime += stepTime;
                _emissionAccumulator += stepTime * _authoring.Emission.Rate;

                var rateSpawnCount = Mathf.FloorToInt(_emissionAccumulator);

                if (rateSpawnCount > 0)
                {
                    _emissionAccumulator -= rateSpawnCount;
                }

                spawnCount += rateSpawnCount;
                spawnCount += GetBurstSpawnCount(previousTime, _emissionTime);

                remainingTime -= stepTime;

                if (_emissionTime < duration)
                {
                    break;
                }

                if (!_authoring.Playback.Loop)
                {
                    _emissionCompleted = true;
                    break;
                }

                _emissionTime = 0;
                _nextBurstIndex = 0;
            }

            return spawnCount;
        }

        int GetBurstSpawnCount(float previousTime, float currentTime)
        {
            var bursts = _authoring.Emission.Bursts;
            var spawnCount = 0;

            while (_nextBurstIndex < bursts.Length)
            {
                var burst = bursts[_nextBurstIndex];

                if (burst.Time > currentTime)
                {
                    break;
                }

                if (burst.Time >= previousTime)
                {
                    spawnCount += burst.Count;
                }

                _nextBurstIndex++;
            }

            return spawnCount;
        }
    }
}