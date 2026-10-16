using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

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
#if COMPILER_UDONSHARP
            return (FluxParticleDragHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.Drag);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleDragHandle>(modules, FluxParticleModuleIds.Drag);
#endif
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

