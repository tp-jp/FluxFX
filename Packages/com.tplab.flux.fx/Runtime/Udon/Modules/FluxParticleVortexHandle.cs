using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Vortexの操作ハンドル
    /// </summary>
    public class FluxParticleVortexHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// Vortexのハンドルを取得し、初回のみ生成します。
        /// </summary>
        internal static FluxParticleVortexHandle Get(FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (FluxParticleVortexHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.Vortex);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleVortexHandle>(modules, FluxParticleModuleIds.Vortex);
#endif
        }
    }

    public static class FluxParticleVortexHandleExtensions
    {
        /// <summary>
        /// 中心を取得します。
        /// </summary>
        public static Vector3 GetCenter(this FluxParticleVortexHandle handle) => handle.GetValue((int)FluxParticleVortexParameter.Center);

        /// <summary>
        /// 中心を変更します。
        /// </summary>
        public static bool SetCenter(this FluxParticleVortexHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleVortexParameter.Center, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 回転軸を取得します。
        /// </summary>
        public static Vector3 GetAxis(this FluxParticleVortexHandle handle) => handle.GetValue((int)FluxParticleVortexParameter.Axis);

        /// <summary>
        /// 回転軸を変更します。
        /// </summary>
        public static bool SetAxis(this FluxParticleVortexHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleVortexParameter.Axis, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 強さを取得します。
        /// </summary>
        public static float GetStrength(this FluxParticleVortexHandle handle) => handle.GetValue((int)FluxParticleVortexParameter.Strength).x;

        /// <summary>
        /// 強さを変更します。
        /// </summary>
        public static bool SetStrength(this FluxParticleVortexHandle handle, float value) => handle.SetValue((int)FluxParticleVortexParameter.Strength, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}

