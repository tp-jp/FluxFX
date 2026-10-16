using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// TextureSheetAnimationの操作ハンドル
    /// </summary>
    public class FluxParticleTextureSheetAnimationHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// TextureSheetAnimationのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleTextureSheetAnimationHandle Get(FluxParticleModules modules)
        {
            const string id = "tplab.fluxfx.texture-sheet-animation";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleTextureSheetAnimationHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleTextureSheetAnimationHandle)new DataList();
#else
            var handle = new FluxParticleTextureSheetAnimationHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleTextureSheetAnimationHandleExtensions
    {
        /// <summary>
        /// タイル数を取得します。
        /// </summary>
        public static Vector2 GetTiles(this FluxParticleTextureSheetAnimationHandle handle) => handle.GetValue((int)FluxParticleTextureSheetAnimationParameter.Tiles);

        /// <summary>
        /// タイル数を変更します。シミュレーション開始後は変更できません。
        /// </summary>
        public static bool SetTiles(this FluxParticleTextureSheetAnimationHandle handle, int columns, int rows)
        {
            var value = new Vector4(columns, rows, 0, 0);
            if ((double)value.x != columns || (double)value.y != rows) return false;
            return handle.SetValue((int)FluxParticleTextureSheetAnimationParameter.Tiles, FluxParticleValueType.Float2, value);
        }

        /// <summary>
        /// 周回数を取得します。
        /// </summary>
        public static float GetCycles(this FluxParticleTextureSheetAnimationHandle handle) => handle.GetValue((int)FluxParticleTextureSheetAnimationParameter.Cycles).x;

        /// <summary>
        /// 周回数を変更します。
        /// </summary>
        public static bool SetCycles(this FluxParticleTextureSheetAnimationHandle handle, float value) => handle.SetValue((int)FluxParticleTextureSheetAnimationParameter.Cycles, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}

