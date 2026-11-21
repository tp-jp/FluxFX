using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// 固定IDで取得する音声チャンネルの設定ハンドル。GPU出力は読み戻しません。
    /// </summary>
    public class FluxParticleAudioChannelHandle : FluxParticleParameterHandle
    {
    }

    /// <summary>
    /// チャンネルの取得と薄い型付きAPI。入力検証はExecutorへ委譲します。
    /// </summary>
    public static class FluxParticleAudioChannelHandleExtensions
    {
        /// <summary>
        /// 固定IDに対応する設定ハンドルを取得します。未組み込み・不明IDはnull。
        /// 構成が変わった場合は取得し直してください。
        /// </summary>
        /// <param name="modules">対象SystemのModules</param>
        /// <param name="id">Inspectorに表示されるチャンネルの固定ID</param>
        /// <returns>同じSystemとIDで再利用するハンドル</returns>
        public static FluxParticleAudioChannelHandle GetAudioChannel(this FluxParticleModules modules, string id)
        {
            if (modules == null || string.IsNullOrEmpty(id) || modules.Executor == null) return null;
            var prefix = "audio-input.channel." + id;
            var cached = modules.GetCachedHandle(prefix);
            if (cached != null) return (FluxParticleAudioChannelHandle)cached;
            var executor = modules.Executor;
            var slots = new int[4];
            slots[(int)FluxParticleAudioChannelParameter.Source] = executor.ResolveParameter(prefix + ".source", (int)FluxParticleValueType.Float);
            slots[(int)FluxParticleAudioChannelParameter.Gain] = executor.ResolveParameter(prefix + ".gain", (int)FluxParticleValueType.Float);
            slots[(int)FluxParticleAudioChannelParameter.Attack] = executor.ResolveParameter(prefix + ".attack", (int)FluxParticleValueType.Float);
            slots[(int)FluxParticleAudioChannelParameter.Release] = executor.ResolveParameter(prefix + ".release", (int)FluxParticleValueType.Float);
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] < 0) return null;
            }
#if COMPILER_UDONSHARP
            var handle = new DataList();
#else
            var handle = new FluxParticleAudioChannelHandle();
#endif
            FluxParticleParameterHandle.InitializeSlots(handle, executor, null, slots);
            modules.CacheHandle(prefix, handle);
            return (FluxParticleAudioChannelHandle)handle;
        }

        /// <summary>
        /// 入力元を取得します。
        /// </summary>
        public static FluxParticleAudioSource GetSource(this FluxParticleAudioChannelHandle handle) =>
            (FluxParticleAudioSource)(int)handle.GetValue((int)FluxParticleAudioChannelParameter.Source).x;

        /// <summary>
        /// 入力元を変更します。
        /// </summary>
        public static bool SetSource(this FluxParticleAudioChannelHandle handle, FluxParticleAudioSource value) =>
            handle.SetValue((int)FluxParticleAudioChannelParameter.Source, FluxParticleValueType.Float,
                new Vector4(System.Convert.ToSingle((int)value), 0, 0, 0));

        /// <summary>
        /// 追加感度を取得します。
        /// </summary>
        public static float GetGain(this FluxParticleAudioChannelHandle handle) => handle.GetValue((int)FluxParticleAudioChannelParameter.Gain).x;

        /// <summary>
        /// 追加感度を変更します。
        /// </summary>
        public static bool SetGain(this FluxParticleAudioChannelHandle handle, float value) =>
            handle.SetValue((int)FluxParticleAudioChannelParameter.Gain, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 立ち上がり時間（秒）を取得します。
        /// </summary>
        public static float GetAttack(this FluxParticleAudioChannelHandle handle) => handle.GetValue((int)FluxParticleAudioChannelParameter.Attack).x;

        /// <summary>
        /// 立ち上がり時間（秒）を変更します。
        /// </summary>
        public static bool SetAttack(this FluxParticleAudioChannelHandle handle, float value) =>
            handle.SetValue((int)FluxParticleAudioChannelParameter.Attack, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 減衰時間（秒）を取得します。
        /// </summary>
        public static float GetRelease(this FluxParticleAudioChannelHandle handle) => handle.GetValue((int)FluxParticleAudioChannelParameter.Release).x;

        /// <summary>
        /// 減衰時間（秒）を変更します。
        /// </summary>
        public static bool SetRelease(this FluxParticleAudioChannelHandle handle, float value) =>
            handle.SetValue((int)FluxParticleAudioChannelParameter.Release, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}
