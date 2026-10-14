using System;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleOptionalOperationDependencyTests
    {
        [Test]
        public void OptionalConstraintIgnoresAbsentWriter()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(Write("drag"));
            var order = FluxParticleOptionalDependencyResolver.OrderWriters(definitions, "particle.velocity",
                new[] { new FluxParticleOptionalOperationDependency("drag", "gravity") });
            CollectionAssert.AreEqual(new[] { "drag" }, order);
        }

        [Test]
        public void OptionalConstraintReordersReversedRegistration()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(Write("drag"));
            definitions.AddOperation(Write("gravity"));
            var order = FluxParticleOptionalDependencyResolver.OrderWriters(definitions, "particle.velocity",
                new[] { new FluxParticleOptionalOperationDependency("drag", "gravity") });
            CollectionAssert.AreEqual(new[] { "gravity", "drag" }, order);
        }

        [Test]
        public void AmbiguousWritersFail()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(Write("a"));
            definitions.AddOperation(Write("b"));
            Assert.Throws<InvalidOperationException>(() => FluxParticleOptionalDependencyResolver.OrderWriters(definitions,
                "particle.velocity", Array.Empty<FluxParticleOptionalOperationDependency>()));
        }

        [Test]
        public void CircularOptionalConstraintsFail()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(Write("a"));
            definitions.AddOperation(Write("b"));
            Assert.Throws<InvalidOperationException>(() => FluxParticleOptionalDependencyResolver.OrderWriters(definitions,
                "particle.velocity", new[]
                {
                    new FluxParticleOptionalOperationDependency("a", "b"),
                    new FluxParticleOptionalOperationDependency("b", "a")
                }));
        }

        static FluxParticleOperationDefinition Write(string id)
        {
            return new FluxParticleOperationDefinition(id, "test", FluxParticlePhase.Update,
                new[] { new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious) },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Test.hlsl", "Test");
        }
    }
}
