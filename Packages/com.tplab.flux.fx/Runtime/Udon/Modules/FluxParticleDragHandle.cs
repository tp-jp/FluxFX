using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Dragの操作ハンドル
    /// </summary>
    public class FluxParticleDragHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// Dragのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleDragHandle Get(FluxParticleModules modules)
        {
            const string id = "fluxfx.drag";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleDragHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleDragHandle)new DataList();
#else
            var handle = new FluxParticleDragHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleDragHandleExtensions
    {
        /// <summary>
        /// 抵抗を取得します。
        /// </summary>
        public static float GetDrag(this FluxParticleDragHandle handle) => handle.GetValue((int)FluxParticleDragParameter.Drag).x;

        /// <summary>
        /// 抵抗を変更します。
        /// </summary>
        public static bool SetDrag(this FluxParticleDragHandle handle, float value) => handle.SetValue((int)FluxParticleDragParameter.Drag, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}

