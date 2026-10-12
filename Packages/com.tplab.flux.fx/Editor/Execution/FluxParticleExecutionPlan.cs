using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Execution
{
    /// <summary>
    /// ParticleのOperationを検証・分類し、決定的な順序で保持する実行計画です。
    /// Target・Phase・属性書き込みの基本的な整合性を検証します。
    /// GPU Pass間のデータ依存関係、状態Bufferの読み取り時点、
    /// Shader生成および実際のGPU実行順序は、この段階では管理しません。
    /// </summary>
    public sealed class FluxParticleExecutionPlan
    {
        const FluxParticleAttribute DefinedAttributes =
            FluxParticleAttribute.Position |
            FluxParticleAttribute.Age |
            FluxParticleAttribute.Velocity |
            FluxParticleAttribute.Lifetime |
            FluxParticleAttribute.StartColor |
            FluxParticleAttribute.StartSize |
            FluxParticleAttribute.StartRotation;

        readonly List<FluxParticleOperation> _operations;
        readonly IReadOnlyList<FluxParticleOperation> _readOnlyOperations;

        /// <summary>
        /// 検証済みのOperation一覧です。
        /// Target、Phase、Order、ModuleId、OperationIdの順で並び、外部から変更できません。
        /// </summary>
        public IReadOnlyList<FluxParticleOperation> Operations => _readOnlyOperations;

        /// <summary>
        /// 実行計画に登録されているOperationの総数です。
        /// </summary>
        public int OperationCount => _operations.Count;

        FluxParticleExecutionPlan(List<FluxParticleOperation> operations)
        {
            _operations = operations;
            _readOnlyOperations = _operations.AsReadOnly();
        }

        /// <summary>
        /// Operation一覧から実行計画を生成します。
        /// 個々のOperationの実行位置と属性アクセスを検証し、
        /// ModuleIdとOperationIdの重複を検出した後、決定的な順序へ並び替えます。
        /// </summary>
        /// <param name="operations">実行計画へ登録するOperation一覧。</param>
        /// <returns>検証と並び替えが完了したExecutionPlan。</returns>
        /// <exception cref="ArgumentNullException">
        /// Operation一覧がnullの場合に発生します。
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// nullのOperation、重複した識別子、不正なTarget・Phase、
        /// または許可されていない属性アクセスを検出した場合に発生します。
        /// </exception>
        public static FluxParticleExecutionPlan Create(IEnumerable<FluxParticleOperation> operations)
        {
            if (operations == null) throw new ArgumentNullException(nameof(operations));

            var result = new List<FluxParticleOperation>();
            var identifiers = new HashSet<string>(StringComparer.Ordinal);

            foreach (var operation in operations)
            {
                if (operation == null)
                {
                    throw new InvalidOperationException("Execution Plan contains a null Operation.");
                }

                ValidateOperation(operation);

                var identifier = operation.ModuleId + "\0" + operation.OperationId;

                if (!identifiers.Add(identifier))
                {
                    throw new InvalidOperationException(
                        $"Duplicate Operation ID: {operation.ModuleId}/{operation.OperationId}");
                }

                result.Add(operation);
            }

            result.Sort(CompareOperations);

            return new FluxParticleExecutionPlan(result);
        }

        /// <summary>
        /// 指定したTargetとPhaseに属するOperationを取得します。
        /// 結果はOrder、ModuleId、OperationIdの順に並び、外部から変更できません。
        /// </summary>
        /// <param name="target">取得対象となるOperationのTarget。</param>
        /// <param name="phase">取得対象となるOperationのPhase。</param>
        /// <returns>条件に一致するOperation一覧。該当がない場合は空の一覧。</returns>
        public IReadOnlyList<FluxParticleOperation> GetOperations(
            FluxParticleOperationTarget target,
            FluxParticleOperationPhase phase)
        {
            var result = new List<FluxParticleOperation>();

            foreach (var operation in _operations)
            {
                if (operation.Target != target || operation.Phase != phase) continue;

                result.Add(operation);
            }

            return result.AsReadOnly();
        }

        /// <summary>
        /// 指定したTargetとPhaseに属するOperationが存在するか確認します。
        /// 将来的なGPU Passの実行条件判定などに利用できます。
        /// </summary>
        /// <param name="target">確認対象となるOperationのTarget。</param>
        /// <param name="phase">確認対象となるOperationのPhase。</param>
        /// <returns>条件に一致するOperationが1つ以上存在する場合はtrue。</returns>
        public bool HasOperations(
            FluxParticleOperationTarget target,
            FluxParticleOperationPhase phase)
        {
            foreach (var operation in _operations)
            {
                if (operation.Target == target && operation.Phase == phase) return true;
            }

            return false;
        }

        /// <summary>
        /// Operationの実行位置と属性アクセスが、現在定義されている実行モデルに適合するか検証します。
        /// GPU Pass間のRead/Write依存関係や、複数Operation間の書き込み競合は検証しません。
        /// </summary>
        static void ValidateOperation(FluxParticleOperation operation)
        {
            if (!Enum.IsDefined(typeof(FluxParticleOperationTarget), operation.Target))
            {
                throw new InvalidOperationException(
                    $"Invalid Operation target: {operation.ModuleId}/{operation.OperationId}");
            }

            if (!Enum.IsDefined(typeof(FluxParticleOperationPhase), operation.Phase))
            {
                throw new InvalidOperationException(
                    $"Invalid Operation phase: {operation.ModuleId}/{operation.OperationId}");
            }

            if ((operation.ReadAttributes & ~DefinedAttributes) != FluxParticleAttribute.None ||
                (operation.WriteAttributes & ~DefinedAttributes) != FluxParticleAttribute.None)
            {
                throw new InvalidOperationException(
                    $"Operation contains undefined attributes: {operation.ModuleId}/{operation.OperationId}");
            }

            if (operation.Target == FluxParticleOperationTarget.Rendering)
            {
                if (operation.Phase != FluxParticleOperationPhase.Render ||
                    operation.WriteAttributes != FluxParticleAttribute.None)
                {
                    throw new InvalidOperationException(
                        $"Rendering Operation must use Render phase without state writes: {operation.ModuleId}/{operation.OperationId}");
                }

                return;
            }

            if (operation.Phase == FluxParticleOperationPhase.Render)
            {
                throw new InvalidOperationException(
                    $"Simulation Operation cannot use Render phase: {operation.ModuleId}/{operation.OperationId}");
            }

            var writableAttributes = GetWritableAttributes(operation.Target);
            var invalidWrites = operation.WriteAttributes & ~writableAttributes;

            if (invalidWrites != FluxParticleAttribute.None)
            {
                throw new InvalidOperationException(
                    $"Operation cannot write {invalidWrites} to {operation.Target}: {operation.ModuleId}/{operation.OperationId}");
            }
        }

        /// <summary>
        /// 現在のState Layoutにおいて、指定したTargetが変更できる論理属性を取得します。
        /// 物理Textureの構成が変更された場合は、State Layout側への責務移管を検討します。
        /// </summary>
        static FluxParticleAttribute GetWritableAttributes(FluxParticleOperationTarget target)
        {
            switch (target)
            {
                case FluxParticleOperationTarget.Velocity:
                    return FluxParticleAttribute.Velocity | FluxParticleAttribute.Lifetime;

                case FluxParticleOperationTarget.Position:
                    return FluxParticleAttribute.Position | FluxParticleAttribute.Age;

                case FluxParticleOperationTarget.Visual:
                    return FluxParticleAttribute.StartColor | FluxParticleAttribute.StartSize;

                case FluxParticleOperationTarget.Rotation:
                    return FluxParticleAttribute.StartRotation;

                default:
                    return FluxParticleAttribute.None;
            }
        }

        /// <summary>
        /// OperationをTarget、Phase、Order、ModuleId、OperationIdの順に比較します。
        /// 同じ入力から常に同じ順序のExecutionPlanを生成するための比較処理です。
        /// </summary>
        static int CompareOperations(FluxParticleOperation a, FluxParticleOperation b)
        {
            var target = a.Target.CompareTo(b.Target);
            if (target != 0) return target;

            var phase = a.Phase.CompareTo(b.Phase);
            if (phase != 0) return phase;

            var order = a.Order.CompareTo(b.Order);
            if (order != 0) return order;

            var module = string.CompareOrdinal(a.ModuleId, b.ModuleId);
            if (module != 0) return module;

            return string.CompareOrdinal(a.OperationId, b.OperationId);
        }
    }
}