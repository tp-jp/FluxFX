using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Editor.Compilation.Modules;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// 新Program BackendのModule登録とContributionの統合を検証します。
    /// </summary>
    public sealed class FluxParticleModuleRegistryTests
    {
        [Test]
        public void CustomModuleContributesWithoutChangesToCoreDefinitions()
        {
            var definitions = FluxParticleCoreDefinitions.Create();
            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            var pageCount = new FluxParticleProgramCompiler().Compile(definitions).StateLayout.Pages.Count;
            var context = new FluxParticleProgramContribution(definitions);
            var registry = new FluxParticleModuleRegistry(new IFluxParticleModuleDefinition[] { new TestTintDefinition() });
            registry.Contribute(new FluxParticleModule[] { new TestTintModule() }, context);

            Assert.IsTrue(registry.TryGet(typeof(TestTintModule), out var definition));
            Assert.AreEqual(TestTintDefinition.Id, definition.ModuleId);
            Assert.AreEqual(new Vector4(0.2f, 0.4f, 0.6f, 1f), context.InitialValues["test.tint"]);
            var operation = definitions.Operations["test.render-tint"];
            Assert.AreEqual(FluxParticlePhase.Render, operation.Phase);
            CollectionAssert.Contains(operation.Parameters.ToArray(), "test.tint");
            Assert.AreEqual(TestTintDefinition.Id, operation.OwnerId);

            var program = new FluxParticleProgramCompiler().Compile(definitions);
            Assert.AreEqual(pageCount, program.StateLayout.Pages.Count);
            Assert.IsFalse(program.StateLayout.TryGetBinding("render.test-tint", out _));
            Assert.IsTrue(program.Operations.Any(value => value.Id == "test.render-tint"));

            var shader = new FluxParticleProgramShaderGenerator().Generate(definitions, "Hidden/FluxFX/Tests/Module", FluxParticleCoreDefinitions.CreateLifecycle());
            StringAssert.Contains("Assets/FluxFX/Tests/Editor/Hlsl/TestRenderTint.hlsl", shader.RenderSource);
            StringAssert.Contains("FluxFX_TestRenderTint", shader.RenderSource);
        }

        [Test]
        public void AutomaticDiscoveryFindsCustomDefinition()
        {
            var registry = new FluxParticleModuleRegistry();
            Assert.IsTrue(registry.TryGet(typeof(TestTintModule), out var definition));
            Assert.AreEqual(TestTintDefinition.Id, definition.ModuleId);
        }

        [Test]
        public void RejectsDuplicateModuleRegistrations()
        {
            Assert.Throws<InvalidOperationException>(() => new FluxParticleModuleRegistry(new IFluxParticleModuleDefinition[]
            {
                new TestTintDefinition(), new TestTintDefinition()
            }));
            Assert.Throws<InvalidOperationException>(() => new FluxParticleModuleRegistry(new IFluxParticleModuleDefinition[]
            {
                new TestTintDefinition(), new TestForeignOwnerDefinition(TestTintDefinition.Id)
            }));
        }

        [Test]
        public void RejectsDuplicateOrUnknownAuthoringModules()
        {
            var registry = new FluxParticleModuleRegistry(new IFluxParticleModuleDefinition[] { new TestTintDefinition() });
            var context = new FluxParticleProgramContribution(FluxParticleCoreDefinitions.Create());
            Assert.Throws<InvalidOperationException>(() => registry.Contribute(new FluxParticleModule[]
            {
                new TestTintModule(), new TestTintModule()
            }, context));
            Assert.Throws<InvalidOperationException>(() => registry.Contribute(new FluxParticleModule[]
            {
                new TestUnknownModule()
            }, new FluxParticleProgramContribution(FluxParticleCoreDefinitions.Create())));
        }

        [Test]
        public void RejectsNonFiniteInitialValueAndForeignOwnedOperation()
        {
            var context = new FluxParticleProgramContribution(new FluxParticleDefinitionBuilder());
            Assert.Throws<InvalidOperationException>(() => context.AddParameter(
                new FluxParticleParameterDefinition("test.invalid", FluxParticleValueType.Float, FluxParticleParameterPolicy.Static),
                new Vector4(float.NaN, 0, 0, 0)));
            Assert.IsFalse(context.Parameters.ContainsKey("test.invalid"));
            var registry = new FluxParticleModuleRegistry(new IFluxParticleModuleDefinition[] { new TestForeignOwnerDefinition() });
            Assert.Throws<InvalidOperationException>(() => registry.Contribute(new FluxParticleModule[]
            {
                new TestUnknownModule()
            }, context));
        }

        public sealed class TestTintModule : FluxParticleModule
        {
            public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.Rendering;
            public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.None;
            public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.None;
        }

        public sealed class TestUnknownModule : FluxParticleModule
        {
            public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.Rendering;
            public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.None;
            public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.None;
        }

        public sealed class TestTintDefinition : IFluxParticleModuleDefinition
        {
            public const string Id = "tplab.fluxfx.tests.tint";
            public Type ModuleType => typeof(TestTintModule);
            public string ModuleId => Id;

            public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
            {
                context.AddValue(new FluxParticleValueDefinition("render.test-tint", FluxParticleValueType.Float4,
                    FluxParticleWritePolicy.OwnerManaged, Id));
                context.AddParameter(new FluxParticleParameterDefinition("test.tint", FluxParticleValueType.Float4,
                    FluxParticleParameterPolicy.Static), new Vector4(0.2f, 0.4f, 0.6f, 1f));
                context.AddOperation(new FluxParticleOperationDefinition("test.render-tint", Id, FluxParticlePhase.Render,
                    Array.Empty<FluxParticleValueRead>(),
                    new[] { new FluxParticleValueWrite("render.test-tint", FluxParticleWriteKind.Replace) }, null,
                    "Assets/FluxFX/Tests/Editor/Hlsl/TestRenderTint.hlsl", "FluxFX_TestRenderTint", new[] { "test.tint" }));
            }
        }

        public sealed class TestForeignOwnerDefinition : IFluxParticleModuleDefinition
        {
            public Type ModuleType => typeof(TestUnknownModule);
            public string ModuleId { get; }

            public TestForeignOwnerDefinition()
                : this("tplab.fluxfx.tests.foreign-owner")
            {
            }

            public TestForeignOwnerDefinition(string moduleId)
            {
                ModuleId = moduleId;
            }

            public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
            {
                context.AddOperation(new FluxParticleOperationDefinition("test.invalid-owner", "fluxfx.core", FluxParticlePhase.Render,
                    Array.Empty<FluxParticleValueRead>(), Array.Empty<FluxParticleValueWrite>(), null,
                    "Assets/FluxFX/Tests/Editor/Hlsl/TestRenderTint.hlsl", "FluxFX_TestRenderTint"));
            }
        }
    }
}
