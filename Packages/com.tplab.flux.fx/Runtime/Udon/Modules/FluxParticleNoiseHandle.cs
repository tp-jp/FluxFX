using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Noiseの操作ハンドル
    /// </summary>
    public class FluxParticleNoiseHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// Noiseのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleNoiseHandle Get(FluxParticleModules modules)
        {
            const string id = "tplab.fluxfx.noise";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleNoiseHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleNoiseHandle)new DataList();
#else
            var handle = new FluxParticleNoiseHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleNoiseHandleExtensions
    {
        /// <summary>
        /// 強さを取得します。
        /// </summary>
        public static float GetStrength(this FluxParticleNoiseHandle handle) => handle.GetValue((int)FluxParticleNoiseParameter.Strength).x;

        /// <summary>
        /// 強さを変更します。
        /// </summary>
        public static bool SetStrength(this FluxParticleNoiseHandle handle, float value) => handle.SetValue((int)FluxParticleNoiseParameter.Strength, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// スケールを取得します。
        /// </summary>
        public static float GetScale(this FluxParticleNoiseHandle handle) => handle.GetValue((int)FluxParticleNoiseParameter.Scale).x;

        /// <summary>
        /// スケールを変更します。
        /// </summary>
        public static bool SetScale(this FluxParticleNoiseHandle handle, float value) => handle.SetValue((int)FluxParticleNoiseParameter.Scale, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 変化速度を取得します。
        /// </summary>
        public static float GetSpeed(this FluxParticleNoiseHandle handle) => handle.GetValue((int)FluxParticleNoiseParameter.Speed).x;

        /// <summary>
        /// 変化速度を変更します。
        /// </summary>
        public static bool SetSpeed(this FluxParticleNoiseHandle handle, float value) => handle.SetValue((int)FluxParticleNoiseParameter.Speed, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}

