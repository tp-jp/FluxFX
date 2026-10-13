using System;
using NUnit.Framework;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Rendering;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// GPU SimulationとRenderingのModule DefinitionからOperationを生成し、
    /// 単一のExecutionPlanへ統合できることを検証します。
    /// Shader生成およびRuntimeでの描画結果は検証対象としません。
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
        /// Moduleが存在しない場合に空のExecutionPlanを生成できることを確認します。
        /// </summary>
        [Test]
        public void EmptyModulesProduceEmptyExecutionPlan()
        {
            var plan = Compile();

            Assert.AreEqual(0, plan.OperationCount);
        }

        /// <summary>
        /// Gravity DefinitionからVelocity Update Operationが生成されることを確認します。
        /// </summary>
        [Test]
        public void GravityProducesVelocityUpdateOperation()
        {
            AddModule(new FluxParticleGravityModule());

            var operations = Compile().GetOperations(
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            Assert.AreEqual(1, operations.Count);
            Assert.AreEqual("tplab.fluxfx.gravity", operations[0].ModuleId);
            Assert.AreEqual("apply", operations[0].OperationId);
            Assert.AreEqual(FluxParticleAttribute.Velocity, operations[0].ReadAttributes);
            Assert.AreEqual(FluxParticleAttribute.Velocity, operations[0].WriteAttributes);

            AssertStateRead(
                operations[0],
                FluxParticleAttribute.Velocity,
                FluxParticleStateReadSource.Working);
        }

        /// <summary>
        /// VelocityOverLifetimeがPosition Update Operationを生成し、
        /// WorkingとUpdatedの読み取り契約を保持することを確認します。
        /// </summary>
        [Test]
        public void VelocityOverLifetimeProducesPositionUpdateOperation()
        {
            AddModule(new FluxParticleVelocityOverLifetimeModule());

            var operations = Compile().GetOperations(
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
        /// GravityとVelocityOverLifetimeのOperationを異なるTargetへ分類できることを確認します。
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
        /// 全Velocity ModuleがAuthoringの登録順ではなくOrderに従って並ぶことを確認します。
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
        /// Rendering Definitionが4種類すべて自動登録されていることを確認します。
        /// </summary>
        [Test]
        public void RenderDefinitionsAreRegistered()
        {
            var registry = new FluxParticleRenderModuleRegistry();

            Assert.AreEqual(4, registry.Count);
            Assert.IsTrue(registry.TryGet(typeof(FluxParticleColorOverLifetimeModule), out _));
            Assert.IsTrue(registry.TryGet(typeof(FluxParticleSizeOverLifetimeModule), out _));
            Assert.IsTrue(registry.TryGet(typeof(FluxParticleRotationOverLifetimeModule), out _));
            Assert.IsTrue(registry.TryGet(typeof(FluxParticleTextureSheetAnimationModule), out _));
        }

        /// <summary>
        /// ColorOverLifetimeがAge、Lifetime、StartColorを参照するRendering Operationを生成することを確認します。
        /// </summary>
        [Test]
        public void ColorOverLifetimeProducesRenderOperation()
        {
            AddModule(new FluxParticleColorOverLifetimeModule());

            var operation = GetSingleRenderOperation();

            Assert.AreEqual("tplab.fluxfx.color-over-lifetime", operation.ModuleId);
            Assert.AreEqual("evaluate", operation.OperationId);
            Assert.AreEqual(
                FluxParticleAttribute.Age |
                FluxParticleAttribute.Lifetime |
                FluxParticleAttribute.StartColor,
                operation.ReadAttributes);

            AssertUpdatedReads(operation,
                FluxParticleAttribute.Age,
                FluxParticleAttribute.Lifetime,
                FluxParticleAttribute.StartColor);
        }

        /// <summary>
        /// SizeOverLifetimeがAge、Lifetime、StartSizeを参照するRendering Operationを生成することを確認します。
        /// </summary>
        [Test]
        public void SizeOverLifetimeProducesRenderOperation()
        {
            AddModule(new FluxParticleSizeOverLifetimeModule());

            var operation = GetSingleRenderOperation();

            Assert.AreEqual("tplab.fluxfx.size-over-lifetime", operation.ModuleId);
            Assert.AreEqual(
                FluxParticleAttribute.Age |
                FluxParticleAttribute.Lifetime |
                FluxParticleAttribute.StartSize,
                operation.ReadAttributes);

            AssertUpdatedReads(operation,
                FluxParticleAttribute.Age,
                FluxParticleAttribute.Lifetime,
                FluxParticleAttribute.StartSize);
        }

        /// <summary>
        /// RotationOverLifetimeがAge、Lifetime、StartRotationを参照するRendering Operationを生成することを確認します。
        /// </summary>
        [Test]
        public void RotationOverLifetimeProducesRenderOperation()
        {
            AddModule(new FluxParticleRotationOverLifetimeModule());

            var operation = GetSingleRenderOperation();

            Assert.AreEqual("tplab.fluxfx.rotation-over-lifetime", operation.ModuleId);
            Assert.AreEqual(
                FluxParticleAttribute.Age |
                FluxParticleAttribute.Lifetime |
                FluxParticleAttribute.StartRotation,
                operation.ReadAttributes);

            AssertUpdatedReads(operation,
                FluxParticleAttribute.Age,
                FluxParticleAttribute.Lifetime,
                FluxParticleAttribute.StartRotation);
        }

        /// <summary>
        /// TextureSheetAnimationがAgeとLifetimeを参照するRendering Operationを生成することを確認します。
        /// </summary>
        [Test]
        public void TextureSheetAnimationProducesRenderOperation()
        {
            AddModule(new FluxParticleTextureSheetAnimationModule());

            var operation = GetSingleRenderOperation();

            Assert.AreEqual("tplab.fluxfx.texture-sheet-animation", operation.ModuleId);
            Assert.AreEqual(
                FluxParticleAttribute.Age | FluxParticleAttribute.Lifetime,
                operation.ReadAttributes);

            AssertUpdatedReads(operation,
                FluxParticleAttribute.Age,
                FluxParticleAttribute.Lifetime);
        }

        /// <summary>
        /// 4種類のRendering Moduleが同一ExecutionPlanに登録できることを確認します。
        /// </summary>
        [Test]
        public void AllRenderingModulesCompileTogether()
        {
            AddModule(new FluxParticleTextureSheetAnimationModule());
            AddModule(new FluxParticleRotationOverLifetimeModule());
            AddModule(new FluxParticleSizeOverLifetimeModule());
            AddModule(new FluxParticleColorOverLifetimeModule());

            var operations = Compile().GetOperations(
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render);

            Assert.AreEqual(4, operations.Count);

            foreach (var operation in operations)
            {
                Assert.AreEqual(FluxParticleAttribute.None, operation.WriteAttributes);
            }

            Assert.AreEqual("tplab.fluxfx.color-over-lifetime", operations[0].ModuleId);
            Assert.AreEqual("tplab.fluxfx.rotation-over-lifetime", operations[1].ModuleId);
            Assert.AreEqual("tplab.fluxfx.size-over-lifetime", operations[2].ModuleId);
            Assert.AreEqual("tplab.fluxfx.texture-sheet-animation", operations[3].ModuleId);
        }

        /// <summary>
        /// GPU SimulationとRenderingのModuleを単一のExecutionPlanへ統合できることを確認します。
        /// </summary>
        [Test]
        public void SimulationAndRenderingModulesCompileTogether()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleVelocityOverLifetimeModule());
            AddModule(new FluxParticleColorOverLifetimeModule());
            AddModule(new FluxParticleTextureSheetAnimationModule());

            var plan = Compile();

            Assert.AreEqual(4, plan.OperationCount);

            Assert.AreEqual(1, plan.GetOperations(
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update).Count);

            Assert.AreEqual(1, plan.GetOperations(
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update).Count);

            Assert.AreEqual(2, plan.GetOperations(
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render).Count);
        }

        /// <summary>
        /// Moduleが属する実行系とは異なるRegistryに登録されていないことを確認します。
        /// </summary>
        [Test]
        public void GpuAndRenderRegistriesAreSeparate()
        {
            var gpuRegistry = new FluxParticleGpuModuleRegistry();
            var renderRegistry = new FluxParticleRenderModuleRegistry();

            Assert.IsTrue(gpuRegistry.TryGet(typeof(FluxParticleGravityModule), out _));
            Assert.IsFalse(gpuRegistry.TryGet(typeof(FluxParticleColorOverLifetimeModule), out _));

            Assert.IsTrue(renderRegistry.TryGet(typeof(FluxParticleColorOverLifetimeModule), out _));
            Assert.IsFalse(renderRegistry.TryGet(typeof(FluxParticleGravityModule), out _));
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
        /// 単一Rendering ModuleのOperationを取得します。
        /// </summary>
        FluxParticleOperation GetSingleRenderOperation()
        {
            var operations = Compile().GetOperations(
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render);

            Assert.AreEqual(1, operations.Count);
            Assert.AreEqual(FluxParticleAttribute.None, operations[0].WriteAttributes);

            return operations[0];
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

        /// <summary>
        /// Rendering Operationの読み取り属性がすべてUpdatedであることを確認します。
        /// </summary>
        static void AssertUpdatedReads(
            FluxParticleOperation operation,
            params FluxParticleAttribute[] attributes)
        {
            Assert.AreEqual(attributes.Length, operation.StateReads.Count);

            foreach (var attribute in attributes)
            {
                AssertStateRead(
                    operation,
                    attribute,
                    FluxParticleStateReadSource.Updated);
            }
        }
    }
}