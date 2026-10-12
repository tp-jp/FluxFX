using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// 指定したModule型に対応するGPU Definitionの基底クラスです。
    /// パラメータ収集とOperation生成を型安全なメソッドへ委譲します。
    /// </summary>
    /// <typeparam name="TModule">Definitionが処理するParticle Moduleの型。</typeparam>
    public abstract class FluxParticleGpuModuleDefinition<TModule> : IFluxParticleGpuModuleDefinition
        where TModule : FluxParticleModule
    {
        /// <summary>
        /// このDefinitionに対応するModule型です。
        /// </summary>
        public Type ModuleType => typeof(TModule);

        /// <summary>
        /// Moduleの識別子です。
        /// </summary>
        public abstract string ModuleId { get; }

        /// <summary>
        /// ModuleのHLSLファイルパスです。
        /// </summary>
        public abstract string HlslPath { get; }

        /// <summary>
        /// 現行Shader Generatorに渡すHLSL関数名です。
        /// </summary>
        public abstract string EntryPoint { get; }

        /// <summary>
        /// 同一実行位置における処理の優先順位です。
        /// </summary>
        public abstract int Order { get; }

        void IFluxParticleGpuModuleDefinition.CollectParameters(
            FluxParticleModule module,
            FluxParticleParameterCollector collector)
        {
            CollectParameters((TModule)module, collector);
        }

        IEnumerable<FluxParticleOperation> IFluxParticleGpuModuleDefinition.CreateOperations(
            FluxParticleModule module)
        {
            return CreateOperations((TModule)module);
        }

        /// <summary>
        /// Module固有のGPUパラメータを収集します。
        /// </summary>
        /// <param name="module">設定値の取得元となるModule。</param>
        /// <param name="collector">GPUパラメータの登録先。</param>
        public virtual void CollectParameters(TModule module, FluxParticleParameterCollector collector)
        {
        }

        /// <summary>
        /// Module固有のOperationを生成します。
        /// 未移行のDefinitionでは例外を発生させ、不完全なExecutionPlanの生成を防ぎます。
        /// </summary>
        /// <param name="module">Operationの生成元となるModule。</param>
        /// <returns>生成されたOperation一覧。</returns>
        /// <exception cref="NotSupportedException">
        /// DefinitionがOperation生成に未対応の場合に発生します。
        /// </exception>
        public virtual IEnumerable<FluxParticleOperation> CreateOperations(TModule module)
        {
            throw new NotSupportedException(
                $"Operation generation is not implemented for module: {ModuleId}");
        }
    }
}