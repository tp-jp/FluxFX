using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.GpuModules;

namespace TpLab.Flux.FX.Editor.Execution
{
    /// <summary>
    /// GPU Module単位で共有するパラメータ情報を保持します。
    /// 同一Moduleが複数Operationを持つ場合も、Module情報は1つのインスタンスを共有します。
    /// </summary>
    public sealed class FluxParticleCompiledModule
    {
        /// <summary>
        /// Moduleを識別する一意のIDです。
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// Moduleの型です。
        /// </summary>
        public Type ModuleType { get; }

        /// <summary>
        /// Moduleに属するGPUパラメータです。
        /// 各Passには、そのPassで使用するModuleのパラメータのみを配置します。
        /// </summary>
        public IReadOnlyList<FluxParticleGpuParameterCompilation> Parameters { get; }

        /// <summary>
        /// Moduleの有効状態を制御するUniform名です。
        /// </summary>
        public string EnabledParameterName { get; }

        /// <summary>
        /// Module共通のCompilation情報を作成します。
        /// </summary>
        /// <param name="moduleId">Moduleの識別子。</param>
        /// <param name="moduleType">Moduleの型。</param>
        /// <param name="parameters">Module固有のGPUパラメータ。</param>
        /// <param name="enabledParameterName">有効状態を制御するUniform名。</param>
        public FluxParticleCompiledModule(
            string moduleId,
            Type moduleType,
            IEnumerable<FluxParticleGpuParameterCompilation> parameters,
            string enabledParameterName)
        {
            if (string.IsNullOrWhiteSpace(moduleId)) throw new ArgumentException("Module ID is required.", nameof(moduleId));
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (string.IsNullOrWhiteSpace(enabledParameterName)) throw new ArgumentException("Enabled parameter name is required.", nameof(enabledParameterName));

            var values = new List<FluxParticleGpuParameterCompilation>(parameters);

            foreach (var parameter in values)
            {
                if (parameter == null)
                {
                    throw new ArgumentException("Module parameters cannot contain null.", nameof(parameters));
                }
            }

            ModuleId = moduleId;
            ModuleType = moduleType;
            Parameters = values.AsReadOnly();
            EnabledParameterName = enabledParameterName;
        }
    }

    /// <summary>
    /// GPU上で実行するOperationと、そのShader実装との対応を保持します。
    /// Module共通情報はCompiledModuleを参照し、OperationごとにHLSLを指定します。
    /// </summary>
    public sealed class FluxParticleCompiledOperation
    {
        /// <summary>
        /// 実行位置と状態アクセスを定義したOperationです。
        /// </summary>
        public FluxParticleOperation Operation { get; }

        /// <summary>
        /// Operationを所有するModuleのCompilation情報です。
        /// </summary>
        public FluxParticleCompiledModule Module { get; }

        /// <summary>
        /// OperationのHLSL実装を保持するファイルです。
        /// </summary>
        public string HlslPath { get; }

        /// <summary>
        /// Operationの実行時に呼び出すHLSL関数名です。
        /// </summary>
        public string EntryPoint { get; }

        /// <summary>
        /// GPU OperationのShader実装を定義します。
        /// </summary>
        /// <param name="operation">対応するSimulation Operation。</param>
        /// <param name="module">Operationを所有するModule。</param>
        /// <param name="hlslPath">HLSLファイルパス。</param>
        /// <param name="entryPoint">HLSL関数名。</param>
        public FluxParticleCompiledOperation(
            FluxParticleOperation operation,
            FluxParticleCompiledModule module,
            string hlslPath,
            string entryPoint)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (string.IsNullOrWhiteSpace(hlslPath)) throw new ArgumentException("HLSL path is required.", nameof(hlslPath));
            if (string.IsNullOrWhiteSpace(entryPoint)) throw new ArgumentException("Entry point is required.", nameof(entryPoint));

            if (operation.Target == FluxParticleOperationTarget.Rendering)
            {
                throw new ArgumentException("Rendering Operations cannot use Simulation Shader compilation.", nameof(operation));
            }

            if (operation.ModuleId != module.ModuleId)
            {
                throw new ArgumentException("Operation Module ID does not match its compiled Module.", nameof(module));
            }

            Operation = operation;
            Module = module;
            HlslPath = hlslPath.Replace('\\', '/');
            EntryPoint = entryPoint;
        }
    }

    /// <summary>
    /// 単一のSimulation Targetに配置するGPU Operationをまとめます。
    /// SpawnとUpdateのOperationを区別して保持し、物理GPU Passへの配置単位を表します。
    /// </summary>
    public sealed class FluxParticleCompiledPass
    {
        /// <summary>
        /// このPassが更新する状態Targetです。
        /// </summary>
        public FluxParticleOperationTarget Target { get; }

        /// <summary>
        /// Particle発生時に実行するOperationです。
        /// </summary>
        public IReadOnlyList<FluxParticleCompiledOperation> SpawnOperations { get; }

        /// <summary>
        /// Particle更新時に実行するOperationです。
        /// </summary>
        public IReadOnlyList<FluxParticleCompiledOperation> UpdateOperations { get; }

        /// <summary>
        /// このPassで使用するModuleです。
        /// 同一Moduleが複数Operationを持つ場合でも重複しません。
        /// </summary>
        public IReadOnlyList<FluxParticleCompiledModule> Modules { get; }

        internal FluxParticleCompiledPass(
            FluxParticleOperationTarget target,
            IReadOnlyList<FluxParticleCompiledOperation> operations)
        {
            Target = target;

            var spawn = new List<FluxParticleCompiledOperation>();
            var update = new List<FluxParticleCompiledOperation>();
            var modules = new List<FluxParticleCompiledModule>();
            var moduleIds = new HashSet<string>(StringComparer.Ordinal);
            var uniformNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var operation in operations)
            {
                switch (operation.Operation.Phase)
                {
                    case FluxParticleOperationPhase.Spawn:
                        spawn.Add(operation);
                        break;

                    case FluxParticleOperationPhase.Update:
                        update.Add(operation);
                        break;

                    default:
                        throw new InvalidOperationException($"Invalid Simulation phase: {operation.Operation.Phase}");
                }

                if (!moduleIds.Add(operation.Module.ModuleId)) continue;

                var module = operation.Module;

                RegisterUniform(uniformNames, module.EnabledParameterName, target);

                foreach (var parameter in module.Parameters)
                {
                    RegisterUniform(uniformNames, parameter.Name, target);
                }

                modules.Add(module);
            }

            spawn.Sort(CompareOperations);
            update.Sort(CompareOperations);

            SpawnOperations = spawn.AsReadOnly();
            UpdateOperations = update.AsReadOnly();
            Modules = modules.AsReadOnly();
        }

        /// <summary>
        /// 同一Passで使用するUniform名が重複していないことを検証します。
        /// </summary>
        static void RegisterUniform(
            HashSet<string> names,
            string name,
            FluxParticleOperationTarget target)
        {
            if (!names.Add(name))
            {
                throw new InvalidOperationException($"Duplicate GPU uniform in {target} Pass: {name}");
            }
        }

        /// <summary>
        /// 同一Phase内でのOperation実行順序を確定します。
        /// </summary>
        static int CompareOperations(
            FluxParticleCompiledOperation a,
            FluxParticleCompiledOperation b)
        {
            var order = a.Operation.Order.CompareTo(b.Operation.Order);
            if (order != 0) return order;

            var module = string.CompareOrdinal(a.Operation.ModuleId, b.Operation.ModuleId);
            if (module != 0) return module;

            return string.CompareOrdinal(a.Operation.OperationId, b.Operation.OperationId);
        }
    }

    /// <summary>
    /// ExecutionPlanとGPU Shader実装を対応付けたCompilationモデルです。
    /// SimulationをTargetごとのPassへ集約し、Rendering Operationを独立して保持します。
    /// Shaderソース生成やRuntime Bindingの書き出しは担当しません。
    /// </summary>
    public sealed class FluxParticleOperationCompilation
    {
        readonly IReadOnlyList<FluxParticleCompiledPass> _passes;

        /// <summary>
        /// 元となるExecutionPlanです。
        /// </summary>
        public FluxParticleExecutionPlan ExecutionPlan { get; }

        /// <summary>
        /// Simulation用のGPU Pass一覧です。
        /// Velocity、Position、Visual、Rotationの順に保持します。
        /// </summary>
        public IReadOnlyList<FluxParticleCompiledPass> Passes => _passes;

        /// <summary>
        /// Simulation用Shader生成には含めないRendering Operationです。
        /// </summary>
        public IReadOnlyList<FluxParticleOperation> RenderOperations { get; }

        FluxParticleOperationCompilation(
            FluxParticleExecutionPlan executionPlan,
            IReadOnlyList<FluxParticleCompiledPass> passes,
            IReadOnlyList<FluxParticleOperation> renderOperations)
        {
            ExecutionPlan = executionPlan;
            _passes = passes;
            RenderOperations = renderOperations;
        }

        /// <summary>
        /// ExecutionPlanとGPU OperationのShader実装からCompilationを生成します。
        /// すべてのSimulation Operationに対応するShader実装が必要です。
        /// Rendering OperationはShader実装一覧には含めず、別途保持します。
        /// </summary>
        /// <param name="executionPlan">検証済みのExecutionPlan。</param>
        /// <param name="compiledOperations">Simulation OperationのShader実装一覧。</param>
        /// <returns>Target単位に分類したOperationCompilation。</returns>
        /// <exception cref="InvalidOperationException">
        /// Simulation Operationの不足、重複、Module情報の不一致などがある場合に発生します。
        /// </exception>
        public static FluxParticleOperationCompilation Create(
            FluxParticleExecutionPlan executionPlan,
            IEnumerable<FluxParticleCompiledOperation> compiledOperations)
        {
            if (executionPlan == null) throw new ArgumentNullException(nameof(executionPlan));
            if (compiledOperations == null) throw new ArgumentNullException(nameof(compiledOperations));

            var expected = new HashSet<FluxParticleOperation>();
            var compiled = new HashSet<FluxParticleOperation>();
            var moduleMap = new Dictionary<string, FluxParticleCompiledModule>(StringComparer.Ordinal);
            var byTarget = new Dictionary<FluxParticleOperationTarget, List<FluxParticleCompiledOperation>>();
            var render = new List<FluxParticleOperation>();

            foreach (var target in GetSimulationTargets())
            {
                byTarget.Add(target, new List<FluxParticleCompiledOperation>());
            }

            foreach (var operation in executionPlan.Operations)
            {
                if (operation.Target == FluxParticleOperationTarget.Rendering)
                {
                    render.Add(operation);
                }
                else
                {
                    expected.Add(operation);
                }
            }

            foreach (var entry in compiledOperations)
            {
                if (entry == null)
                {
                    throw new InvalidOperationException("Compiled Operations cannot contain null.");
                }

                var operation = entry.Operation;

                if (!expected.Contains(operation))
                {
                    throw new InvalidOperationException($"GPU Operation is not registered in ExecutionPlan: {operation.ModuleId}/{operation.OperationId}");
                }

                if (!compiled.Add(operation))
                {
                    throw new InvalidOperationException($"Duplicate compiled Operation: {operation.ModuleId}/{operation.OperationId}");
                }

                if (moduleMap.TryGetValue(entry.Module.ModuleId, out var registeredModule))
                {
                    if (!ReferenceEquals(registeredModule, entry.Module))
                    {
                        throw new InvalidOperationException($"Compiled Module must be shared across Operations: {entry.Module.ModuleId}");
                    }
                }
                else
                {
                    moduleMap.Add(entry.Module.ModuleId, entry.Module);
                }

                byTarget[operation.Target].Add(entry);
            }

            if (compiled.Count != expected.Count)
            {
                foreach (var operation in expected)
                {
                    if (compiled.Contains(operation)) continue;

                    throw new InvalidOperationException($"GPU Operation has no Shader implementation: {operation.ModuleId}/{operation.OperationId}");
                }
            }

            var passes = new List<FluxParticleCompiledPass>();

            foreach (var target in GetSimulationTargets())
            {
                passes.Add(new FluxParticleCompiledPass(target, byTarget[target]));
            }

            return new FluxParticleOperationCompilation(
                executionPlan,
                passes.AsReadOnly(),
                render.AsReadOnly());
        }

        /// <summary>
        /// 指定したSimulation TargetのCompilationを取得します。
        /// Renderingは対象外です。
        /// </summary>
        /// <param name="target">取得対象のSimulation Target。</param>
        /// <returns>一致するGPU PassのCompilation。</returns>
        /// <exception cref="InvalidOperationException">
        /// 指定したTargetがSimulation用ではない場合に発生します。
        /// </exception>
        public FluxParticleCompiledPass GetPass(FluxParticleOperationTarget target)
        {
            foreach (var pass in _passes)
            {
                if (pass.Target == target) return pass;
            }

            throw new InvalidOperationException($"Simulation Pass was not found: {target}");
        }

        /// <summary>
        /// 現在のSimulation Target一覧をGPU実行順序で取得します。
        /// </summary>
        static FluxParticleOperationTarget[] GetSimulationTargets()
        {
            return new[]
            {
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationTarget.Position,
                FluxParticleOperationTarget.Visual,
                FluxParticleOperationTarget.Rotation
            };
        }
    }
}