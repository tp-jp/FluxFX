using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Moduleハンドルを初回だけ初期化し、System単位で再利用します。
    /// </summary>
    /// <remarks>
    /// カスタムModule作者向けの取得APIです。Moduleごとの取得用拡張メソッドから呼び出します。
    /// 同じModule IDには常に同じハンドル型を対応させてください。IDは契約と共有します。
    /// 通常のC#ではGet&lt;T&gt;を使用し、COMPILER_UDONSHARPではGetの戻り値をハンドル型へ変換します。
    /// UdonではDataListをハンドルとして扱うため、派生クラスのコンストラクターやインスタンスフィールドに初期化処理を持たせないでください。
    /// Bindingの取得・初期化・キャッシュ登録はFactoryが行います。呼び出し側でDataListを組み立てる必要はありません。
    /// </remarks>
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
