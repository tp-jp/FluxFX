using System;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// ExecutionPlanのOperation分類、順序保証、実行位置と属性アクセスの検証を行います。
    /// GPU Shaderの生成やRuntimeの実行結果は対象としません。
    /// </summary>
    public class FluxParticleExecutionPlanTests
    {
        /// <summary>
        /// Operationが存在しない実行計画を生成できることを確認します。
        /// </summary>
        [Test]
        public void EmptyPlanIsValid()
        {
            var plan = FluxParticleExecutionPlan.Create(Array.Empty<FluxParticleOperation>());

            Assert.AreEqual(0, plan.OperationCount);
            Assert.IsEmpty(plan.Operations);
        }

        /// <summary>
        /// Gravity相当のOperationがVelocityのUpdate Phaseへ分類されることを確認します。
        /// </summary>
        [Test]
        public void GravityUsesVelocityUpdate()
        {
            var operation = CreateOperation(
                "gravity",
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Velocity);

            var plan = FluxParticleExecutionPlan.Create(new[] { operation });

            Assert.AreEqual(1, plan.OperationCount);
            Assert.IsTrue(plan.HasOperations(
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update));
        }

        /// <summary>
        /// VelocityOverLifetime相当のOperationがPositionのUpdate Phaseへ分類されることを確認します。
        /// </summary>
        [Test]
        public void VelocityOverLifetimeUsesPositionUpdate()
        {
            var operation = CreateOperation(
                "velocity-over-lifetime",
                "apply-position",
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Position |
                FluxParticleAttribute.Velocity |
                FluxParticleAttribute.Age |
                FluxParticleAttribute.Lifetime,
                FluxParticleAttribute.Position);

            var plan = FluxParticleExecutionPlan.Create(new[] { operation });

            Assert.AreEqual(1, plan.GetOperations(
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update).Count);
        }

        /// <summary>
        /// RenderingのOperationがParticleの状態を書き換えずに登録できることを確認します。
        /// </summary>
        [Test]
        public void RenderingOperationDoesNotWriteState()
        {
            var operation = CreateOperation(
                "color-over-lifetime",
                "evaluate",
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render,
                FluxParticleAttribute.StartColor |
                FluxParticleAttribute.Age |
                FluxParticleAttribute.Lifetime,
                FluxParticleAttribute.None);

            var plan = FluxParticleExecutionPlan.Create(new[] { operation });

            Assert.AreEqual(1, plan.GetOperations(
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render).Count);
        }

        /// <summary>
        /// 1つのModuleが異なるTarget・Phaseに複数Operationを定義できることを確認します。
        /// </summary>
        [Test]
        public void OneModuleCanDefineMultipleOperations()
        {
            var spawn = CreateOperation(
                "custom-module",
                "initialize-velocity",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Spawn,
                FluxParticleAttribute.Position,
                FluxParticleAttribute.Velocity);

            var update = CreateOperation(
                "custom-module",
                "update-position",
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Position |
                FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Position);

            var plan = FluxParticleExecutionPlan.Create(new[] { update, spawn });

            Assert.AreEqual(2, plan.OperationCount);
            Assert.IsTrue(plan.HasOperations(
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Spawn));
            Assert.IsTrue(plan.HasOperations(
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update));
        }

        /// <summary>
        /// 同一Target・Phase内のOperationがOrder、ModuleIdの順に並ぶことを確認します。
        /// </summary>
        [Test]
        public void OperationsAreSortedByOrderAndModuleId()
        {
            var last = CreateOperation(
                "z-module",
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Velocity,
                200);

            var middle = CreateOperation(
                "b-module",
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Velocity,
                100);

            var first = CreateOperation(
                "a-module",
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Velocity,
                100);

            var plan = FluxParticleExecutionPlan.Create(new[] { last, middle, first });
            var operations = plan.GetOperations(
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            Assert.AreSame(first, operations[0]);
            Assert.AreSame(middle, operations[1]);
            Assert.AreSame(last, operations[2]);
        }

        /// <summary>
        /// 同一Module内でOperationIdが重複した場合に例外となることを確認します。
        /// TargetやPhaseが異なっていても同一Operationとして扱います。
        /// </summary>
        [Test]
        public void DuplicateOperationIdIsRejected()
        {
            var first = CreateOperation(
                "custom-module",
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Spawn,
                FluxParticleAttribute.None,
                FluxParticleAttribute.Velocity);

            var second = CreateOperation(
                "custom-module",
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Velocity);

            Assert.Throws<InvalidOperationException>(() =>
                FluxParticleExecutionPlan.Create(new[] { first, second }));
        }

        /// <summary>
        /// Positionを更新するOperationがVelocityへ書き込む構成を拒否することを確認します。
        /// </summary>
        [Test]
        public void PositionOperationCannotWriteVelocity()
        {
            var operation = CreateOperation(
                "invalid-module",
                "apply",
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Position,
                FluxParticleAttribute.Velocity);

            Assert.Throws<InvalidOperationException>(() =>
                FluxParticleExecutionPlan.Create(new[] { operation }));
        }

        /// <summary>
        /// RenderingのOperationが状態Bufferへの書き込みを宣言できないことを確認します。
        /// </summary>
        [Test]
        public void RenderingOperationCannotWriteState()
        {
            var operation = CreateOperation(
                "invalid-render",
                "apply",
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render,
                FluxParticleAttribute.StartColor,
                FluxParticleAttribute.StartColor);

            Assert.Throws<InvalidOperationException>(() =>
                FluxParticleExecutionPlan.Create(new[] { operation }));
        }

        /// <summary>
        /// SimulationのOperationがRender Phaseで実行される構成を拒否することを確認します。
        /// </summary>
        [Test]
        public void SimulationOperationCannotUseRenderPhase()
        {
            var operation = CreateOperation(
                "invalid-phase",
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Render,
                FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Velocity);

            Assert.Throws<InvalidOperationException>(() =>
                FluxParticleExecutionPlan.Create(new[] { operation }));
        }

        /// <summary>
        /// 未定義のParticle属性へのアクセスを持つOperationを拒否することを確認します。
        /// </summary>
        [Test]
        public void UnknownAttributesAreRejected()
        {
            var operation = CreateOperation(
                "invalid-attribute",
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update,
                (FluxParticleAttribute)(1 << 20),
                FluxParticleAttribute.Velocity);

            Assert.Throws<InvalidOperationException>(() =>
                FluxParticleExecutionPlan.Create(new[] { operation }));
        }

        /// <summary>
        /// テスト用のOperationを生成します。
        /// 実際のModule DefinitionやShader生成には依存させません。
        /// </summary>
        static FluxParticleOperation CreateOperation(
            string moduleId,
            string operationId,
            FluxParticleOperationTarget target,
            FluxParticleOperationPhase phase,
            FluxParticleAttribute reads,
            FluxParticleAttribute writes,
            int order = 100)
        {
            return new FluxParticleOperation(
                moduleId,
                operationId,
                target,
                phase,
                reads,
                writes,
                order);
        }
    }
}