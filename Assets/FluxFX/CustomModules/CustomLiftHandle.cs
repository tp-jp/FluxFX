using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.CustomModules
{
    /// <summary>
    /// Custom Liftの操作ハンドル
    /// </summary>
    public class CustomLiftHandle : FluxParticleModuleHandle { }

    public static class CustomLiftHandleExtensions
    {
        /// <summary>
        /// Custom Liftのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static CustomLiftHandle CustomLift(this FluxParticleModules modules)
        {
            const string id = "custom.fluxfx.lift";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (CustomLiftHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (CustomLiftHandle)new DataList();
#else
            var handle = new CustomLiftHandle();
#endif
            FluxParticleModuleHandle.Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }

        /// <summary>
        /// 加速度の強さを取得します。
        /// </summary>
        public static float GetStrength(this CustomLiftHandle handle) => handle.GetValue(0).x;

        /// <summary>
        /// 加速度の強さを変更します。
        /// </summary>
        public static bool SetStrength(this CustomLiftHandle handle, float strength) => handle.SetValue(0, 1, new Vector4(strength, 0, 0, 0));

        /// <summary>
        /// 加速度の方向を取得します。
        /// </summary>
        public static Vector3 GetDirection(this CustomLiftHandle handle) => handle.GetValue(1);

        /// <summary>
        /// 加速度の方向を変更します。
        /// </summary>
        public static bool SetDirection(this CustomLiftHandle handle, Vector3 direction) => handle.SetValue(1, 3, direction);
    }
}
