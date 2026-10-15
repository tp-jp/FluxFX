using System;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleEmitter : UdonSharpBehaviour
    {
        // Spawn Seedを長時間安定して扱うため、Shader側と同じ周期に制限する。
        const int SpawnSeedPeriod = 1048576;

        [SerializeField]
        float rate;
        [SerializeField]
        float startDelay;
        [SerializeField]
        float duration = 5;
        [SerializeField]
        bool loop = true;
        [SerializeField]
        float[] burstTimes = new float[0];
        [SerializeField]
        int[] burstCounts = new int[0];

        float[] _burstTimes;
        int[] _burstCounts;
        int _particleCount;
        int _spawnCursor;
        int _spawnSeed;
        float _duration;
        bool _loop;
        double[] _playback;

        public int SpawnStart { get; private set; }

        public int SpawnSeedStart { get; private set; }

        public int SpawnCount { get; private set; }

        /// <summary>
        /// 非LoopのEmissionが終了しているかどうか
        /// </summary>
        public bool HasCompleted => _playback != null && FluxParticleEmissionScheduler.HasCompleted(_playback);

        public float Rate => rate;
        public float StartDelay => startDelay;
        public float Duration => duration;
        public bool Loop => loop;
        public int BurstCount => burstTimes.Length;

        /// <summary>
        /// 1秒あたりの生成数を変更します。
        /// </summary>
        public bool SetRate(float value)
        {
            if (!FiniteNonNegative(value)) return false;
            rate = value;
            return true;
        }

        /// <summary>
        /// 次回Restartの待機秒数を変更します。
        /// </summary>
        public bool SetStartDelay(float value)
        {
            if (!FiniteNonNegative(value)) return false;
            startDelay = value;
            return true;
        }

        /// <summary>
        /// 次回Restartの周期を変更します。
        /// </summary>
        public bool SetDuration(float value)
        {
            if (!FiniteNonNegative(value) || value < FluxParticleEmissionScheduler.MinimumDuration) return false;
            duration = value;
            return true;
        }

        /// <summary>
        /// 次回RestartのLoop設定を変更します。
        /// </summary>
        public void SetLoop(bool value)
        {
            loop = value;
        }

        public float GetBurstTime(int index)
        {
            if (index < 0 || index >= burstTimes.Length) return 0;
            return burstTimes[index];
        }

        public int GetBurstCount(int index)
        {
            if (index < 0 || index >= burstCounts.Length) return 0;
            return burstCounts[index];
        }

        /// <summary>
        /// 昇順のBurst設定をコピーし、次回Restartから使用します。
        /// </summary>
        public bool SetBursts(float[] times, int[] counts)
        {
            if (times == null || counts == null || times.Length != counts.Length) return false;
            for (var i = 0; i < times.Length; i++)
            {
                if (!FiniteNonNegative(times[i]) || counts[i] < 1 || (i > 0 && times[i] < times[i - 1])) return false;
            }
            var copiedTimes = new float[times.Length];
            var copiedCounts = new int[counts.Length];
            for (var i = 0; i < times.Length; i++)
            {
                copiedTimes[i] = times[i];
                copiedCounts[i] = counts[i];
            }
            burstTimes = copiedTimes;
            burstCounts = copiedCounts;
            return true;
        }

        static bool FiniteNonNegative(float value)
        {
            return value >= 0 && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal void Initialize(int particleCount)
        {
            _particleCount = particleCount;
            _playback = FluxParticleEmissionScheduler.Create(startDelay);
            ResetPlayback();
        }
        internal void UpdateEmission(float deltaTime)
        {
            SpawnStart = _spawnCursor;
            SpawnSeedStart = _spawnSeed;
            SpawnCount = 0;

            if (_playback == null || _particleCount < 1) return;

            var requested = FluxParticleEmissionScheduler.Emit(_playback, deltaTime, rate,
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
            if (position >= divisor)
            {
                position -= divisor;
            }
            return (int)position;
        }

        internal void ResetPlayback()
        {
            if (_playback == null) return;
            _duration = duration;
            _loop = loop;
            _burstTimes = burstTimes;
            _burstCounts = burstCounts;
            FluxParticleEmissionScheduler.Reset(_playback, startDelay);
            SpawnStart = _spawnCursor;
            SpawnSeedStart = _spawnSeed;
            SpawnCount = 0;
        }
    }
}
