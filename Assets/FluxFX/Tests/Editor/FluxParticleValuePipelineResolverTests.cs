using System;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// 特定のValue名に依存しないUpdate Operationの接続を検証します。
    /// </summary>
    public sealed class FluxParticleValuePipelineResolverTests
    {
        [Test]
        public void ArbitraryValueWritersUseExplicitOrder()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(Write("custom.second"));
            definitions.AddOperation(Write("custom.first"));

            var last = FluxParticleValuePipelineResolver.Resolve(definitions, "custom.value",
                operation => operation.Id == "custom.first" ? 100 : 200);

            Assert.AreEqual("custom.second", last);
            var firstRead = definitions.Operations["custom.first"].Reads.Single();
            Assert.AreEqual(FluxParticleReadVersion.StepPrevious, firstRead.Version);
            var secondRead = definitions.Operations["custom.second"].Reads.Single();
            Assert.AreEqual(FluxParticleReadVersion.AfterOperation, secondRead.Version);
            Assert.AreEqual("custom.first", secondRead.ProducerOperationId);
            CollectionAssert.Contains(definitions.Operations["custom.second"].Dependencies.ToArray(), "custom.first");
        }

        [Test]
        public void MissingOrDuplicateOrderIsRejected()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddOperation(Write("custom.first"));
            definitions.AddOperation(Write("custom.second"));
            Assert.Throws<InvalidOperationException>(() => FluxParticleValuePipelineResolver.Resolve(definitions,
                "custom.value", operation => null));
            Assert.Throws<InvalidOperationException>(() => FluxParticleValuePipelineResolver.Resolve(definitions,
                "custom.value", operation => 100));
        }

        [Test]
        public void NoWriterLeavesValueUnchanged()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            Assert.IsNull(FluxParticleValuePipelineResolver.Resolve(definitions, "custom.value", operation => 100));
        }

        [Test]
        public void ConsumerReadsFinalWriter()
        {
            var consumer = new FluxParticleOperationDefinition("custom.consumer", "custom", FluxParticlePhase.Update,
                new[] { new FluxParticleValueRead("custom.value", FluxParticleReadVersion.StepPrevious) },
                new[] { new FluxParticleValueWrite("custom.other", FluxParticleWriteKind.Modify) }, null,
                "Custom.hlsl", "Custom");
            var linked = FluxParticleValuePipelineResolver.Connect(consumer, "custom.value", "custom.last");
            Assert.AreEqual("custom.last", linked.Reads.Single().ProducerOperationId);
            CollectionAssert.Contains(linked.Dependencies.ToArray(), "custom.last");
            var unlinked = FluxParticleValuePipelineResolver.Connect(consumer, "custom.value", null);
            Assert.AreEqual(FluxParticleReadVersion.StepPrevious, unlinked.Reads.Single().Version);
        }

        static FluxParticleOperationDefinition Write(string id)
        {
            return new FluxParticleOperationDefinition(id, "custom", FluxParticlePhase.Update,
                new[] { new FluxParticleValueRead("custom.value", FluxParticleReadVersion.StepPrevious) },
                new[] { new FluxParticleValueWrite("custom.value", FluxParticleWriteKind.Modify) }, null,
                "Custom.hlsl", "Custom");
        }
    }
}
