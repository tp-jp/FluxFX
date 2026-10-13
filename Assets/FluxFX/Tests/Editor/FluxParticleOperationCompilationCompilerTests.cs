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
    /// 実際のModule Definitionを使用したOperationCompilationの構築を検証します。
    /// Shader生成やRuntime Bindingの書き出しは検証対象としません。
    /// </summary>
    public class FluxParticleOperationCompilationCompilerTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;
        FluxParticleOperationCompilationCompiler _compiler;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Operation Compilation Compiler Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _compiler = new FluxParticleOperationCompilationCompiler(new FluxParticleGpuModuleRegistry());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        /// <summary>
        /// Moduleが存在しない場合、空のSimulation PassとRendering一覧を生成できることを確認します。
        /// </summary>
        [Test]
        public void EmptyAuthoringProducesEmptyCompilation()
        {
            var compilation = Compile();

            Assert.AreEqual(4, compilation.Passes.Count);
            Assert.IsEmpty(compilation.RenderOperations);

            foreach (var pass in compilation.Passes)
            {
                Assert.IsEmpty(pass.Modules);
                Assert.IsEmpty(pass.SpawnOperations);
                Assert.IsEmpty(pass.UpdateOperations);
            }
        }

        /// <summary>
        /// GravityのOperationがVelocity Passに配置され、
        /// HLSLパスとEntryPointがDefinitionの値と一致することを確認します。
        /// </summary>
        [Test]
        public void GravityProducesCompiledVelocityOperation()
        {
            AddModule(new FluxParticleGravityModule());

            var compilation = Compile();
            var pass = compilation.GetPass(FluxParticleOperationTarget.Velocity);

            Assert.AreEqual(1, pass.UpdateOperations.Count);
            Assert.AreEqual(1, pass.Modules.Count);

            var operation = pass.UpdateOperations[0];

            Assert.AreEqual("tplab.fluxfx.gravity", operation.Operation.ModuleId);
            Assert.AreEqual("FluxFX_Gravity", operation.EntryPoint);
            Assert.AreEqual("Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Gravity.hlsl", operation.HlslPath);
            Assert.AreSame(pass.Modules[0], operation.Module);
        }

        /// <summary>
        /// Moduleのパラメータ値をDefinitionから収集し、
        /// Enabled Uniform名を既存の命名規則に従って生成することを確認します。
        /// </summary>
        [Test]
        public void GravityCollectsParametersAndEnabledUniform()
        {
            AddModule(new FluxParticleGravityModule());

            var module = Compile().GetPass(FluxParticleOperationTarget.Velocity).Modules[0];

            Assert.AreEqual(typeof(FluxParticleGravityModule), module.ModuleType);
            Assert.AreEqual(1, module.Parameters.Count);
            Assert.AreEqual("_Gravity", module.Parameters[0].Name);
            Assert.AreEqual(
                FluxParticleGpuModuleEnabled.GetParameterName("tplab.fluxfx.gravity"),
                module.EnabledParameterName);
        }

        /// <summary>
        /// 6種類のVelocity ModuleとVelocityOverLifetimeを、
        /// 対応する2つのSimulation Passに配置できることを確認します。
        /// </summary>
        [Test]
        public void AllGpuModulesCompileIntoExpectedPasses()
        {
            AddModule(new FluxParticleLimitVelocityModule());
            AddModule(new FluxParticleVortexModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleNoiseModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleVelocityOverLifetimeModule());

            var compilation = Compile();
            var velocity = compilation.GetPass(FluxParticleOperationTarget.Velocity);
            var position = compilation.GetPass(FluxParticleOperationTarget.Position);

            Assert.AreEqual(6, velocity.UpdateOperations.Count);
            Assert.AreEqual(6, velocity.Modules.Count);
            Assert.AreEqual(1, position.UpdateOperations.Count);
            Assert.AreEqual(1, position.Modules.Count);

            Assert.AreEqual("tplab.fluxfx.gravity", velocity.UpdateOperations[0].Operation.ModuleId);
            Assert.AreEqual("tplab.fluxfx.force", velocity.UpdateOperations[1].Operation.ModuleId);
            Assert.AreEqual("tplab.fluxfx.noise", velocity.UpdateOperations[2].Operation.ModuleId);
            Assert.AreEqual("tplab.fluxfx.vortex", velocity.UpdateOperations[3].Operation.ModuleId);
            Assert.AreEqual("tplab.fluxfx.drag", velocity.UpdateOperations[4].Operation.ModuleId);
            Assert.AreEqual("tplab.fluxfx.limit-velocity", velocity.UpdateOperations[5].Operation.ModuleId);
            Assert.AreEqual("tplab.fluxfx.velocity-over-lifetime", position.UpdateOperations[0].Operation.ModuleId);
        }

        /// <summary>
        /// Rendering ModuleがExecutionPlanに残り、
        /// Simulation用Passには追加されないことを確認します。
        /// </summary>
        [Test]
        public void RenderingModulesDoNotProduceSimulationBindings()
        {
            AddModule(new FluxParticleColorOverLifetimeModule());
            AddModule(new FluxParticleSizeOverLifetimeModule());
            AddModule(new FluxParticleRotationOverLifetimeModule());
            AddModule(new FluxParticleTextureSheetAnimationModule());

            var compilation = Compile();

            Assert.AreEqual(4, compilation.RenderOperations.Count);
            Assert.AreEqual(4, compilation.ExecutionPlan.OperationCount);

            foreach (var pass in compilation.Passes)
            {
                Assert.IsEmpty(pass.Modules);
                Assert.IsEmpty(pass.SpawnOperations);
                Assert.IsEmpty(pass.UpdateOperations);
            }
        }

        /// <summary>
        /// GPU ModuleとRendering Moduleの混在構成で、
        /// 単一のExecutionPlanと適切なSimulation Passを生成できることを確認します。
        /// </summary>
        [Test]
        public void SimulationAndRenderingCompileTogether()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleVelocityOverLifetimeModule());
            AddModule(new FluxParticleColorOverLifetimeModule());
            AddModule(new FluxParticleTextureSheetAnimationModule());

            var compilation = Compile();

            Assert.AreEqual(4, compilation.ExecutionPlan.OperationCount);
            Assert.AreEqual(2, compilation.RenderOperations.Count);
            Assert.AreEqual(1, compilation.GetPass(FluxParticleOperationTarget.Velocity).UpdateOperations.Count);
            Assert.AreEqual(1, compilation.GetPass(FluxParticleOperationTarget.Position).UpdateOperations.Count);
        }

        /// <summary>
        /// Shader実装とModule情報を同じCompilation内に保持し、
        /// 複数PassでModuleパラメータの実体を重複生成しない設計を確認します。
        /// </summary>
        [Test]
        public void CompiledOperationsReferenceTheirPassModules()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleVelocityOverLifetimeModule());

            var compilation = Compile();

            foreach (var pass in compilation.Passes)
            {
                foreach (var operation in pass.UpdateOperations)
                {
                    Assert.Contains(operation.Module, (System.Collections.ICollection)pass.Modules);
                }
            }
        }

        /// <summary>
        /// nullのCompilePlanを受け取った場合に例外が発生することを確認します。
        /// </summary>
        [Test]
        public void NullCompilePlanIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => _compiler.Compile(null));
        }

        /// <summary>
        /// AuthoringのModule設定からOperationCompilationを生成します。
        /// </summary>
        FluxParticleOperationCompilation Compile()
        {
            var plan = FluxParticleCompilePlan.Create(_authoring);
            return _compiler.Compile(plan);
        }

        /// <summary>
        /// テスト用ModuleをAuthoringに追加します。
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
    }
}