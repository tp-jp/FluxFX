using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Modules
{
    /// <summary>
    /// Module設定の型とContributionの引数型を統一します。
    /// </summary>
    /// <typeparam name="TModule">対象Module設定の型</typeparam>
    public abstract class FluxParticleModuleDefinition<TModule> : IFluxParticleModuleDefinition
        where TModule : FluxParticleModule
    {
        /// <summary>
        /// 対象Moduleの型
        /// </summary>
        public Type ModuleType => typeof(TModule);

        /// <summary>
        /// Module種別を一意に識別するID
        /// </summary>
        public abstract string ModuleId { get; }

        void IFluxParticleModuleDefinition.Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!(module is TModule typedModule))
            {
                throw new ArgumentException($"Module Definition {GetType().FullName} requires {typeof(TModule).FullName}, but received {module.GetType().FullName}.", nameof(module));
            }

            Contribute(typedModule, context);
        }

        /// <summary>
        /// 型付きModule設定からCompilationに必要な定義を登録します。
        /// </summary>
        /// <param name="module">登録対象のModule設定</param>
        /// <param name="context">論理定義とParameter初期値の登録先</param>
        public abstract void Contribute(TModule module, FluxParticleProgramContribution context);
    }
}
