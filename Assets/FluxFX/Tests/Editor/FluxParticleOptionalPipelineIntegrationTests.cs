using System;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleOptionalPipelineIntegrationTests
    {
        [TestCase(false, false, "velocity.identity")]
        [TestCase(true, false, "gravity")]
        [TestCase(false, true, "drag")]
        [TestCase(true, true, "drag")]
        public void VelocityPipelineResolvesModulePresence(bool gravity, bool drag, string producer)
        {
            var definitions = new FluxParticleDefinitionBuilder();
            if (drag) definitions.AddOperation(Write("drag"));
            if (gravity) definitions.AddOperation(Write("gravity"));
            if (drag) definitions.AddOptionalDependency(new FluxParticleOptionalOperationDependency("drag", "gravity"));

            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            var motion = definitions.Operations["motion"];
            Assert.AreEqual(producer, motion.Reads.Single(read => read.ValueId == "particle.velocity").ProducerOperationId);
            if (gravity && drag)
            {
                Assert.AreEqual("gravity", definitions.Operations["drag"].Reads.Single().ProducerOperationId);
                CollectionAssert.Contains(definitions.Operations["drag"].Dependencies.ToArray(), "gravity");
            }
        }

        [Test]
        public void OptionalDependencyDoesNotHideAmbiguousWriters()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(Write("gravity"));
            definitions.AddOperation(Write("custom"));
            Assert.Throws<InvalidOperationException>(() => FluxParticleVelocityPipelineResolver.Resolve(definitions));
        }

        [Test]
        public void OptionalDependencyCycleIsRejected()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(Write("gravity"));
            definitions.AddOperation(Write("drag"));
            definitions.AddOptionalDependency(new FluxParticleOptionalOperationDependency("drag", "gravity"));
            definitions.AddOptionalDependency(new FluxParticleOptionalOperationDependency("gravity", "drag"));
            Assert.Throws<InvalidOperationException>(() => FluxParticleVelocityPipelineResolver.Resolve(definitions));
        }

        static FluxParticleOperationDefinition Write(string id)
        {
            return new FluxParticleOperationDefinition(id, "test", FluxParticlePhase.Update,
                new[] { new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious) },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Custom.hlsl", "Custom");
        }
    }
}
