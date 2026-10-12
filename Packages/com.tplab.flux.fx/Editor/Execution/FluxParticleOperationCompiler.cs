using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.GpuModules;

namespace TpLab.Flux.FX.Editor.Execution
{
    /// <summary>
    /// Particle Moduleに対応するGPU DefinitionからOperationを収集し、
    /// ExecutionPlanを構築します。
    /// 旧ExecutionStageからの変換は行わず、各Definitionが直接宣言したOperationを使用します。
    /// </summary>
    public sealed class FluxParticleOperationCompiler
    {
        readonly FluxParticleGpuModuleRegistry _registry;

        /// <summary>
        /// GPU Module Registryを使用するOperation Compilerを生成します。
        /// </summary>
        /// <param name="registry">Module型からDefinitionを解決するRegistry。</param>
        /// <exception cref="ArgumentNullException">
        /// Registryがnullの場合に発生します。
        /// </exception>
        public FluxParticleOperationCompiler(FluxParticleGpuModuleRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// CompilePlanに登録されたModuleのOperationを収集し、
        /// ExecutionPlanとして検証・並び替えます。
        /// Moduleの有効・無効にかかわらずOperationを保持します。
        /// </summary>
        /// <param name="plan">Operationの生成対象となるCompilePlan。</param>
        /// <returns>各DefinitionのOperationを統合したExecutionPlan。</returns>
        /// <exception cref="ArgumentNullException">
        /// CompilePlanがnullの場合に発生します。
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Definitionの登録漏れや、無効なOperation構成を検出した場合に発生します。
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// ModuleのDefinitionがOperation生成に未対応の場合に発生します。
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

                var definition = _registry.Get(module);
                var moduleOperations = definition.CreateOperations(module);

                if (moduleOperations == null)
                {
                    throw new InvalidOperationException($"Module Definition returned null Operations: {definition.ModuleId}");
                }

                foreach (var operation in moduleOperations)
                {
                    if (operation == null)
                    {
                        throw new InvalidOperationException($"Module Definition returned a null Operation: {definition.ModuleId}");
                    }

                    if (operation.ModuleId != definition.ModuleId)
                    {
                        throw new InvalidOperationException($"Operation Module ID does not match Definition: {definition.ModuleId}/{operation.OperationId}");
                    }

                    operations.Add(operation);
                }
            }

            return FluxParticleExecutionPlan.Create(operations);
        }
    }
}