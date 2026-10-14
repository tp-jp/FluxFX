using System;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// Update WriterのPage分離とPage間依存を回帰検証します。
    /// </summary>
    public sealed class FluxParticlePageSchedulingTests
    {
        const string Owner = "fluxfx.core";

        [Test]
        public void IndependentUpdateWritersUseSeparatePages()
        {
            var definitions = CreateTwoWriters();
            var program = Compile(definitions);

            Assert.AreEqual(2, program.StateLayout.Pages.Count);
            Assert.AreEqual(2, program.ExecutionPlan.Passes.Count);
            Assert.IsTrue(program.StateLayout.TryGetBinding("custom.a", out var a));
            Assert.IsTrue(program.StateLayout.TryGetBinding("custom.b", out var b));
            Assert.AreNotEqual(a.PageId, b.PageId);
        }

        [Test]
        public void CrossPageDependencySchedulesProducerBeforeConsumer()
        {
            var definitions = CreateTwoWriters();
            definitions.ReplaceOperation(Operation("update.b", FluxParticlePhase.Update,
                new[] { new FluxParticleValueRead("custom.a", FluxParticleReadVersion.AfterOperation, "update.a"),
                    new FluxParticleValueRead("custom.b", FluxParticleReadVersion.StepPrevious) },
                new[] { Write("custom.b", FluxParticleWriteKind.Modify) }, "update.a"));

            var program = Compile(definitions);
            Assert.AreEqual(2, program.StateLayout.Pages.Count);
            Assert.AreEqual(2, program.ExecutionPlan.Passes.Count);
            var first = program.ExecutionPlan.Passes[0];
            var second = program.ExecutionPlan.Passes[1];
            Assert.AreEqual("update.a", first.UpdateOperations.Single().Id);
            Assert.AreEqual("update.b", second.UpdateOperations.Single().Id);
            Assert.IsTrue(second.Reads.Any(read => read.PageId == first.OutputPageId &&
                read.Version == FluxParticlePageVersion.Updated));
        }

        [Test]
        public void SpawnOnlyStateFitsWithoutAdditionalPass()
        {
            var definitions = CreateTwoWriters();
            var baseline = Compile(definitions);
            definitions.AddValue(new FluxParticleValueDefinition("custom.spawn-only", FluxParticleValueType.Float,
                FluxParticleWritePolicy.SharedMutable, Owner));
            definitions.AddOperation(Operation("spawn.extra", FluxParticlePhase.Spawn,
                Array.Empty<FluxParticleValueRead>(),
                new[] { Write("custom.spawn-only", FluxParticleWriteKind.Initialize) }));

            var program = Compile(definitions);
            Assert.AreEqual(baseline.StateLayout.Pages.Count, program.StateLayout.Pages.Count);
            Assert.AreEqual(baseline.ExecutionPlan.Passes.Count, program.ExecutionPlan.Passes.Count);
            Assert.IsTrue(program.StateLayout.TryGetBinding("custom.spawn-only", out _));
        }

        static FluxParticleDefinitionBuilder CreateTwoWriters()
        {
            var definitions = new FluxParticleDefinitionBuilder();
            definitions.AddValue(new FluxParticleValueDefinition("custom.a", FluxParticleValueType.Float,
                FluxParticleWritePolicy.SharedMutable, Owner));
            definitions.AddValue(new FluxParticleValueDefinition("custom.b", FluxParticleValueType.Float,
                FluxParticleWritePolicy.SharedMutable, Owner));
            foreach (var id in new[] { "a", "b" })
            {
                definitions.AddOperation(Operation("spawn." + id, FluxParticlePhase.Spawn,
                    Array.Empty<FluxParticleValueRead>(), new[] { Write("custom." + id, FluxParticleWriteKind.Initialize) }));
                definitions.AddOperation(Operation("update." + id, FluxParticlePhase.Update,
                    new[] { new FluxParticleValueRead("custom." + id, FluxParticleReadVersion.StepPrevious) },
                    new[] { Write("custom." + id, FluxParticleWriteKind.Modify) }));
            }
            return definitions;
        }

        static FluxParticleValueWrite Write(string valueId, FluxParticleWriteKind kind)
        {
            return new FluxParticleValueWrite(valueId, kind);
        }

        static FluxParticleOperationDefinition Operation(string id, FluxParticlePhase phase,
            FluxParticleValueRead[] reads, FluxParticleValueWrite[] writes, params string[] dependencies)
        {
            return new FluxParticleOperationDefinition(id, Owner, phase, reads, writes, dependencies,
                "Tests/" + id + ".hlsl", "Execute");
        }

        static FluxParticleCompiledProgram Compile(FluxParticleDefinitionBuilder definitions)
        {
            return new FluxParticleProgramCompiler().Compile(definitions);
        }
    }
}
