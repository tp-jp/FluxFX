using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Execution
{
    /// <summary>
    /// Operationが処理するParticleの状態、または描画処理を表します。
    /// SimulationのTargetは基本的に状態Bufferの更新先に対応します。
    /// </summary>
    public enum FluxParticleOperationTarget
    {
        /// <summary>
        /// VelocityとLifetimeを保持する状態Buffer。
        /// </summary>
        Velocity,

        /// <summary>
        /// PositionとAgeを保持する状態Buffer。
        /// </summary>
        Position,

        /// <summary>
        /// ColorとSizeを保持する状態Buffer。
        /// </summary>
        Visual,

        /// <summary>
        /// Rotationを保持する状態Buffer。
        /// </summary>
        Rotation,

        /// <summary>
        /// 状態Bufferを書き換えず、描画時にParticleの表現を評価します。
        /// </summary>
        Rendering
    }

    /// <summary>
    /// Operationを実行するParticleのライフサイクル上のタイミングを表します。
    /// PhaseはGPU Dispatchの単位ではなく、Pass内部の実行位置を定義します。
    /// </summary>
    public enum FluxParticleOperationPhase
    {
        /// <summary>
        /// Particleの発生時に実行します。
        /// </summary>
        Spawn,

        /// <summary>
        /// 生存中のParticleの状態更新時に実行します。
        /// </summary>
        Update,

        /// <summary>
        /// Particleの描画時に実行します。
        /// </summary>
        Render
    }

    /// <summary>
    /// Moduleが実行する個々の処理を定義します。
    /// 1つのModuleから複数のOperationを生成でき、それぞれ異なるTargetやPhaseを指定できます。
    /// 実行位置と論理属性のアクセス契約を保持し、HLSLやGPU Passの実装は扱いません。
    /// </summary>
    public sealed class FluxParticleOperation
    {
        /// <summary>
        /// このOperationを所有するModuleの識別子です。
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// Module内で一意となるOperationの識別子です。
        /// ModuleIdとOperationIdの組み合わせによってOperationを識別します。
        /// </summary>
        public string OperationId { get; }

        /// <summary>
        /// Operationの実行対象です。
        /// Simulationでは主に書き込み先の状態Bufferを示します。
        /// </summary>
        public FluxParticleOperationTarget Target { get; }

        /// <summary>
        /// Operationの実行タイミングです。
        /// </summary>
        public FluxParticleOperationPhase Phase { get; }

        /// <summary>
        /// Operationが参照する論理Particle属性です。
        /// </summary>
        public FluxParticleAttribute ReadAttributes { get; }

        /// <summary>
        /// Operationが変更する論理Particle属性です。
        /// </summary>
        public FluxParticleAttribute WriteAttributes { get; }

        /// <summary>
        /// 属性ごとの読み取り時点を指定した契約です。
        /// ReadAttributesに指定した属性と一致している必要があります。
        /// </summary>
        public IReadOnlyList<FluxParticleStateRead> StateReads { get; }

        /// <summary>
        /// 同一Target・Phase内での実行順序を決定する値です。
        /// 小さい値が先に実行され、同じ値の場合はModuleId、OperationIdの順で比較します。
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Moduleに所属するOperationを定義します。
        /// StateReadsを省略した場合は、ReadAttributes内の各属性をPreviousとして扱います。
        /// 実行位置と属性アクセスの整合性はExecutionPlanで検証します。
        /// </summary>
        /// <param name="moduleId">Operationを所有するModuleの識別子。</param>
        /// <param name="operationId">Module内で一意となるOperationの識別子。</param>
        /// <param name="target">実行対象となる状態Buffer、またはRendering。</param>
        /// <param name="phase">Operationを実行するタイミング。</param>
        /// <param name="readAttributes">参照する論理Particle属性。</param>
        /// <param name="writeAttributes">変更する論理Particle属性。</param>
        /// <param name="order">同一Target・Phase内での実行優先順位。</param>
        /// <param name="stateReads">属性ごとの読み取り時点。省略時はPrevious。</param>
        /// <exception cref="ArgumentException">
        /// ModuleIdまたはOperationIdが空の場合に発生します。
        /// </exception>
        public FluxParticleOperation(
            string moduleId,
            string operationId,
            FluxParticleOperationTarget target,
            FluxParticleOperationPhase phase,
            FluxParticleAttribute readAttributes,
            FluxParticleAttribute writeAttributes,
            int order,
            IEnumerable<FluxParticleStateRead> stateReads = null)
        {
            if (string.IsNullOrWhiteSpace(moduleId)) throw new ArgumentException("Module ID is required.", nameof(moduleId));
            if (string.IsNullOrWhiteSpace(operationId)) throw new ArgumentException("Operation ID is required.", nameof(operationId));

            ModuleId = moduleId;
            OperationId = operationId;
            Target = target;
            Phase = phase;
            ReadAttributes = readAttributes;
            WriteAttributes = writeAttributes;
            Order = order;

            var reads = new List<FluxParticleStateRead>();

            if (stateReads != null)
            {
                reads.AddRange(stateReads);
            }
            else
            {
                for (var bit = 0; bit < 32; bit++)
                {
                    var attribute = (FluxParticleAttribute)(1 << bit);

                    if ((readAttributes & attribute) == 0) continue;

                    reads.Add(new FluxParticleStateRead(
                        attribute,
                        FluxParticleStateReadSource.Previous));
                }
            }

            StateReads = reads.AsReadOnly();
        }
    }
}