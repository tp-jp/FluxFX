using System;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleEmitter : UdonSharpBehaviour
    {
        // Spawn Seedを長時間安定して扱うため、Shader側と同じ周期に制限する。
        const int SpawnSeedPeriod = 1048576;

        float[] _burstTimes;
        int[] _burstCounts;
        int _particleCount;
        int _spawnCursor;
        int _spawnSeed;
        float _startDelay;
        float _duration;
        bool _loop;
        object _parameters;
        double[] _playback;

        FluxParticleEmissionParameters Parameters => (FluxParticleEmissionParameters)_parameters;

        public int SpawnStart { get; private set; }

        public int SpawnSeedStart { get; private set; }

        public int SpawnCount { get; private set; }

        /// <summary>
        /// 非LoopのEmissionが終了しているかどうか
        /// </summary>
        public bool HasCompleted => FluxParticleEmissionScheduler.HasCompleted(_playback);

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
            _duration = Mathf.Max(duration, FluxParticleEmissionScheduler.MinimumDuration);
            _loop = loop;
            _playback = FluxParticleEmissionScheduler.Create(_startDelay);

            ResetPlayback();
        }

        internal void UpdateEmission(float deltaTime)
        {
            SpawnStart = _spawnCursor;
            SpawnSeedStart = _spawnSeed;
            SpawnCount = 0;

            var requested = FluxParticleEmissionScheduler.Emit(_playback, deltaTime, Parameters.GetRate(),
                _duration, _loop, _burstTimes, _burstCounts);
            SpawnCount = (int)Math.Min(requested, _particleCount);
            var skipped = requested - SpawnCount;
            SpawnStart = Wrap(_spawnCursor, skipped, _particleCount);
            SpawnSeedStart = Wrap(_spawnSeed, skipped, SpawnSeedPeriod);
            _spawnCursor = Wrap(_spawnCursor, requested, _particleCount);
            _spawnSeed = Wrap(_spawnSeed, requested, SpawnSeedPeriod);
        }

        static int Wrap(int start, long count, int period)
        {
            var divisor = (long)period;
            var remainder = count - count / divisor * divisor;
            var position = start + remainder;
            if (position >= divisor) position -= divisor;
            return (int)position;
        }

        internal void ResetPlayback()
        {
            FluxParticleEmissionScheduler.Reset(_playback, _startDelay);
            SpawnStart = _spawnCursor;
            SpawnSeedStart = _spawnSeed;
            SpawnCount = 0;
        }
    }
}
