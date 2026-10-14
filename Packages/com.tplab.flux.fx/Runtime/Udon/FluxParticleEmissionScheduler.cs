using System;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// RuntimeとEditor Previewで共通のEmission時刻と生成要求数を計算します。
    /// </summary>
    public static class FluxParticleEmissionScheduler
    {
        public const float MinimumDuration = 0.01f;

        const int Fraction = 0;
        const int CycleTime = 1;
        const int Delay = 2;
        const int BurstIndex = 3;
        const int Completed = 4;

        /// <summary>
        /// 非LoopのEmissionが終了したかを返します。
        /// </summary>
        /// <param name="state">Scheduler専用の再生状態</param>
        /// <returns>Emissionが終了している場合はtrue</returns>
        public static bool HasCompleted(double[] state)
        {
            return state[Completed] != 0;
        }

        /// <summary>
        /// 再生位置を保持する状態を作成します。
        /// </summary>
        /// <param name="startDelay">生成開始までの待機秒数</param>
        /// <returns>Scheduler専用の再生状態</returns>
        public static double[] Create(float startDelay)
        {
            var state = new double[5];
            Reset(state, startDelay);
            return state;
        }

        /// <summary>
        /// Emissionの再生位置と端数を初期化します。
        /// </summary>
        /// <param name="state">Scheduler専用の再生状態</param>
        /// <param name="startDelay">生成開始までの待機秒数</param>
        public static void Reset(double[] state, float startDelay)
        {
            state[Fraction] = 0;
            state[CycleTime] = 0;
            state[Delay] = Math.Max(0, startDelay);
            state[BurstIndex] = 0;
            state[Completed] = 0;
        }

        /// <summary>
        /// 経過時間に含まれるRateとBurstの生成要求数を返します。
        /// </summary>
        /// <param name="state">Scheduler専用の再生状態</param>
        /// <param name="deltaTime">経過秒数</param>
        /// <param name="rate">1秒あたりの生成数</param>
        /// <param name="duration">1周期の秒数</param>
        /// <param name="loop">周期終了後に再び生成するか</param>
        /// <param name="burstTimes">昇順のBurst時刻</param>
        /// <param name="burstCounts">各Burstの生成数</param>
        /// <returns>容量制限前の生成要求数</returns>
        public static long Emit(double[] state, float deltaTime, float rate, float duration, bool loop,
            float[] burstTimes, int[] burstCounts)
        {
            if (state[Completed] != 0 || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) ||
                float.IsNaN(rate) || float.IsInfinity(rate)) return 0;

            var remaining = (double)deltaTime;
            var delayStep = Math.Min(state[Delay], remaining);
            state[Delay] -= delayStep;
            remaining -= delayStep;
            var cycleDuration = Math.Max(duration, MinimumDuration);
            long count = 0;
            while (remaining > 0)
            {
                var step = Math.Min(remaining, cycleDuration - state[CycleTime]);
                var previous = state[CycleTime];
                state[CycleTime] += step;
                remaining -= step;
                state[Fraction] += step * Math.Max(0, rate);
                var rateCount = (long)Math.Min(int.MaxValue, Math.Floor(state[Fraction]));
                state[Fraction] -= rateCount;
                count += rateCount;
                var burstIndex = (int)state[BurstIndex];
                while (burstIndex < burstTimes.Length && burstTimes[burstIndex] <= state[CycleTime])
                {
                    if (burstTimes[burstIndex] >= previous)
                    {
                        count += burstCounts[burstIndex];
                    }
                    burstIndex++;
                }
                state[BurstIndex] = burstIndex;
                if (state[CycleTime] < cycleDuration) break;
                if (!loop)
                {
                    state[Completed] = 1;
                    break;
                }
                state[CycleTime] = 0;
                state[BurstIndex] = 0;
            }
            return count;
        }
    }
}