using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Rendering;

namespace TpLab.Flux.FX.Editor.Execution
{
    /// <summary>
    /// GPU SimulationとRenderingのModule DefinitionからOperationを収集し、
    /// 単一のExecutionPlanを構築します。
    /// 旧ExecutionStageから実行先を変換することはありません。
    /// </summary>
    public sealed class FluxParticleOperationCompiler
    {
        readonly FluxParticleGpuModuleRegistry _gpuRegistry;
        readonly FluxParticleRenderModuleRegistry _renderRegistry;

        /// <summary>
        /// GPU Module Registryと標準のRendering Module Registryを使用するCompilerを生成します。
        /// </summary>
        /// <param name="gpuRegistry">GPU Module型からDefinitionを解決するRegistry。</param>
        public FluxParticleOperationCompiler(FluxParticleGpuModuleRegistry gpuRegistry)
            : this(gpuRegistry, new FluxParticleRenderModuleRegistry())
        {
        }

        /// <summary>
        /// GPUとRenderingの両Registryを指定してCompilerを生成します。
        /// </summary>
        /// <param name="gpuRegistry">GPU Simulation用Definitionを管理するRegistry。</param>
        /// <param name="renderRegistry">Rendering用Definitionを管理するRegistry。</param>
        /// <exception cref="ArgumentNullException">
        /// いずれかのRegistryがnullの場合に発生します。
        /// </exception>
        public FluxParticleOperationCompiler(
            FluxParticleGpuModuleRegistry gpuRegistry,
            FluxParticleRenderModuleRegistry renderRegistry)
        {
            _gpuRegistry = gpuRegistry ?? throw new ArgumentNullException(nameof(gpuRegistry));
            _renderRegistry = renderRegistry ?? throw new ArgumentNullException(nameof(renderRegistry));
        }

        /// <summary>
        /// CompilePlanに登録されたModuleからSimulationとRenderingのOperationを生成します。
        /// ModuleのEnabled状態にかかわらずOperationを保持します。
        /// Definitionの不足、登録先の重複、不正なOperationを検出した場合は失敗します。
        /// </summary>
        /// <param name="plan">Operation生成対象のCompilePlan。</param>
        /// <returns>両実行系のOperationを統合したExecutionPlan。</returns>
        /// <exception cref="ArgumentNullException">
        /// CompilePlanがnullの場合に発生します。
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Definitionの登録やOperationの構成に不整合がある場合に発生します。
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// 対応DefinitionがOperation生成を実装していない場合に発生します。
        /// </exception>
        public FluxParticleExecutionPlan Compile(FluxParticleCompilePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var operations = new List<FluxParticleOperation>();

            foreach (var module in plan.Modules)
            {
                if (module == null)
                {
                    throw new InvalidOperationException("CompilePlan contains a null Module.");
                }

                var moduleType = module.GetType();
                var hasGpuDefinition = _gpuRegistry.TryGet(moduleType, out var gpuDefinition);
                var hasRenderDefinition = _renderRegistry.TryGet(moduleType, out var renderDefinition);

                if (hasGpuDefinition && hasRenderDefinition)
                {
                    throw new InvalidOperationException(
                        $"Module has both GPU and Render Definitions: {moduleType.FullName}");
                }

                if (!hasGpuDefinition && !hasRenderDefinition)
                {
                    throw new InvalidOperationException(
                        $"Module Definition is not registered: {moduleType.FullName}");
                }

                var moduleId = hasGpuDefinition ? gpuDefinition.ModuleId : renderDefinition.ModuleId;
                var moduleOperations = hasGpuDefinition
                    ? gpuDefinition.CreateOperations(module)
                    : renderDefinition.CreateOperations(module);

                if (moduleOperations == null)
                {
                    throw new InvalidOperationException(
                        $"Module Definition returned null Operations: {moduleId}");
                }

                foreach (var operation in moduleOperations)
                {
                    if (operation == null)
                    {
                        throw new InvalidOperationException(
                            $"Module Definition returned a null Operation: {moduleId}");
                    }

                    if (operation.ModuleId != moduleId)
                    {
                        throw new InvalidOperationException(
                            $"Operation Module ID does not match Definition: {moduleId}/{operation.OperationId}");
                    }

                    if (hasRenderDefinition &&
                        (operation.Target != FluxParticleOperationTarget.Rendering ||
                         operation.Phase != FluxParticleOperationPhase.Render))
                    {
                        throw new InvalidOperationException(
                            $"Render Definition returned a non-render Operation: {moduleId}/{operation.OperationId}");
                    }

                    operations.Add(operation);
                }
            }

            return FluxParticleExecutionPlan.Create(operations);
        }
    }
}