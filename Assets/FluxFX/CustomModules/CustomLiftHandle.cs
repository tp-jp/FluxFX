using TpLab.Flux.FX.Udon.Parameters;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.CustomModules
{
    /// <summary>
    /// Custom Liftの操作ハンドル
    /// </summary>
    public class CustomLiftHandle : FluxParticleModuleHandle
    {
        internal const int StrengthParameterIndex = 0;
        internal const int DirectionParameterIndex = 1;
    }

    public static class CustomLiftHandleExtensions
    {
        /// <summary>
        /// Custom Liftのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static CustomLiftHandle CustomLift(this FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (CustomLiftHandle)FluxParticleModuleHandleFactory.Get(modules, CustomLiftIds.Module);
#else
            return FluxParticleModuleHandleFactory.Get<CustomLiftHandle>(modules, CustomLiftIds.Module);
#endif
        }

        /// <summary>
        /// 加速度の強さを取得します。
        /// </summary>
        public static float GetStrength(this CustomLiftHandle handle) => handle.GetValue(CustomLiftHandle.StrengthParameterIndex).x;

        /// <summary>
        /// 加速度の強さを変更します。
        /// </summary>
        public static bool SetStrength(this CustomLiftHandle handle, float strength) => handle.SetValue(CustomLiftHandle.StrengthParameterIndex, FluxParticleValueType.Float, new Vector4(strength, 0, 0, 0));

        /// <summary>
        /// 加速度の方向を取得します。
        /// </summary>
        public static Vector3 GetDirection(this CustomLiftHandle handle) => handle.GetValue(CustomLiftHandle.DirectionParameterIndex);

        /// <summary>
        /// 加速度の方向を変更します。
        /// </summary>
        public static bool SetDirection(this CustomLiftHandle handle, Vector3 direction) => handle.SetValue(CustomLiftHandle.DirectionParameterIndex, FluxParticleValueType.Float3, direction);
    }
}
