using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// Emitterが管理するRateとBurstの操作ハンドル
    /// </summary>
    /// <remarks>
    /// System.Emissionから取得し、nullでないことを確認して拡張メソッドで操作します。
    /// 生成スケジュールと入力検証はEmitterが管理します。
    /// newによる直接生成や、継承されたDataList操作での内容変更は行わないでください。
    /// Systemの置き換えやPlayの再開始後は取得し直し、破棄後は使用しないでください。
    /// </remarks>
    public class FluxParticleEmissionHandle : DataList
    {
        internal const int EmitterIndex = 0;

        internal static FluxParticleEmissionHandle Create(FluxParticleEmitter emitter)
        {
#if COMPILER_UDONSHARP
            var handle = (FluxParticleEmissionHandle)new DataList();
#else
            var handle = new FluxParticleEmissionHandle();
#endif
            handle.Add(new DataToken(emitter));
            return handle;
        }
    }

    public static class FluxParticleEmissionHandleExtensions
    {
        /// <summary>
        /// 1秒あたりの生成数を取得します。
        /// </summary>
        public static float GetRate(this FluxParticleEmissionHandle handle) => ((FluxParticleEmitter)handle[FluxParticleEmissionHandle.EmitterIndex].Reference).Rate;

        /// <summary>
        /// 1秒あたりの生成数を即時変更します。
        /// </summary>
        public static bool SetRate(this FluxParticleEmissionHandle handle, float value) => ((FluxParticleEmitter)handle[FluxParticleEmissionHandle.EmitterIndex].Reference).SetRate(value);

        /// <summary>
        /// Burst設定数を取得します。
        /// </summary>
        public static int GetBurstCount(this FluxParticleEmissionHandle handle) => ((FluxParticleEmitter)handle[FluxParticleEmissionHandle.EmitterIndex].Reference).BurstCount;

        /// <summary>
        /// 指定したBurstの時刻を取得します。
        /// </summary>
        public static float GetBurstTime(this FluxParticleEmissionHandle handle, int index) => ((FluxParticleEmitter)handle[FluxParticleEmissionHandle.EmitterIndex].Reference).GetBurstTime(index);

        /// <summary>
        /// 指定したBurstの生成数を取得します。
        /// </summary>
        public static int GetBurstParticleCount(this FluxParticleEmissionHandle handle, int index) => ((FluxParticleEmitter)handle[FluxParticleEmissionHandle.EmitterIndex].Reference).GetBurstCount(index);

        /// <summary>
        /// 昇順のBurst設定をコピーし、次回Restartから使用します。
        /// </summary>
        public static bool SetBursts(this FluxParticleEmissionHandle handle, float[] times, int[] counts) => ((FluxParticleEmitter)handle[FluxParticleEmissionHandle.EmitterIndex].Reference).SetBursts(times, counts);
    }
}

