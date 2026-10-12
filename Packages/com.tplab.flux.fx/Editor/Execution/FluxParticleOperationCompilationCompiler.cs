using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Rendering;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Execution
{
    /// <summary>
    /// Module DefinitionからExecutionPlanとShader実装情報を収集し、
    /// Operation単位のCompilationを構築します。
    /// SimulationとRenderingのOperationは共通のExecutionPlanで管理しますが、
    /// GPU Shader実装とUniformの収集はSimulation Moduleのみを対象とします。
    /// </summary>
    public sealed class FluxParticleOperationCompilationCompiler
    {
        readonly FluxParticleGpuModuleRegistry _gpuRegistry;
        readonly FluxParticleOperationCompiler _operationCompiler;

        /// <summary>
        /// 標準のRendering Registryを使用するCompilation Compilerを生成します。
        /// </summary>
        /// <param name="gpuRegistry">
        /// GPU Simulation用のModule Definitionを管理するRegistry。
        /// </param>
        public FluxParticleOperationCompilationCompiler(FluxParticleGpuModuleRegistry gpuRegistry)
            : this(gpuRegistry, new FluxParticleRenderModuleRegistry())
        {
        }

        /// <summary>
        /// GPUとRenderingのRegistryを指定してCompilation Compilerを生成します。
        /// </summary>
        /// <param name="gpuRegistry">
        /// GPU Simulation用Definitionを管理するRegistry。
        /// </param>
        /// <param name="renderRegistry">
        /// Rendering用Definitionを管理するRegistry。
        /// </param>
        public FluxParticleOperationCompilationCompiler(
            FluxParticleGpuModuleRegistry gpuRegistry,
            FluxParticleRenderModuleRegistry renderRegistry)
        {
            if (gpuRegistry == null) throw new ArgumentNullException(nameof(gpuRegistry));
            if (renderRegistry == null) throw new ArgumentNullException(nameof(renderRegistry));

            _gpuRegistry = gpuRegistry;
            _operationCompiler = new FluxParticleOperationCompiler(gpuRegistry, renderRegistry);
        }

        /// <summary>
        /// CompilePlanからOperationCompilationを構築します。
        /// Operationの検証にはExecutionPlanを使用し、Simulation Moduleについては
        /// パラメータ収集とShader実装への対応付けを行います。
        /// </summary>
        /// <param name="plan">
        /// Compilationの生成対象となるCompilePlan。
        /// </param>
        /// <returns>
        /// Simulation PassとRendering Operationを含むCompilation。
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// CompilePlanがnullの場合に発生します。
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Moduleの識別情報、Operation、またはパラメータ収集に不整合がある場合に発生します。
        /// </exception>
        public FluxParticleOperationCompilation Compile(FluxParticleCompilePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var executionPlan = _operationCompiler.Compile(plan);
            var compiledOperations = new List<FluxParticleCompiledOperation>();
            var moduleIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var module in plan.Modules)
            {
                if (!_gpuRegistry.TryGet(module.GetType(), out var definition)) continue;

                if (!moduleIds.Add(definition.ModuleId))
                {
                    throw new InvalidOperationException($"Duplicate GPU Module in Compilation: {definition.ModuleId}");
                }

                var moduleCompilation = CompileModule(module, definition);

                foreach (var operation in executionPlan.Operations)
                {
                    if (operation.ModuleId != definition.ModuleId) continue;

                    if (operation.Target == FluxParticleOperationTarget.Rendering)
                    {
                        throw new InvalidOperationException($"GPU Definition cannot produce Rendering Operation: {definition.ModuleId}/{operation.OperationId}");
                    }

                    compiledOperations.Add(new FluxParticleCompiledOperation(
                        operation,
                        moduleCompilation,
                        definition.HlslPath,
                        definition.EntryPoint));
                }
            }

            return FluxParticleOperationCompilation.Create(executionPlan, compiledOperations);
        }

        /// <summary>
        /// GPU ModuleのUniformを収集し、複数Operationから共有できるModuleCompilationを生成します。
        /// Enabled Uniformには既存GPU Compilerと同じ命名規則を使用します。
        /// </summary>
        static FluxParticleCompiledModule CompileModule(
            FluxParticleModule module,
            IFluxParticleGpuModuleDefinition definition)
        {
            var collector = new FluxParticleParameterCollector();
            definition.CollectParameters(module, collector);

            var parameters = new List<FluxParticleGpuParameterCompilation>();

            foreach (var parameter in collector.Parameters)
            {
                parameters.Add(new FluxParticleGpuParameterCompilation(parameter));
            }

            var enabledParameterName = FluxParticleGpuModuleEnabled.GetParameterName(definition.ModuleId);

            return new FluxParticleCompiledModule(
                definition.ModuleId,
                definition.ModuleType,
                parameters,
                enabledParameterName);
        }
    }
}