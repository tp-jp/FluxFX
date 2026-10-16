using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Moduleハンドルを初回だけ初期化し、System単位で再利用します。
    /// </summary>
    public static class FluxParticleModuleHandleFactory
    {
        /// <summary>
        /// 組み込み済みModuleのハンドルを取得します。
        /// </summary>
        /// <returns>キャッシュ済みまたは初期化済みのハンドル。未組み込みの場合はnull</returns>
#if COMPILER_UDONSHARP
        public static DataList Get(FluxParticleModules modules, string moduleId)
#else
        public static T Get<T>(FluxParticleModules modules, string moduleId) where T : FluxParticleModuleHandle, new()
#endif
        {
            if (modules == null) return null;
            var cached = modules.GetCachedHandle(moduleId);
            if (cached != null)
            {
#if COMPILER_UDONSHARP
                return cached;
#else
                return (T)cached;
#endif
            }
            var binding = modules.GetBinding(moduleId);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            // UdonではDataListをそのままハンドルとして使用します。
            var handle = binding;
#else
            var handle = new T();
            FluxParticleModuleHandle.Initialize(handle, binding);
#endif
            modules.CacheHandle(moduleId, handle);
            return handle;
        }
    }
}
