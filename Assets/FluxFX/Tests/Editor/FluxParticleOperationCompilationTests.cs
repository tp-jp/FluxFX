using System;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Execution;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// Operation単位のGPU CompilationとPassへの配置を検証します。
    /// Shader生成、Uniform値の書き出し、GPU実行は検証対象としません。
    /// </summary>
    public class FluxParticleOperationCompilationTests
    {
        /// <summary>
        /// 空のExecutionPlanから、Simulation用の4つのPassを生成できることを確認します。
        /// </summary>
        [Test]
        public void EmptyPlanProducesEmptyPasses()
        {
            var plan = FluxParticleExecutionPlan.Create(Array.Empty<FluxParticleOperation>());
            var compilation = FluxParticleOperationCompilation.Create(
                plan,
                Array.Empty<FluxParticleCompiledOperation>());

            Assert.AreEqual(4, compilation.Passes.Count);
            Assert.IsEmpty(compilation.RenderOperations);

            foreach (var pass in compilation.Passes)
            {
                Assert.IsEmpty(pass.SpawnOperations);
                Assert.IsEmpty(pass.UpdateOperations);
                Assert.IsEmpty(pass.Modules);
            }
        }

        /// <summary>
        /// SpawnとUpdateのOperationを同じPassに配置し、実行Phaseごとに分離できることを確認します。
        /// </summary>
        [Test]
        public void SpawnAndUpdateOperationsSharePass()
        {
            var spawn = CreateOperation(
                "custom",
                "spawn",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Spawn);

            var update = CreateOperation(
                "custom",
                "update",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            var module = CreateModule("custom");
            var compilation = Compile(
                new[] { spawn, update },
                new[]
                {
                    Bind(spawn, module, "FluxFX_Spawn"),
                    Bind(update, module, "FluxFX_Update")
                });

            var pass = compilation.GetPass(FluxParticleOperationTarget.Velocity);

            Assert.AreEqual(1, pass.SpawnOperations.Count);
            Assert.AreEqual(1, pass.UpdateOperations.Count);
            Assert.AreEqual(1, pass.Modules.Count);
            Assert.AreSame(module, pass.Modules[0]);
            Assert.AreEqual("FluxFX_Spawn", pass.SpawnOperations[0].EntryPoint);
            Assert.AreEqual("FluxFX_Update", pass.UpdateOperations[0].EntryPoint);
        }

        /// <summary>
        /// 同じModuleのOperationを複数Passに配置してもModule情報を共有できることを確認します。
        /// </summary>
        [Test]
        public void OneModuleCanProduceOperationsAcrossPasses()
        {
            var velocity = CreateOperation(
                "custom",
                "velocity",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            var position = CreateOperation(
                "custom",
                "position",
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update);

            var module = CreateModule("custom");
            var compilation = Compile(
                new[] { velocity, position },
                new[]
                {
                    Bind(velocity, module, "FluxFX_UpdateVelocity"),
                    Bind(position, module, "FluxFX_UpdatePosition")
                });

            var velocityPass = compilation.GetPass(FluxParticleOperationTarget.Velocity);
            var positionPass = compilation.GetPass(FluxParticleOperationTarget.Position);

            Assert.AreEqual(1, velocityPass.UpdateOperations.Count);
            Assert.AreEqual(1, positionPass.UpdateOperations.Count);
            Assert.AreSame(module, velocityPass.Modules[0]);
            Assert.AreSame(module, positionPass.Modules[0]);
        }

        /// <summary>
        /// Rendering OperationをSimulation用Shader Compilationから分離できることを確認します。
        /// </summary>
        [Test]
        public void RenderingOperationIsStoredSeparately()
        {
            var render = new FluxParticleOperation(
                "render",
                "evaluate",
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render,
                FluxParticleAttribute.None,
                FluxParticleAttribute.None,
                100);

            var compilation = Compile(
                new[] { render },
                Array.Empty<FluxParticleCompiledOperation>());

            Assert.AreEqual(1, compilation.RenderOperations.Count);
            Assert.AreSame(render, compilation.RenderOperations[0]);

            foreach (var pass in compilation.Passes)
            {
                Assert.IsEmpty(pass.UpdateOperations);
                Assert.IsEmpty(pass.SpawnOperations);
            }
        }

        /// <summary>
        /// ExecutionPlanに存在しないGPU Operationの登録を拒否することを確認します。
        /// </summary>
        [Test]
        public void UnknownCompiledOperationIsRejected()
        {
            var expected = CreateOperation(
                "custom",
                "expected",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            var unknown = CreateOperation(
                "custom",
                "unknown",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            Assert.Throws<InvalidOperationException>(() =>
                Compile(
                    new[] { expected },
                    new[] { Bind(unknown, CreateModule("custom"), "FluxFX_Unknown") }));
        }

        /// <summary>
        /// Shader実装が存在しないSimulation Operationを拒否することを確認します。
        /// </summary>
        [Test]
        public void MissingShaderImplementationIsRejected()
        {
            var operation = CreateOperation(
                "custom",
                "update",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            Assert.Throws<InvalidOperationException>(() =>
                Compile(
                    new[] { operation },
                    Array.Empty<FluxParticleCompiledOperation>()));
        }

        /// <summary>
        /// 同一Operationに複数のShader実装を登録することを拒否することを確認します。
        /// </summary>
        [Test]
        public void DuplicateCompiledOperationIsRejected()
        {
            var operation = CreateOperation(
                "custom",
                "update",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            var module = CreateModule("custom");
            var binding = Bind(operation, module, "FluxFX_Update");

            Assert.Throws<InvalidOperationException>(() =>
                Compile(new[] { operation }, new[] { binding, binding }));
        }

        /// <summary>
        /// 同一ModuleIdを持つOperationが異なるModuleCompilationを参照することを拒否します。
        /// </summary>
        [Test]
        public void ModuleCompilationMustBeShared()
        {
            var first = CreateOperation(
                "custom",
                "first",
                FluxParticleOperationTarget.Velocity,
                FluxParticleOperationPhase.Update);

            var second = CreateOperation(
                "custom",
                "second",
                FluxParticleOperationTarget.Position,
                FluxParticleOperationPhase.Update);

            Assert.Throws<InvalidOperationException>(() =>
                Compile(
                    new[] { first, second },
                    new[]
                    {
                        Bind(first, CreateModule("custom"), "FluxFX_First"),
                        Bind(second, CreateModule("custom"), "FluxFX_Second")
                    }));
        }

        /// <summary>
        /// Rendering OperationをSimulation用Shader実装として登録できないことを確認します。
        /// </summary>
        [Test]
        public void RenderingCannotHaveSimulationShaderBinding()
        {
            var operation = new FluxParticleOperation(
                "render",
                "evaluate",
                FluxParticleOperationTarget.Rendering,
                FluxParticleOperationPhase.Render,
                FluxParticleAttribute.None,
                FluxParticleAttribute.None,
                100);

            Assert.Throws<ArgumentException>(() =>
                Bind(operation, CreateModule("render"), "FluxFX_Render"));
        }

        /// <summary>
        /// テスト用のOperationを生成します。
        /// </summary>
        static FluxParticleOperation CreateOperation(
            string moduleId,
            string operationId,
            FluxParticleOperationTarget target,
            FluxParticleOperationPhase phase)
        {
            return new FluxParticleOperation(
                moduleId,
                operationId,
                target,
                phase,
                FluxParticleAttribute.None,
                FluxParticleAttribute.None,
                100);
        }

        /// <summary>
        /// テスト用のModule Compilationを生成します。
        /// </summary>
        static FluxParticleCompiledModule CreateModule(string moduleId)
        {
            return new FluxParticleCompiledModule(
                moduleId,
                typeof(FluxParticleGravityModule),
                Array.Empty<FluxParticleGpuParameterCompilation>(),
                "_TestEnabled");
        }

        /// <summary>
        /// OperationとShader実装のテスト用Bindingを生成します。
        /// </summary>
        static FluxParticleCompiledOperation Bind(
            FluxParticleOperation operation,
            FluxParticleCompiledModule module,
            string entryPoint)
        {
            return new FluxParticleCompiledOperation(
                operation,
                module,
                "Assets/Tests/Custom.hlsl",
                entryPoint);
        }

        /// <summary>
        /// テスト用のExecutionPlanとShader BindingからCompilationを生成します。
        /// </summary>
        static FluxParticleOperationCompilation Compile(
            FluxParticleOperation[] operations,
            FluxParticleCompiledOperation[] bindings)
        {
            var plan = FluxParticleExecutionPlan.Create(operations);

            return FluxParticleOperationCompilation.Create(plan, bindings);
        }
    }
}