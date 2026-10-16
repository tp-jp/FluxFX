using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// RotationOverLifetimeの操作ハンドル
    /// </summary>
    public class FluxParticleRotationOverLifetimeHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// RotationOverLifetimeのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleRotationOverLifetimeHandle Get(FluxParticleModules modules)
        {
            const string id = "tplab.fluxfx.rotation-over-lifetime";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleRotationOverLifetimeHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleRotationOverLifetimeHandle)new DataList();
#else
            var handle = new FluxParticleRotationOverLifetimeHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleRotationOverLifetimeHandleExtensions
    {
        /// <summary>
        /// 終端回転を取得します。
        /// </summary>
        public static Vector3 GetEndRotation(this FluxParticleRotationOverLifetimeHandle handle) => handle.GetValue((int)FluxParticleRotationOverLifetimeParameter.EndRotation);

        /// <summary>
        /// 終端回転を変更します。
        /// </summary>
        public static bool SetEndRotation(this FluxParticleRotationOverLifetimeHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleRotationOverLifetimeParameter.EndRotation, FluxParticleValueType.Float3, value);
    }
}

