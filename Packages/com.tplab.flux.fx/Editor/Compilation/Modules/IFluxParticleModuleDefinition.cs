using System;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// GPUとRenderingで共用するModule定義の契約です。
    /// </summary>
    public interface IFluxParticleModuleDefinition
    {
        /// <summary>
        /// このDefinitionが対象とするModuleの型です。
        /// </summary>
        Type ModuleType { get; }

        /// <summary>
        /// Module種別を一意に識別するIDです。
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// Module設定を読み取り、Compilationに必要な定義を登録します。
        /// </summary>
        /// <param name="module">登録対象のModule設定。</param>
        /// <param name="context">論理定義とParameter初期値の登録先。</param>
        void Contribute(FluxParticleModule module, FluxParticleProgramContribution context);
    }
}
