using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// GPU Moduleの識別情報、Shader実装、パラメータ、およびOperation定義を提供します。
    /// Operationの実行先は旧ExecutionStageから推論せず、Definition側で直接宣言します。
    /// </summary>
    public interface IFluxParticleGpuModuleDefinition
    {
        /// <summary>
        /// このDefinitionが扱うModuleの型です。
        /// </summary>
        Type ModuleType { get; }

        /// <summary>
        /// GPU Moduleを識別する一意のIDです。
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// GPU処理を定義するHLSLファイルのパスです。
        /// </summary>
        string HlslPath { get; }

        /// <summary>
        /// GPU処理の呼び出しに使用するHLSL関数名です。
        /// 現行Shader GeneratorではModuleごとに単一のEntryPointを扱います。
        /// </summary>
        string EntryPoint { get; }

        /// <summary>
        /// 同一実行位置に属するModuleの実行優先順位です。
        /// </summary>
        int Order { get; }

        /// <summary>
        /// ModuleのGPUパラメータを収集します。
        /// </summary>
        /// <param name="module">パラメータの取得元となるModule。</param>
        /// <param name="collector">パラメータの登録先。</param>
        void CollectParameters(FluxParticleModule module, FluxParticleParameterCollector collector);

        /// <summary>
        /// Moduleの設定から実行Operationを生成します。
        /// 一つのModuleが複数のOperationを返すことを許可します。
        /// </summary>
        /// <param name="module">Operationの生成元となるModule。</param>
        /// <returns>Moduleが所有するOperation一覧。</returns>
        IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleModule module);
    }
}