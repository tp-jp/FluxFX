using System;
using NUnit.Framework;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// GPU Module DefinitionからのOperation生成とExecutionPlanへの統合を検証します。
    /// 旧ExecutionStageによる変換やShader生成には依存しません。
    /// </summary>
    public class FluxParticleOperationCompilerTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;
        FluxParticleOperationCompiler _compiler;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Operation Compiler Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _compiler = new FluxParticleOperationCompiler(new FluxParticleGpuModuleRegistry());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        /// <summary>
        /// Moduleが存在しない場合に空のExecutionPlanが生成されることを確認します。
        /// </summary>
        [Test]
        public void EmptyModulesProduceEmptyExecutionPlan()
        {
            var plan = Compile();

            Assert.AreEqual(0, plan.OperationCount);
        }

        /// <summary>
        /// Gravity DefinitionがVelocity UpdateのOperationを直接生成することを確認します。
        /// </summary>
        [Test]
        public void GravityProducesVelocityUpdateOperation()
        {
            AddModule(new FluxParticleGravityModule());

            var plan = Compile();
            var operations = plan.GetOperations(
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            Assert.AreEqual(1, operations.Count);

            var operation = operations[0];

            Assert.AreEqual("tplab.fluxfx.gravity", operation.ModuleId);
            Assert.AreEqual("apply", operation.OperationId);
            Assert.AreEqual(FluxParticleAttribute.Velocity, operation.ReadAttributes);
            Assert.AreEqual(FluxParticleAttribute.Velocity, operation.WriteAttributes);
            AssertStateRead(operation, FluxParticleAttribute.Velocity, FluxParticleStateReadSource.Working);
        }

        /// <summary>
        /// VelocityOverLifetime DefinitionがPosition UpdateのOperationを生成し、
        /// WorkingとUpdatedの読み取り契約を保持することを確認します。
        /// </summary>
        [Test]
        public void VelocityOverLifetimeProducesPositionUpdateOperation()
        {
            AddModule(new FluxParticleVelocityOverLifetimeModule());

            var plan = Compile();
            var operations = plan.GetOperations(
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update);

            Assert.AreEqual(1, operations.Count);
            Assert.AreEqual("tplab.fluxfx.velocity-over-lifetime", operations[0].ModuleId);
            Assert.AreEqual("apply-position", operations[0].OperationId);
            Assert.AreEqual(4, operations[0].StateReads.Count);

            AssertStateRead(operations[0], FluxParticleAttribute.Position, FluxParticleStateReadSource.Working);
            AssertStateRead(operations[0], FluxParticleAttribute.Age, FluxParticleStateReadSource.Working);
            AssertStateRead(operations[0], FluxParticleAttribute.Velocity, FluxParticleStateReadSource.Updated);
            AssertStateRead(operations[0], FluxParticleAttribute.Lifetime, FluxParticleStateReadSource.Updated);
        }

        /// <summary>
        /// GravityとVelocityOverLifetimeのOperationが異なるTargetに正しく分類されることを確認します。
        /// </summary>
        [Test]
        public void GravityAndVelocityOverLifetimeCompileTogether()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleVelocityOverLifetimeModule());

            var plan = Compile();

            Assert.AreEqual(2, plan.OperationCount);
            Assert.IsTrue(plan.HasOperations(
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update));
            Assert.IsTrue(plan.HasOperations(
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update));
        }

        /// <summary>
        /// Force、Drag、LimitVelocityがWorking Velocityを読み書きすることを確認します。
        /// </summary>
        [Test]
        public void ForceDragAndLimitVelocityUseWorkingVelocity()
        {
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleLimitVelocityModule());

            var operations = Compile().GetOperations(
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            Assert.AreEqual(3, operations.Count);
            Assert.AreEqual("tplab.fluxfx.force", operations[0].ModuleId);
            Assert.AreEqual("tplab.fluxfx.drag", operations[1].ModuleId);
            Assert.AreEqual("tplab.fluxfx.limit-velocity", operations[2].ModuleId);

            foreach (var operation in operations)
            {
                Assert.AreEqual(FluxParticleAttribute.Velocity, operation.ReadAttributes);
                Assert.AreEqual(FluxParticleAttribute.Velocity, operation.WriteAttributes);
                AssertStateRead(operation, FluxParticleAttribute.Velocity, FluxParticleStateReadSource.Working);
            }
        }

        /// <summary>
        /// NoiseとVortexがPrevious PositionとWorking Velocityを参照することを確認します。
        /// </summary>
        [Test]
        public void NoiseAndVortexUsePreviousPosition()
        {
            AddModule(new FluxParticleNoiseModule());
            AddModule(new FluxParticleVortexModule());

            var operations = Compile().GetOperations(
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            Assert.AreEqual(2, operations.Count);
            Assert.AreEqual("tplab.fluxfx.noise", operations[0].ModuleId);
            Assert.AreEqual("tplab.fluxfx.vortex", operations[1].ModuleId);

            foreach (var operation in operations)
            {
                Assert.AreEqual(
                    FluxParticleAttribute.Position | FluxParticleAttribute.Velocity,
                    operation.ReadAttributes);

                Assert.AreEqual(FluxParticleAttribute.Velocity, operation.WriteAttributes);
                AssertStateRead(operation, FluxParticleAttribute.Position, FluxParticleStateReadSource.Previous);
                AssertStateRead(operation, FluxParticleAttribute.Velocity, FluxParticleStateReadSource.Working);
            }
        }

        /// <summary>
        /// 全Velocity ModuleがHLSLの適用順序と同じ順序でExecutionPlanへ登録されることを確認します。
        /// ModuleのAuthoring登録順序には依存しません。
        /// </summary>
        [Test]
        public void AllVelocityModulesAreSortedByExecutionOrder()
        {
            AddModule(new FluxParticleLimitVelocityModule());
            AddModule(new FluxParticleVortexModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleNoiseModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleGravityModule());

            var operations = Compile().GetOperations(
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            Assert.AreEqual(6, operations.Count);

            Assert.AreEqual("tplab.fluxfx.gravity", operations[0].ModuleId);
            Assert.AreEqual("tplab.fluxfx.force", operations[1].ModuleId);
            Assert.AreEqual("tplab.fluxfx.noise", operations[2].ModuleId);
            Assert.AreEqual("tplab.fluxfx.vortex", operations[3].ModuleId);
            Assert.AreEqual("tplab.fluxfx.drag", operations[4].ModuleId);
            Assert.AreEqual("tplab.fluxfx.limit-velocity", operations[5].ModuleId);

            Assert.AreEqual(100, operations[0].Order);
            Assert.AreEqual(200, operations[1].Order);
            Assert.AreEqual(300, operations[2].Order);
            Assert.AreEqual(400, operations[3].Order);
            Assert.AreEqual(500, operations[4].Order);
            Assert.AreEqual(600, operations[5].Order);
        }

        /// <summary>
        /// GPU Definitionが登録されていないRendering Moduleを渡した場合に、
        /// 不完全なExecutionPlanを返さずエラーとなることを確認します。
        /// </summary>
        [Test]
        public void ModuleWithoutGpuDefinitionIsRejected()
        {
            AddModule(new FluxParticleColorOverLifetimeModule());

            Assert.Throws<InvalidOperationException>(() => Compile());
        }

        /// <summary>
        /// AuthoringのModule構成からExecutionPlanを生成します。
        /// </summary>
        FluxParticleExecutionPlan Compile()
        {
            var plan = FluxParticleCompilePlan.Create(_authoring);
            return _compiler.Compile(plan);
        }

        /// <summary>
        /// テスト対象のModuleをAuthoringへ追加します。
        /// </summary>
        void AddModule(FluxParticleModule module)
        {
            var serializedObject = new SerializedObject(_authoring);
            var modules = serializedObject.FindProperty("modules");

            var index = modules.arraySize;
            modules.arraySize++;
            modules.GetArrayElementAtIndex(index).managedReferenceValue = module;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 指定した属性が期待する読み取り時点で宣言されていることを確認します。
        /// </summary>
        static void AssertStateRead(
            FluxParticleOperation operation,
            FluxParticleAttribute attribute,
            FluxParticleStateReadSource source)
        {
            foreach (var read in operation.StateReads)
            {
                if (read.Attribute != attribute) continue;

                Assert.AreEqual(source, read.Source);
                return;
            }

            Assert.Fail($"State read was not found: {attribute}");
        }
    }
}