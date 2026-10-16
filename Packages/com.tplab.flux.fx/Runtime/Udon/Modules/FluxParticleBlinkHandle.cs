using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Blinkの操作ハンドル
    /// </summary>
    public class FluxParticleBlinkHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// Blinkのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleBlinkHandle Get(FluxParticleModules modules)
        {
            const string id = "tplab.fluxfx.blink";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleBlinkHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleBlinkHandle)new DataList();
#else
            var handle = new FluxParticleBlinkHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleBlinkHandleExtensions
    {
        /// <summary>
        /// 周波数を取得します。
        /// </summary>
        public static float GetFrequency(this FluxParticleBlinkHandle handle) => handle.GetValue((int)FluxParticleBlinkParameter.Frequency).x;

        /// <summary>
        /// 周波数を変更します。
        /// </summary>
        public static bool SetFrequency(this FluxParticleBlinkHandle handle, float value) => handle.SetValue((int)FluxParticleBlinkParameter.Frequency, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 点灯時間の割合を取得します。
        /// </summary>
        public static float GetDuty(this FluxParticleBlinkHandle handle) => handle.GetValue((int)FluxParticleBlinkParameter.Duty).x;

        /// <summary>
        /// 点灯時間の割合を変更します。
        /// </summary>
        public static bool SetDuty(this FluxParticleBlinkHandle handle, float value) => handle.SetValue((int)FluxParticleBlinkParameter.Duty, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 消灯時の倍率を取得します。
        /// </summary>
        public static float GetMinimum(this FluxParticleBlinkHandle handle) => handle.GetValue((int)FluxParticleBlinkParameter.Minimum).x;

        /// <summary>
        /// 消灯時の倍率を変更します。
        /// </summary>
        public static bool SetMinimum(this FluxParticleBlinkHandle handle, float value) => handle.SetValue((int)FluxParticleBlinkParameter.Minimum, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 切り替わりの滑らかさを取得します。
        /// </summary>
        public static float GetSmoothness(this FluxParticleBlinkHandle handle) => handle.GetValue((int)FluxParticleBlinkParameter.Smoothness).x;

        /// <summary>
        /// 切り替わりの滑らかさを変更します。
        /// </summary>
        public static bool SetSmoothness(this FluxParticleBlinkHandle handle, float value) => handle.SetValue((int)FluxParticleBlinkParameter.Smoothness, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 位相のランダム化量を取得します。
        /// </summary>
        public static float GetRandomPhase(this FluxParticleBlinkHandle handle) => handle.GetValue((int)FluxParticleBlinkParameter.RandomPhase).x;

        /// <summary>
        /// 位相のランダム化量を変更します。
        /// </summary>
        public static bool SetRandomPhase(this FluxParticleBlinkHandle handle, float value) => handle.SetValue((int)FluxParticleBlinkParameter.RandomPhase, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 時間の基準を取得します。
        /// </summary>
        public static FluxParticleBlinkTimeMode GetTimeMode(this FluxParticleBlinkHandle handle) => (FluxParticleBlinkTimeMode)(int)handle.GetValue((int)FluxParticleBlinkParameter.TimeMode).x;

        /// <summary>
        /// 時間の基準を変更します。
        /// </summary>
        public static bool SetTimeMode(this FluxParticleBlinkHandle handle, FluxParticleBlinkTimeMode value) => handle.SetValue((int)FluxParticleBlinkParameter.TimeMode, FluxParticleValueType.Float, new Vector4(System.Convert.ToSingle((int)value), 0f, 0f, 0f));

        /// <summary>
        /// 補正対象を取得します。
        /// </summary>
        public static FluxParticleBlinkTarget GetTarget(this FluxParticleBlinkHandle handle) => (FluxParticleBlinkTarget)(int)handle.GetValue((int)FluxParticleBlinkParameter.Target).x;

        /// <summary>
        /// 補正対象を変更します。
        /// </summary>
        public static bool SetTarget(this FluxParticleBlinkHandle handle, FluxParticleBlinkTarget value) => handle.SetValue((int)FluxParticleBlinkParameter.Target, FluxParticleValueType.Float, new Vector4(System.Convert.ToSingle((int)value), 0f, 0f, 0f));

    }
}
