using System;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// Module拡張によって不要なPersistent StateやGPU Passが増えないことを検証します。
    /// </summary>
    public sealed class FluxParticleCompilerIntegrationTests
    {
        [Test]
        public void ReadOnlyValueAndRenderOperationDoNotIncreasePagesOrPasses()
        {
            var definitions = FluxParticleCoreDefinitions.Create();
            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            var compiler = new FluxParticleProgramCompiler();
            var baseline = compiler.Compile(definitions);

            definitions.AddValue(new FluxParticleValueDefinition("custom.tint", FluxParticleValueType.Float4,
                FluxParticleWritePolicy.ReadOnly, "custom"));
            definitions.AddProvider(new FluxParticleValueProvider("custom.tint", FluxParticleProviderKind.Uniform));
            definitions.AddOperation(new FluxParticleOperationDefinition("render.custom", "custom", FluxParticlePhase.Render,
                new[] { new FluxParticleValueRead("custom.tint", FluxParticleReadVersion.Provided) },
                Array.Empty<FluxParticleValueWrite>(), null, "Custom.hlsl", "Custom"));

            var extended = compiler.Compile(definitions);
            Assert.AreEqual(2, baseline.StateLayout.Pages.Count);
            Assert.AreEqual(2, baseline.ExecutionPlan.Passes.Count);
            Assert.AreEqual(baseline.StateLayout.Pages.Count, extended.StateLayout.Pages.Count);
            Assert.AreEqual(baseline.ExecutionPlan.Passes.Count, extended.ExecutionPlan.Passes.Count);
            Assert.IsFalse(extended.StateLayout.TryGetBinding("custom.tint", out _));
            Assert.IsTrue(extended.Operations.Any(operation => operation.Id == "render.custom"));
        }

    }
}
