using System;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// Operation依存グラフによるWriter接続を検証します。
    /// </summary>
    public sealed class FluxParticleDependencyGraphTests
    {
        [Test]
        public void ResolvesReverseRegistrationOrder()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(Writer("last", "middle"));
            definitions.AddOperation(Writer("first"));
            definitions.AddOperation(Writer("middle", "first"));

            var finalWriter = FluxParticleValuePipelineResolver.Resolve(definitions, "custom.value");

            Assert.AreEqual("last", finalWriter);
            Assert.AreEqual(FluxParticleReadVersion.StepPrevious, definitions.Operations["first"].Reads.Single().Version);
            Assert.AreEqual("first", definitions.Operations["middle"].Reads.Single().ProducerOperationId);
            Assert.AreEqual("middle", definitions.Operations["last"].Reads.Single().ProducerOperationId);
        }

        [Test]
        public void RejectsAmbiguousWriters()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(Writer("first"));
            definitions.AddOperation(Writer("second"));

            var error = Assert.Throws<InvalidOperationException>(() =>
                FluxParticleValuePipelineResolver.Resolve(definitions, "custom.value"));
            StringAssert.Contains("Ambiguous", error.Message);
        }

        [Test]
        public void RejectsCyclicWriters()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(Writer("first", "second"));
            definitions.AddOperation(Writer("second", "first"));

            var error = Assert.Throws<InvalidOperationException>(() =>
                FluxParticleValuePipelineResolver.Resolve(definitions, "custom.value"));
            StringAssert.Contains("Cyclic", error.Message);
        }

        [Test]
        public void NoWritersReturnsNull()
        {
            Assert.IsNull(FluxParticleValuePipelineResolver.Resolve(new FluxParticleDefinitionBuilder(), "custom.value"));
        }

        static FluxParticleOperationDefinition Writer(string id, params string[] dependencies)
        {
            return new FluxParticleOperationDefinition(id, "custom", FluxParticlePhase.Update,
                new[] { new FluxParticleValueRead("custom.value", FluxParticleReadVersion.StepPrevious) },
                new[] { new FluxParticleValueWrite("custom.value", FluxParticleWriteKind.Modify) }, dependencies,
                "Custom.hlsl", "Custom");
        }
    }
}
