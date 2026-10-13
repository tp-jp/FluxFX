using System;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// Operationの状態読み取り時点とGPU Passの依存関係を検証します。
    /// </summary>
    public class FluxParticleStateAccessTests
    {
        /// <summary>
        /// Velocity Updateが前フレームのPositionを参照できることを確認します。
        /// </summary>
        [Test]
        public void VelocityCanReadPreviousPosition()
        {
            var operation = CreateOperation(
                FluxParticleOperationTarget.Velocity,
                FluxParticleAttribute.Position,
                new FluxParticleStateRead(
                    FluxParticleAttribute.Position,
                    FluxParticleStateReadSource.Previous));

            AssertValid(operation);
        }

        /// <summary>
        /// Position Updateが更新済みVelocityを参照できることを確認します。
        /// </summary>
        [Test]
        public void PositionCanReadUpdatedVelocity()
        {
            var operation = CreateOperation(
                FluxParticleOperationTarget.Position,
                FluxParticleAttribute.Velocity,
                new FluxParticleStateRead(
                    FluxParticleAttribute.Velocity,
                    FluxParticleStateReadSource.Updated));

            AssertValid(operation);
        }

        /// <summary>
        /// Position Updateが同一PassのWorking Positionを参照できることを確認します。
        /// </summary>
        [Test]
        public void PositionCanReadWorkingPosition()
        {
            var operation = CreateOperation(
                FluxParticleOperationTarget.Position,
                FluxParticleAttribute.Position,
                new FluxParticleStateRead(
                    FluxParticleAttribute.Position,
                    FluxParticleStateReadSource.Working));

            AssertValid(operation);
        }

        /// <summary>
        /// Velocity Passから未実行のPosition Passの更新結果を参照できないことを確認します。
        /// </summary>
        [Test]
        public void VelocityCannotReadUpdatedPosition()
        {
            var operation = CreateOperation(
                FluxParticleOperationTarget.Velocity,
                FluxParticleAttribute.Position,
                new FluxParticleStateRead(
                    FluxParticleAttribute.Position,
                    FluxParticleStateReadSource.Updated));

            AssertInvalid(operation);
        }

        /// <summary>
        /// 別TargetのWorking状態を参照できないことを確認します。
        /// </summary>
        [Test]
        public void PositionCannotReadWorkingVelocity()
        {
            var operation = CreateOperation(
                FluxParticleOperationTarget.Position,
                FluxParticleAttribute.Velocity,
                new FluxParticleStateRead(
                    FluxParticleAttribute.Velocity,
                    FluxParticleStateReadSource.Working));

            AssertInvalid(operation);
        }

        /// <summary>
        /// 同じPass自身のUpdated状態を参照できないことを確認します。
        /// 同一Pass内の更新結果はWorkingを使用します。
        /// </summary>
        [Test]
        public void PositionCannotReadUpdatedPosition()
        {
            var operation = CreateOperation(
                FluxParticleOperationTarget.Position,
                FluxParticleAttribute.Position,
                new FluxParticleStateRead(
                    FluxParticleAttribute.Position,
                    FluxParticleStateReadSource.Updated));

            AssertInvalid(operation);
        }

        /// <summary>
        /// RenderingがSimulationの更新済み状態を参照できることを確認します。
        /// </summary>
        [Test]
        public void RenderingCanReadUpdatedRotation()
        {
            var operation = new FluxParticleOperation(
                "render-test",
                "evaluate",
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render,
                FluxParticleAttribute.StartRotation,
                FluxParticleAttribute.None,
                100,
                new[]
                {
                    new FluxParticleStateRead(
                        FluxParticleAttribute.StartRotation,
                        FluxParticleStateReadSource.Updated)
                });

            AssertValid(operation);
        }

        /// <summary>
        /// ReadAttributesとStateReadsの属性が一致しない場合に拒否することを確認します。
        /// </summary>
        [Test]
        public void MissingDeclaredReadIsRejected()
        {
            var operation = new FluxParticleOperation(
                "read-test",
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Position | FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Velocity,
                100,
                new[]
                {
                    new FluxParticleStateRead(
                        FluxParticleAttribute.Position,
                        FluxParticleStateReadSource.Previous)
                });

            AssertInvalid(operation);
        }

        /// <summary>
        /// 同一属性の読み取り契約が重複した場合に拒否することを確認します。
        /// </summary>
        [Test]
        public void DuplicateStateReadIsRejected()
        {
            var operation = new FluxParticleOperation(
                "read-test",
                "apply",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Velocity,
                100,
                new[]
                {
                    new FluxParticleStateRead(
                        FluxParticleAttribute.Velocity,
                        FluxParticleStateReadSource.Previous),
                    new FluxParticleStateRead(
                        FluxParticleAttribute.Velocity,
                        FluxParticleStateReadSource.Working)
                });

            AssertInvalid(operation);
        }

        /// <summary>
        /// 属性を複数指定した単一のStateReadを拒否することを確認します。
        /// </summary>
        [Test]
        public void CombinedAttributeReadIsRejected()
        {
            var operation = CreateOperation(
                FluxParticleOperationTarget.Position,
                FluxParticleAttribute.Position | FluxParticleAttribute.Velocity,
                new FluxParticleStateRead(
                    FluxParticleAttribute.Position | FluxParticleAttribute.Velocity,
                    FluxParticleStateReadSource.Previous));

            AssertInvalid(operation);
        }

        /// <summary>
        /// StateReads未指定の場合にPreviousとして展開されることを確認します。
        /// </summary>
        [Test]
        public void DefaultReadsUsePreviousState()
        {
            var operation = new FluxParticleOperation(
                "read-test",
                "apply",
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update,
                FluxParticleAttribute.Position | FluxParticleAttribute.Velocity,
                FluxParticleAttribute.Position,
                100);

            Assert.AreEqual(2, operation.StateReads.Count);
            Assert.AreEqual(FluxParticleStateReadSource.Previous, operation.StateReads[0].Source);
            Assert.AreEqual(FluxParticleStateReadSource.Previous, operation.StateReads[1].Source);

            AssertValid(operation);
        }

        /// <summary>
        /// テスト用のUpdate Operationを生成します。
        /// </summary>
        static FluxParticleOperation CreateOperation(
            FluxParticleOperationTarget target,
            FluxParticleAttribute reads,
            FluxParticleStateRead stateRead)
        {
            return new FluxParticleOperation(
                "read-test",
                "apply",
                target,
                FluxParticleOperationPhase.Update,
                reads,
                FluxParticleAttribute.None,
                100,
                new[] { stateRead });
        }

        /// <summary>
        /// OperationがExecutionPlanへ登録できることを確認します。
        /// </summary>
        static void AssertValid(FluxParticleOperation operation)
        {
            var plan = FluxParticleExecutionPlan.Create(new[] { operation });

            Assert.AreEqual(1, plan.OperationCount);
        }

        /// <summary>
        /// 不正なOperationがExecutionPlanによって拒否されることを確認します。
        /// </summary>
        static void AssertInvalid(FluxParticleOperation operation)
        {
            Assert.Throws<InvalidOperationException>(() =>
                FluxParticleExecutionPlan.Create(new[] { operation }));
        }
    }
}