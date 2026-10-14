using System;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Editor.Compilation.Modules;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// Module ContributionからCompilerとShader生成までの登録順非依存性を確認します。
    /// </summary>
    public sealed class FluxParticleModuleCompilationIntegrationTests
    {
        [Test]
        public void CustomModuleCompilationIsIndependentOfCoreRegistrationOrder()
        {
            var standard = FluxParticleBasicProgram.CreateDefinitions();
            var ordered = CreateWithTint(standard, false);
            var reversed = CreateWithTint(standard, true);

            var compiler = new FluxParticleProgramCompiler();
            var first = compiler.Compile(ordered);
            var second = compiler.Compile(reversed);

            Assert.AreEqual(first.StateLayout.Pages.Count, second.StateLayout.Pages.Count);
            Assert.AreEqual(first.ExecutionPlan.Passes.Count, second.ExecutionPlan.Passes.Count);
            foreach (var value in standard.Values.Keys)
            {
                var hasFirst = first.StateLayout.TryGetBinding(value, out var firstBinding);
                var hasSecond = second.StateLayout.TryGetBinding(value, out var secondBinding);
                Assert.AreEqual(hasFirst, hasSecond, value);
                if (!hasFirst) continue;
                Assert.AreEqual(firstBinding.PageId, secondBinding.PageId, value);
                Assert.AreEqual(firstBinding.ChannelOffset, secondBinding.ChannelOffset, value);
                Assert.AreEqual(firstBinding.ComponentCount, secondBinding.ComponentCount, value);
            }
            for (var index = 0; index < first.ExecutionPlan.Passes.Count; index++)
            {
                var a = first.ExecutionPlan.Passes[index];
                var b = second.ExecutionPlan.Passes[index];
                Assert.AreEqual(a.OutputPageId, b.OutputPageId);
                CollectionAssert.AreEqual(a.SpawnOperations.Select(operation => operation.Id).ToArray(),
                    b.SpawnOperations.Select(operation => operation.Id).ToArray());
                CollectionAssert.AreEqual(a.UpdateOperations.Select(operation => operation.Id).ToArray(),
                    b.UpdateOperations.Select(operation => operation.Id).ToArray());
            }

            var generator = new FluxParticleProgramShaderGenerator();
            var firstShader = generator.Generate(ordered, "Hidden/FluxFX/Tests/ModuleOrder", FluxParticleBasicProgram.CreateLifecycle());
            var secondShader = generator.Generate(reversed, "Hidden/FluxFX/Tests/ModuleOrder", FluxParticleBasicProgram.CreateLifecycle());
            Assert.AreEqual(firstShader.RenderSource, secondShader.RenderSource);
        }

        static FluxParticleDefinitionBuilder CreateWithTint(FluxParticleDefinitionBuilder source, bool reverse)
        {
            var definitions = new FluxParticleDefinitionBuilder();
            foreach (var value in source.Values.Values) definitions.AddValue(value);
            foreach (var parameter in source.Parameters.Values) definitions.AddParameter(parameter);
            foreach (var provider in source.Providers.Values) definitions.AddProvider(provider);
            var operations = reverse ? source.Operations.Values.Reverse() : source.Operations.Values;
            foreach (var operation in operations) definitions.AddOperation(operation);

            var context = new FluxParticleProgramContribution(definitions);
            var registry = new FluxParticleModuleRegistry(new IFluxParticleModuleDefinition[]
            {
                new FluxParticleModuleRegistryTests.TestTintDefinition()
            });
            registry.Contribute(new FluxParticleModule[] { new FluxParticleModuleRegistryTests.TestTintModule() }, context);
            return definitions;
        }
    }
}
