using System;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// 論理定義のコンパイル、Persistent Stateの導出、PageとPassの構成を検証します。
    /// </summary>
    public class FluxParticleProgramCompilerTests
    {
        const string Core = "fluxfx.core";

        [Test]
        public void StandardSimulationProducesTwoPagesAndTwoPasses()
        {
            var program = Compile(CreateStandard());
            Assert.AreEqual(2, program.StateLayout.Pages.Count);
            Assert.AreEqual(2, program.ExecutionPlan.Passes.Count);

            AssertBinding(program, "particle.position", 0, 3);
            AssertBinding(program, "particle.age", 3, 1);
            AssertBinding(program, "particle.velocity", 0, 3);
            AssertBinding(program, "particle.spawn-key", 3, 1);

            Assert.IsFalse(program.StateLayout.TryGetBinding("particle.lifetime", out _));
            Assert.IsFalse(program.StateLayout.TryGetBinding("particle.start-color", out _));
            Assert.IsFalse(program.StateLayout.TryGetBinding("render.color", out _));

            var velocity = program.ExecutionPlan.Passes[0];
            var position = program.ExecutionPlan.Passes[1];

            Assert.AreEqual("gravity", velocity.UpdateOperations[0].Id);
            Assert.AreEqual("drag", velocity.UpdateOperations[1].Id);
            Assert.AreEqual("motion", position.UpdateOperations[0].Id);
            Assert.AreEqual(FluxParticlePassCondition.Always, velocity.Condition);
            Assert.AreEqual(FluxParticlePassCondition.Always, position.Condition);
            Assert.IsTrue(position.Reads.Any(read => read.PageId == velocity.OutputPageId &&
                read.Version == FluxParticlePageVersion.Updated));
        }

        [Test]
        public void RegistrationOrderDoesNotChangeTheLayout()
        {
            var builder = CreateStandard();
            var ordered = Compile(builder);
            var another = new FluxParticleDefinitionBuilder();
            foreach (var value in builder.Values.Values.Reverse()) another.AddValue(value);
            foreach (var parameter in builder.Parameters.Values.Reverse()) another.AddParameter(parameter);
            foreach (var provider in builder.Providers.Values.Reverse()) another.AddProvider(provider);
            foreach (var operation in builder.Operations.Values.Reverse()) another.AddOperation(operation);

            var reversed = Compile(another);
            Assert.AreEqual(ordered.StateLayout.Pages.Count, reversed.StateLayout.Pages.Count);
            Assert.AreEqual(ordered.ExecutionPlan.Passes.Count, reversed.ExecutionPlan.Passes.Count);
            foreach (var value in builder.Values.Values)
            {
                var a = ordered.StateLayout.TryGetBinding(value.Id, out var first);
                var b = reversed.StateLayout.TryGetBinding(value.Id, out var second);
                Assert.AreEqual(a, b);
                if (!a) continue;
                Assert.AreEqual(first.PageId, second.PageId);
                Assert.AreEqual(first.ChannelOffset, second.ChannelOffset);
            }
        }

        [Test]
        public void ASpawnOnlyCustomStateGeneratesConditionalPass()
        {
            var definitions = CreateStandard();
            definitions.AddValue(new FluxParticleValueDefinition("custom.firefly.mode", FluxParticleValueType.Float, FluxParticleWritePolicy.OwnerManaged, "custom.firefly"));
            definitions.AddOperation(Operation("spawn.mode", "custom.firefly", FluxParticlePhase.Spawn,
                Array.Empty<FluxParticleValueRead>(), new[] { Write("custom.firefly.mode", FluxParticleWriteKind.Initialize) }));

            var program = Compile(definitions);
            Assert.AreEqual(3, program.StateLayout.Pages.Count);
            Assert.AreEqual(3, program.ExecutionPlan.Passes.Count);
            Assert.AreEqual(1, program.ExecutionPlan.Passes.Count(pass => pass.Condition == FluxParticlePassCondition.HasSpawn));
            Assert.IsTrue(program.StateLayout.TryGetBinding("custom.firefly.mode", out _));
        }

        [Test]
        public void SpawnOperationWritingMultiplePagesIsProjectedWithoutLosingWrites()
        {
            var definitions = CreateStandard();
            definitions.AddValue(new FluxParticleValueDefinition("custom.firefly.mode", FluxParticleValueType.Float,
                FluxParticleWritePolicy.SharedMutable, Core));
            // A single logical Spawn Operation writes an Update Page and a Spawn-only Page.
            var source = CopyExcluding(definitions, "spawn.velocity");
            source.AddOperation(Operation("spawn.velocity", Core, FluxParticlePhase.Spawn,
                new[] { Read("spawn.key", FluxParticleReadVersion.Provided) },
                new[] { Write("particle.velocity", FluxParticleWriteKind.Initialize),
                        Write("particle.spawn-key", FluxParticleWriteKind.Initialize),
                        Write("custom.firefly.mode", FluxParticleWriteKind.Initialize) }));

            var program = Compile(source);
            Assert.AreEqual(3, program.StateLayout.Pages.Count);
            Assert.AreEqual(3, program.ExecutionPlan.Passes.Count);
            var projected = program.ExecutionPlan.Passes
                .SelectMany(pass => pass.SpawnOperations.Select(op => new { pass.OutputPageId, Operation = op }))
                .Where(entry => entry.Operation.Id == "spawn.velocity").ToArray();
            Assert.AreEqual(2, projected.Length);
            Assert.AreEqual(3, projected.Sum(entry => entry.Operation.Writes.Count));
            Assert.IsTrue(projected.All(entry => entry.Operation.Writes.All(write =>
                program.StateLayout.TryGetBinding(write.ValueId, out var binding) &&
                binding.PageId == entry.OutputPageId)));
        }

        [Test]
        public void StoredLifetimeAndSeedRequireThreePages()
        {
            var definitions = CreateStandard(withDerivedLifetime: false);
            definitions.AddOperation(Operation("spawn.lifetime", Core, FluxParticlePhase.Spawn,
                Array.Empty<FluxParticleValueRead>(), new[] { Write("particle.lifetime", FluxParticleWriteKind.Initialize) }));

            var program = Compile(definitions);
            Assert.AreEqual(3, program.StateLayout.Pages.Count);
            Assert.IsTrue(program.StateLayout.TryGetBinding("particle.lifetime", out _));
            Assert.IsTrue(program.StateLayout.TryGetBinding("particle.spawn-key", out _));
        }

        [Test]
        public void RenderingOnlyDoesNotCreatePersistentPages()
        {
            var builder = new FluxParticleDefinitionBuilder();
            builder.AddValue(new FluxParticleValueDefinition("render.color", FluxParticleValueType.Float4, FluxParticleWritePolicy.SharedMutable, Core));
            builder.AddOperation(Operation("render.init", Core, FluxParticlePhase.Render,
                Array.Empty<FluxParticleValueRead>(), new[] { Write("render.color", FluxParticleWriteKind.Replace) }));
            var program = Compile(builder);
            Assert.AreEqual(0, program.StateLayout.Pages.Count);
            Assert.AreEqual(0, program.ExecutionPlan.Passes.Count);
            Assert.AreEqual(1, program.Operations.Count);
        }

        [Test]
        public void RejectRenderWritesToSimulationState()
        {
            var builder = CreateStandard();
            builder.AddOperation(Operation("render.invalid-state-update", Core, FluxParticlePhase.Render,
                new[] { Read("particle.position", FluxParticleReadVersion.Committed) },
                new[] { Write("particle.position", FluxParticleWriteKind.Modify) }));
            Assert.Throws<InvalidOperationException>(() => Compile(builder));
        }

        [Test]
        public void RejectUnboundedUIntPersistentState()
        {
            var builder = new FluxParticleDefinitionBuilder();
            builder.AddValue(new FluxParticleValueDefinition("custom.seed", FluxParticleValueType.UInt, FluxParticleWritePolicy.SharedMutable, Core));
            builder.AddOperation(Operation("spawn.seed", Core, FluxParticlePhase.Spawn,
                Array.Empty<FluxParticleValueRead>(), new[] { Write("custom.seed", FluxParticleWriteKind.Initialize) }));
            Assert.Throws<InvalidOperationException>(() => Compile(builder));
        }

        [Test]
        public void RejectDuplicateValueDefinition()
        {
            var builder = new FluxParticleDefinitionBuilder();
            builder.AddValue(new FluxParticleValueDefinition("particle.position", FluxParticleValueType.Float3, FluxParticleWritePolicy.SystemManaged, Core));
            Assert.Throws<InvalidOperationException>(() => builder.AddValue(
                new FluxParticleValueDefinition("particle.position", FluxParticleValueType.Float, FluxParticleWritePolicy.SystemManaged, Core)));
        }

        [Test]
        public void RejectMissingSpawnInitializer()
        {
            var builder = CreateStandard();
            var another = CopyExcluding(builder, "spawn.velocity");
            Assert.Throws<InvalidOperationException>(() => Compile(another));
        }

        [Test]
        public void RejectUnorderedWriters()
        {
            var builder = CreateStandard();
            builder.AddOperation(Operation("extra.gravity", Core, FluxParticlePhase.Update,
                new[] { Read("particle.velocity", FluxParticleReadVersion.StepPrevious) },
                new[] { Write("particle.velocity", FluxParticleWriteKind.Modify) }));
            Assert.Throws<InvalidOperationException>(() => Compile(builder));
        }

        [Test]
        public void RejectCyclicOperationDependency()
        {
            var builder = CreateStandard();
            builder.AddOperation(Operation("cycle.a", Core, FluxParticlePhase.Update,
                Array.Empty<FluxParticleValueRead>(), Array.Empty<FluxParticleValueWrite>(), "cycle.b"));
            builder.AddOperation(Operation("cycle.b", Core, FluxParticlePhase.Update,
                Array.Empty<FluxParticleValueRead>(), Array.Empty<FluxParticleValueWrite>(), "cycle.a"));
            Assert.Throws<InvalidOperationException>(() => Compile(builder));
        }

        [Test]
        public void RejectCyclicProviderDependency()
        {
            var builder = CreateStandard();
            builder.AddValue(new FluxParticleValueDefinition("value.a", FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly, Core));
            builder.AddValue(new FluxParticleValueDefinition("value.b", FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly, Core));
            builder.AddProvider(new FluxParticleValueProvider("value.a", FluxParticleProviderKind.Derived,
                new[] { "value.b" }, hlslPath: "A.hlsl", entryPoint: "A"));
            builder.AddProvider(new FluxParticleValueProvider("value.b", FluxParticleProviderKind.Derived,
                new[] { "value.a" }, hlslPath: "B.hlsl", entryPoint: "B"));
            Assert.Throws<InvalidOperationException>(() => Compile(builder));
        }

        [Test]
        public void RejectSpawnInputInRendering()
        {
            var builder = CreateStandard();
            builder.AddOperation(Operation("render.invalid", Core, FluxParticlePhase.Render,
                new[] { Read("spawn.key", FluxParticleReadVersion.Provided) }, Array.Empty<FluxParticleValueWrite>()));
            Assert.Throws<InvalidOperationException>(() => Compile(builder));
        }

        [Test]
        public void RejectInterleavedWritesThatRequireTheSamePageTwice()
        {
            var builder = new FluxParticleDefinitionBuilder();
            AddValue(builder, "particle.position", FluxParticleValueType.Float3, FluxParticleWritePolicy.SharedMutable);
            AddValue(builder, "particle.velocity", FluxParticleValueType.Float3, FluxParticleWritePolicy.SharedMutable);
            builder.AddOperation(Operation("spawn.position", Core, FluxParticlePhase.Spawn,
                Array.Empty<FluxParticleValueRead>(), new[] { Write("particle.position", FluxParticleWriteKind.Initialize) }));
            builder.AddOperation(Operation("spawn.velocity", Core, FluxParticlePhase.Spawn,
                Array.Empty<FluxParticleValueRead>(), new[] { Write("particle.velocity", FluxParticleWriteKind.Initialize) }));
            builder.AddOperation(Operation("move.first", Core, FluxParticlePhase.Update,
                new[] { Read("particle.position", FluxParticleReadVersion.StepPrevious) },
                new[] { Write("particle.position", FluxParticleWriteKind.Modify) }));
            builder.AddOperation(Operation("velocity.correct", Core, FluxParticlePhase.Update,
                new[] { Read("particle.position", FluxParticleReadVersion.AfterOperation, "move.first") },
                new[] { Write("particle.velocity", FluxParticleWriteKind.Modify) }, "move.first"));
            builder.AddOperation(Operation("move.second", Core, FluxParticlePhase.Update,
                new[] { Read("particle.velocity", FluxParticleReadVersion.AfterOperation, "velocity.correct"),
                        Read("particle.position", FluxParticleReadVersion.AfterOperation, "move.first") },
                new[] { Write("particle.position", FluxParticleWriteKind.Modify) }, "velocity.correct", "move.first"));

            var error = Assert.Throws<InvalidOperationException>(() => Compile(builder));
            StringAssert.Contains("multiple updates of a Page", error.Message);
        }

        [Test]
        public void RejectNonMaterializedSpawnLockedParameter()
        {
            var builder = CreateStandard(withDerivedLifetime: false);
            builder.AddParameter(new FluxParticleParameterDefinition("lifetime.dynamic", FluxParticleValueType.Float2, FluxParticleParameterPolicy.SpawnLocked));
            builder.AddProvider(new FluxParticleValueProvider("particle.lifetime", FluxParticleProviderKind.Derived,
                parameters: new[] { "lifetime.dynamic" }, hlslPath: "Lifetime.hlsl", entryPoint: "EvalLifetime"));
            Assert.Throws<InvalidOperationException>(() => Compile(builder));
        }

        [Test]
        public void RejectUnauthorizedCustomStateWrite()
        {
            var builder = CreateStandard();
            builder.AddValue(new FluxParticleValueDefinition("custom.private", FluxParticleValueType.Float, FluxParticleWritePolicy.OwnerManaged, "other.module"));
            builder.AddOperation(Operation("spawn.private", Core, FluxParticlePhase.Spawn,
                Array.Empty<FluxParticleValueRead>(), new[] { Write("custom.private", FluxParticleWriteKind.Initialize) }));
            Assert.Throws<InvalidOperationException>(() => Compile(builder));
        }

        static void AssertBinding(FluxParticleCompiledProgram program, string id, int offset, int count)
        {
            Assert.IsTrue(program.StateLayout.TryGetBinding(id, out var binding), id);
            Assert.AreEqual(offset, binding.ChannelOffset, id);
            Assert.AreEqual(count, binding.ComponentCount, id);
        }

        static FluxParticleCompiledProgram Compile(FluxParticleDefinitionBuilder definitions)
        {
            return new FluxParticleProgramCompiler().Compile(definitions);
        }

        static FluxParticleDefinitionBuilder CopyExcluding(FluxParticleDefinitionBuilder source, string removedId)
        {
            var builder = new FluxParticleDefinitionBuilder();
            foreach (var value in source.Values.Values) builder.AddValue(value);
            foreach (var parameter in source.Parameters.Values) builder.AddParameter(parameter);
            foreach (var provider in source.Providers.Values) builder.AddProvider(provider);
            foreach (var operation in source.Operations.Values)
            {
                if (operation.Id == removedId) continue;
                builder.AddOperation(operation);
            }
            return builder;
        }

        static FluxParticleDefinitionBuilder CreateStandard(bool withDerivedLifetime = true)
        {
            var builder = new FluxParticleDefinitionBuilder();
            AddValue(builder, "particle.position", FluxParticleValueType.Float3, FluxParticleWritePolicy.SystemManaged);
            AddValue(builder, "particle.age", FluxParticleValueType.Float, FluxParticleWritePolicy.SystemManaged);
            AddValue(builder, "particle.velocity", FluxParticleValueType.Float3, FluxParticleWritePolicy.SharedMutable);
            AddValue(builder, "particle.spawn-key", FluxParticleValueType.UInt, FluxParticleWritePolicy.SystemManaged);
            AddValue(builder, "particle.lifetime", FluxParticleValueType.Float, FluxParticleWritePolicy.SystemManaged);
            AddValue(builder, "particle.start-color", FluxParticleValueType.Float4, FluxParticleWritePolicy.ReadOnly);
            AddValue(builder, "render.color", FluxParticleValueType.Float4, FluxParticleWritePolicy.SharedMutable);
            AddValue(builder, "spawn.key", FluxParticleValueType.UInt, FluxParticleWritePolicy.ReadOnly);

            builder.AddProvider(new FluxParticleValueProvider("spawn.key", FluxParticleProviderKind.SpawnInput));
            builder.AddParameter(new FluxParticleParameterDefinition("color.range", FluxParticleValueType.Float4, FluxParticleParameterPolicy.Live));
            builder.AddProvider(new FluxParticleValueProvider("particle.start-color", FluxParticleProviderKind.Derived,
                new[] { "particle.spawn-key" }, new[] { "color.range" }, "StartColor.hlsl", "EvalColor"));

            if (withDerivedLifetime)
            {
                builder.AddParameter(new FluxParticleParameterDefinition("lifetime.range", FluxParticleValueType.Float2, FluxParticleParameterPolicy.Static));
                builder.AddProvider(new FluxParticleValueProvider("particle.lifetime", FluxParticleProviderKind.Derived,
                    new[] { "particle.spawn-key" }, new[] { "lifetime.range" }, "Lifetime.hlsl", "EvalLifetime"));
            }

            builder.AddOperation(Operation("spawn.position", Core, FluxParticlePhase.Spawn,
                new[] { Read("spawn.key", FluxParticleReadVersion.Provided) },
                new[] { Write("particle.position", FluxParticleWriteKind.Initialize), Write("particle.age", FluxParticleWriteKind.Initialize) }));
            builder.AddOperation(Operation("spawn.velocity", Core, FluxParticlePhase.Spawn,
                new[] { Read("spawn.key", FluxParticleReadVersion.Provided) },
                new[] { Write("particle.velocity", FluxParticleWriteKind.Initialize), Write("particle.spawn-key", FluxParticleWriteKind.Initialize) }));
            builder.AddOperation(Operation("gravity", Core, FluxParticlePhase.Update,
                new[] { Read("particle.velocity", FluxParticleReadVersion.StepPrevious) },
                new[] { Write("particle.velocity", FluxParticleWriteKind.Modify) }));
            builder.AddOperation(Operation("drag", Core, FluxParticlePhase.Update,
                new[] { Read("particle.velocity", FluxParticleReadVersion.AfterOperation, "gravity") },
                new[] { Write("particle.velocity", FluxParticleWriteKind.Modify) }, "gravity"));
            builder.AddOperation(Operation("motion", Core, FluxParticlePhase.Update,
                new[]
                {
                    Read("particle.velocity", FluxParticleReadVersion.AfterOperation, "drag"),
                    Read("particle.position", FluxParticleReadVersion.StepPrevious),
                    Read("particle.age", FluxParticleReadVersion.StepPrevious)
                },
                new[] { Write("particle.position", FluxParticleWriteKind.Modify), Write("particle.age", FluxParticleWriteKind.Modify) }, "drag"));
            builder.AddOperation(Operation("render.init-color", Core, FluxParticlePhase.Render,
                new[] { Read("particle.start-color", FluxParticleReadVersion.Provided) },
                new[] { Write("render.color", FluxParticleWriteKind.Replace) }));
            builder.AddOperation(Operation("render.color-over-life", Core, FluxParticlePhase.Render,
                new[]
                {
                    Read("render.color", FluxParticleReadVersion.AfterOperation, "render.init-color"),
                    Read("particle.age", FluxParticleReadVersion.Committed),
                    Read("particle.lifetime", withDerivedLifetime ? FluxParticleReadVersion.Provided : FluxParticleReadVersion.Committed)
                },
                new[] { Write("render.color", FluxParticleWriteKind.Modify) }, "render.init-color"));
            return builder;
        }

        [TestCase(FluxParticlePhase.Spawn)]
        [TestCase(FluxParticlePhase.Update)]
        public void LookupProviderRejectsSimulationUsage(FluxParticlePhase phase)
        {
            var definitions = new FluxParticleDefinitionBuilder();
            AddValue(definitions, "lookup.input", FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly);
            AddValue(definitions, "lookup.value", FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly);
            AddValue(definitions, "lookup.state", FluxParticleValueType.Float, FluxParticleWritePolicy.SharedMutable);
            definitions.AddProvider(new FluxParticleValueProvider("lookup.input", FluxParticleProviderKind.Uniform));
            definitions.AddProvider(new FluxParticleValueProvider("lookup.value", FluxParticleProviderKind.LookupTable,
                new[] { "lookup.input" }, lookupSamples: new[] { 0f, 1f }));
            definitions.AddOperation(Operation("lookup.spawn", Core, FluxParticlePhase.Spawn,
                phase == FluxParticlePhase.Spawn ? new[] { Read("lookup.value", FluxParticleReadVersion.Provided) } :
                    Array.Empty<FluxParticleValueRead>(),
                new[] { Write("lookup.state", FluxParticleWriteKind.Initialize) }));
            if (phase == FluxParticlePhase.Update)
            {
                definitions.AddOperation(Operation("lookup.update", Core, FluxParticlePhase.Update,
                    new[] { Read("lookup.value", FluxParticleReadVersion.Provided) },
                    new[] { Write("lookup.state", FluxParticleWriteKind.Replace) }));
            }
            StringAssert.Contains("only supported in Render", Assert.Throws<InvalidOperationException>(() =>
                new FluxParticleProgramCompiler().Compile(definitions)).Message);
        }

        [Test]
        public void LookupProviderRejectsInvalidSamples()
        {
            Assert.Throws<ArgumentException>(() => new FluxParticleValueProvider("lookup", FluxParticleProviderKind.LookupTable,
                new[] { "input" }, lookupSamples: new[] { 0f }));
            Assert.Throws<ArgumentException>(() => new FluxParticleValueProvider("lookup", FluxParticleProviderKind.LookupTable,
                new[] { "input" }, lookupSamples: new[] { 0f, float.NaN }));
            Assert.Throws<ArgumentException>(() => new FluxParticleValueProvider("lookup", FluxParticleProviderKind.LookupTable,
                new[] { "input" }, lookupSamples: new[] { 0f, float.PositiveInfinity }));
        }
        static void AddValue(FluxParticleDefinitionBuilder builder, string id, FluxParticleValueType type, FluxParticleWritePolicy policy)
        {
            builder.AddValue(new FluxParticleValueDefinition(id, type, policy, Core, type == FluxParticleValueType.UInt ? 1048575u : (uint?)null));
        }

        static FluxParticleValueRead Read(string id, FluxParticleReadVersion version, string producer = null)
        {
            return new FluxParticleValueRead(id, version, producer);
        }

        static FluxParticleValueWrite Write(string id, FluxParticleWriteKind kind)
        {
            return new FluxParticleValueWrite(id, kind);
        }

        static FluxParticleOperationDefinition Operation(
            string id,
            string owner,
            FluxParticlePhase phase,
            FluxParticleValueRead[] reads,
            FluxParticleValueWrite[] writes,
            params string[] dependencies)
        {
            return new FluxParticleOperationDefinition(id, owner, phase, reads, writes, dependencies, "Tests/" + id + ".hlsl", "Execute");
        }
    }
}
