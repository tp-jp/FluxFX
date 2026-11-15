using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// AudioInputの設定操作ハンドル。GPU出力のCPU読み戻しは行いません。
    /// </summary>
    public class FluxParticleAudioInputHandle : FluxParticleModuleHandle
    {
        internal static FluxParticleAudioInputHandle Get(FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (FluxParticleAudioInputHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.AudioInput);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleAudioInputHandle>(modules, FluxParticleModuleIds.AudioInput);
#endif
        }
    }

    /// <summary>
    /// AudioInputの型付き設定API。入力検証はExecutorで行います。
    /// </summary>
    public static class FluxParticleAudioInputHandleExtensions
    {
        /// <summary>
        /// 音域を取得します。
        /// </summary>
        public static FluxParticleAudioBand GetBand(this FluxParticleAudioInputHandle handle) => (FluxParticleAudioBand)(int)handle.GetValue((int)FluxParticleAudioInputParameter.Band).x;

        /// <summary>
        /// 音域を変更します。
        /// </summary>
        public static bool SetBand(this FluxParticleAudioInputHandle handle, FluxParticleAudioBand value) =>
            handle.SetValue((int)FluxParticleAudioInputParameter.Band, FluxParticleValueType.Float, new Vector4(System.Convert.ToSingle((int)value), 0, 0, 0));

        /// <summary>
        /// 入力感度を取得します。
        /// </summary>
        public static float GetGain(this FluxParticleAudioInputHandle handle) => handle.GetValue((int)FluxParticleAudioInputParameter.Gain).x;

        /// <summary>
        /// 入力感度を変更します。
        /// </summary>
        public static bool SetGain(this FluxParticleAudioInputHandle handle, float value) =>
            handle.SetValue((int)FluxParticleAudioInputParameter.Gain, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 無音閾値を取得します。
        /// </summary>
        public static float GetNoiseFloor(this FluxParticleAudioInputHandle handle) => handle.GetValue((int)FluxParticleAudioInputParameter.NoiseFloor).x;

        /// <summary>
        /// 無音閾値を変更します。
        /// </summary>
        public static bool SetNoiseFloor(this FluxParticleAudioInputHandle handle, float value) =>
            handle.SetValue((int)FluxParticleAudioInputParameter.NoiseFloor, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 反応量を取得します。
        /// </summary>
        public static float GetStrength(this FluxParticleAudioInputHandle handle) => handle.GetValue((int)FluxParticleAudioInputParameter.Strength).x;

        /// <summary>
        /// 反応量を変更します。
        /// </summary>
        public static bool SetStrength(this FluxParticleAudioInputHandle handle, float value) =>
            handle.SetValue((int)FluxParticleAudioInputParameter.Strength, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 立ち上がり時間（秒）を取得します。
        /// </summary>
        public static float GetAttack(this FluxParticleAudioInputHandle handle) => handle.GetValue((int)FluxParticleAudioInputParameter.Attack).x;

        /// <summary>
        /// 立ち上がり時間（秒）を変更します。
        /// </summary>
        public static bool SetAttack(this FluxParticleAudioInputHandle handle, float value) =>
            handle.SetValue((int)FluxParticleAudioInputParameter.Attack, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 減衰時間（秒）を取得します。
        /// </summary>
        public static float GetRelease(this FluxParticleAudioInputHandle handle) => handle.GetValue((int)FluxParticleAudioInputParameter.Release).x;

        /// <summary>
        /// 減衰時間（秒）を変更します。
        /// </summary>
        public static bool SetRelease(this FluxParticleAudioInputHandle handle, float value) =>
            handle.SetValue((int)FluxParticleAudioInputParameter.Release, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 遅い応答の追従時間（秒）を取得します。
        /// </summary>
        public static float GetSlowResponse(this FluxParticleAudioInputHandle handle) => handle.GetValue((int)FluxParticleAudioInputParameter.SlowResponse).x;

        /// <summary>
        /// 遅い応答の追従時間（秒）を変更します。
        /// </summary>
        public static bool SetSlowResponse(this FluxParticleAudioInputHandle handle, float value) =>
            handle.SetValue((int)FluxParticleAudioInputParameter.SlowResponse, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 全4音域平均の入力感度を取得します。
        /// </summary>
        public static float GetAverageGain(this FluxParticleAudioInputHandle handle) => handle.GetValue((int)FluxParticleAudioInputParameter.AverageGain).x;

        /// <summary>
        /// 全4音域平均の入力感度を変更します。
        /// </summary>
        public static bool SetAverageGain(this FluxParticleAudioInputHandle handle, float value) =>
            handle.SetValue((int)FluxParticleAudioInputParameter.AverageGain, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 入力更新の停止判定時間（秒）を取得します。
        /// </summary>
        public static float GetTimeout(this FluxParticleAudioInputHandle handle) => handle.GetValue((int)FluxParticleAudioInputParameter.Timeout).x;

        /// <summary>
        /// 入力更新の停止判定時間（秒）を変更します。
        /// </summary>
        public static bool SetTimeout(this FluxParticleAudioInputHandle handle, float value) =>
            handle.SetValue((int)FluxParticleAudioInputParameter.Timeout, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}
