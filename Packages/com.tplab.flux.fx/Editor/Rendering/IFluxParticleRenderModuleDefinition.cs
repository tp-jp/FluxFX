using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Rendering
{
    /// <summary>
    /// Rendering Moduleから描画用Operationを生成するための契約です。
    /// GPU Simulationとは独立し、HLSL EntryPointやGPU Kernelの定義を要求しません。
    /// </summary>
    public interface IFluxParticleRenderModuleDefinition
    {
        /// <summary>
        /// このDefinitionに対応するModule型です。
        /// </summary>
        Type ModuleType { get; }

        /// <summary>
        /// Moduleを識別する一意のIDです。
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// ModuleからRendering Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるModule。</param>
        /// <returns>生成されたOperation一覧。</returns>
        IEnumerable<FluxParticleOperation> CreateOperations(FluxParticleModule module);
    }

    /// <summary>
    /// 指定したRendering Module型に対応するDefinitionの基底クラスです。
    /// Moduleの型変換を共通化し、各DefinitionはOperationの宣言だけを担当します。
    /// </summary>
    /// <typeparam name="TModule">Definitionが処理するRendering Module型。</typeparam>
    public abstract class FluxParticleRenderModuleDefinition<TModule> : IFluxParticleRenderModuleDefinition
        where TModule : FluxParticleModule
    {
        /// <summary>
        /// このDefinitionに対応するModule型です。
        /// </summary>
        public Type ModuleType => typeof(TModule);

        /// <summary>
        /// Moduleを識別する一意のIDです。
        /// </summary>
        public abstract string ModuleId { get; }

        IEnumerable<FluxParticleOperation> IFluxParticleRenderModuleDefinition.CreateOperations(FluxParticleModule module)
        {
            return CreateOperations((TModule)module);
        }

        /// <summary>
        /// Module固有のRendering Operationを生成します。
        /// </summary>
        /// <param name="module">Operationの生成元となるRendering Module。</param>
        /// <returns>Moduleに属するRendering Operation一覧。</returns>
        public abstract IEnumerable<FluxParticleOperation> CreateOperations(TModule module);
    }
}