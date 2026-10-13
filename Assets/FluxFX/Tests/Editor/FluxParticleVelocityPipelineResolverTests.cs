using System;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// Velocity Moduleの構成に応じたMotionの入力解決を検証します。
    /// </summary>
    public class FluxParticleVelocityPipelineResolverTests
    {
        [Test]
        public void NoVelocityModuleUsesStepPrevious()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            var motion = definitions.Operations["motion"];
            var read = motion.Reads.Single(value => value.ValueId == "particle.velocity");
            Assert.AreEqual(FluxParticleReadVersion.AfterOperation, read.Version);
            Assert.AreEqual("velocity.identity", read.ProducerOperationId);
        }

        [Test]
        public void VelocityModuleConnectsToMotion()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(CreateVelocity("custom.velocity"));
            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            var read = definitions.Operations["motion"].Reads.Single(value => value.ValueId == "particle.velocity");
            Assert.AreEqual(FluxParticleReadVersion.AfterOperation, read.Version);
            Assert.AreEqual("custom.velocity", read.ProducerOperationId);
        }

        [Test]
        public void MultipleVelocityModulesAreChained()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(CreateVelocity("custom.z"));
            definitions.AddOperation(CreateVelocity("custom.a"));
            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            Assert.AreEqual("custom.z", definitions.Operations["custom.a"].Reads.Single().ProducerOperationId);
            Assert.AreEqual("custom.a", definitions.Operations["motion"].Reads.First().ProducerOperationId);
        }

        static FluxParticleOperationDefinition CreateVelocity(string id)
        {
            return new FluxParticleOperationDefinition(id, "custom", FluxParticlePhase.Update,
                new[] { new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious) },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Custom.hlsl", "Custom");
        }
    }
}
