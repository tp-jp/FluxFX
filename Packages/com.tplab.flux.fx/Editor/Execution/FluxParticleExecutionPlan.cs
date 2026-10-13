using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Execution
{
    /// <summary>
    /// ParticleのOperationを検証・分類し、決定的な順序で保持する実行計画です。
    /// Target・Phase・属性アクセスの整合性と、固定Pass順序に対する読み取り依存関係を検証します。
    /// Shader生成やGPU Dispatchの実行は担当しません。
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
        /// Target、Phase、Order、ModuleId、OperationIdの順に並び、外部から変更できません。
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
        /// 識別子の重複、属性アクセス、状態の読み取り時点を検証した後、
        /// Target、Phase、Order、ModuleId、OperationIdの順に並び替えます。
        /// </summary>
        /// <param name="operations">実行計画へ登録するOperation一覧。</param>
        /// <returns>検証と並び替えが完了したExecutionPlan。</returns>
        /// <exception cref="ArgumentNullException">
        /// Operation一覧がnullの場合に発生します。
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Operationの構成に不整合がある場合に発生します。
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
        /// </summary>
        /// <param name="target">確認対象となるOperationのTarget。</param>
        /// <param name="phase">確認対象となるOperationのPhase。</param>
        /// <returns>条件に一致するOperationが存在する場合はtrue。</returns>
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
        /// Operationの実行位置、論理属性アクセス、読み取り依存関係を検証します。
        /// 別Operationによる実際の書き込み有無は、ここでは要求しません。
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
                throw new InvalidOperationException($"Invalid Operation phase: {operation.ModuleId}/{operation.OperationId}");
            }

            if ((operation.ReadAttributes & ~DefinedAttributes) != FluxParticleAttribute.None ||
                (operation.WriteAttributes & ~DefinedAttributes) != FluxParticleAttribute.None)
            {
                throw new InvalidOperationException($"Operation contains undefined attributes: {operation.ModuleId}/{operation.OperationId}");
            }

            if (operation.Target == FluxParticleOperationTarget.Rendering)
            {
                if (operation.Phase != FluxParticleOperationPhase.Render ||
                    operation.WriteAttributes != FluxParticleAttribute.None)
                {
                    throw new InvalidOperationException($"Rendering Operation must use Render phase without state writes: {operation.ModuleId}/{operation.OperationId}");
                }
            }
            else
            {
                if (operation.Phase == FluxParticleOperationPhase.Render)
                {
                    throw new InvalidOperationException($"Simulation Operation cannot use Render phase: {operation.ModuleId}/{operation.OperationId}");
                }

                var writableAttributes = GetWritableAttributes(operation.Target);
                var invalidWrites = operation.WriteAttributes & ~writableAttributes;

                if (invalidWrites != FluxParticleAttribute.None)
                {
                    throw new InvalidOperationException($"Operation cannot write {invalidWrites} to {operation.Target}: {operation.ModuleId}/{operation.OperationId}");
                }
            }

            ValidateStateReads(operation);
        }

        /// <summary>
        /// ReadAttributesとStateReadsの対応を検証します。
        /// Workingは同じTargetの属性、Updatedは先行Passの属性に限定します。
        /// </summary>
        static void ValidateStateReads(FluxParticleOperation operation)
        {
            var declaredAttributes = FluxParticleAttribute.None;

            foreach (var read in operation.StateReads)
            {
                if (read == null)
                {
                    throw new InvalidOperationException($"Operation contains a null state read: {operation.ModuleId}/{operation.OperationId}");
                }

                var attribute = read.Attribute;
                var bits = (int)attribute;

                if (attribute == FluxParticleAttribute.None ||
                    (attribute & ~DefinedAttributes) != FluxParticleAttribute.None ||
                    (bits & (bits - 1)) != 0)
                {
                    throw new InvalidOperationException($"State read must specify one defined attribute: {operation.ModuleId}/{operation.OperationId}");
                }

                if ((declaredAttributes & attribute) != FluxParticleAttribute.None)
                {
                    throw new InvalidOperationException($"Duplicate state read for {attribute}: {operation.ModuleId}/{operation.OperationId}");
                }

                declaredAttributes |= attribute;

                if (!Enum.IsDefined(typeof(FluxParticleStateReadSource), read.Source))
                {
                    throw new InvalidOperationException($"Invalid state read source: {operation.ModuleId}/{operation.OperationId}");
                }

                if (read.Source == FluxParticleStateReadSource.Previous) continue;

                var owner = GetAttributeTarget(attribute);

                if (read.Source == FluxParticleStateReadSource.Working)
                {
                    if (operation.Target != owner)
                    {
                        throw new InvalidOperationException($"Working state for {attribute} is not available in {operation.Target}: {operation.ModuleId}/{operation.OperationId}");
                    }

                    continue;
                }

                if (GetPassOrder(owner) >= GetPassOrder(operation.Target))
                {
                    throw new InvalidOperationException($"Updated state for {attribute} is not available before {operation.Target}: {operation.ModuleId}/{operation.OperationId}");
                }
            }

            if (declaredAttributes != operation.ReadAttributes)
            {
                throw new InvalidOperationException($"State reads do not match ReadAttributes: {operation.ModuleId}/{operation.OperationId}");
            }
        }

        /// <summary>
        /// 論理属性を所有するSimulation Targetを取得します。
        /// 現行のState Layoutに基づく対応です。
        /// </summary>
        static FluxParticleOperationTarget GetAttributeTarget(FluxParticleAttribute attribute)
        {
            switch (attribute)
            {
                case FluxParticleAttribute.Velocity:
                case FluxParticleAttribute.Lifetime:
                    return FluxParticleOperationTarget.Velocity;

                case FluxParticleAttribute.Position:
                case FluxParticleAttribute.Age:
                    return FluxParticleOperationTarget.Position;

                case FluxParticleAttribute.StartColor:
                case FluxParticleAttribute.StartSize:
                    return FluxParticleOperationTarget.Visual;

                case FluxParticleAttribute.StartRotation:
                    return FluxParticleOperationTarget.Rotation;

                default:
                    throw new InvalidOperationException($"Unknown Particle attribute: {attribute}");
            }
        }

        /// <summary>
        /// 現行のGPU実行順序におけるTargetの位置を取得します。
        /// この順序はGPU Runtimeの実装と一致させる必要があります。
        /// </summary>
        static int GetPassOrder(FluxParticleOperationTarget target)
        {
            switch (target)
            {
                case FluxParticleOperationTarget.Velocity:
                    return 0;

                case FluxParticleOperationTarget.Position:
                    return 1;

                case FluxParticleOperationTarget.Visual:
                    return 2;

                case FluxParticleOperationTarget.Rotation:
                    return 3;

                case FluxParticleOperationTarget.Rendering:
                    return 4;

                default:
                    throw new InvalidOperationException($"Unknown Operation target: {target}");
            }
        }

        /// <summary>
        /// 現在のState Layoutにおいて、指定したTargetが変更できる論理属性を取得します。
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