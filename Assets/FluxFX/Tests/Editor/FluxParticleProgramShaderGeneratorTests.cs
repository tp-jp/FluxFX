using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleProgramShaderGeneratorTests
    {
        string temporaryFolder;
        readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            temporaryFolder = "Assets/FluxFX/Tests/Phase2Temporary_" + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var value in objects)
            {
                if (value != null) UnityEngine.Object.DestroyImmediate(value);
            }
            objects.Clear();
            if (AssetDatabase.IsValidFolder(temporaryFolder)) AssetDatabase.DeleteAsset(temporaryFolder);
        }

        [Test]
        public void BasicProgramGeneratesTwoPagesAndTwoPassesWithoutVisualState()
        {
            var generated = Generate();
            Assert.AreEqual(2, generated.Compilation.StateLayout.Pages.Count);
            Assert.AreEqual(2, generated.Passes.Count);
            Assert.AreEqual(2, generated.ClearSources.Count);
            Assert.IsFalse(generated.Compilation.StateLayout.TryGetBinding("particle.start-color", out _));
            Assert.IsFalse(generated.Compilation.StateLayout.TryGetBinding("render.color", out _));
            var velocity = generated.Passes.Single(p => p.Plan.UpdateOperations.Any(op => op.Id == "gravity"));
            var motion = generated.Passes.Single(p => p.Plan.UpdateOperations.Any(op => op.Id == "motion"));
            Assert.Less(velocity.Plan.Index, motion.Plan.Index);
            StringAssert.Contains("FluxFX_Gravity(operationGravity)", velocity.Source);
            StringAssert.Contains("FluxFX_Drag(operationDrag)", velocity.Source);
            StringAssert.Contains("operationDrag.valueParticleVelocity = operationGravity.valueParticleVelocity", velocity.Source);
            Assert.IsTrue(motion.Reads.Any(read => read.PageId == velocity.Plan.OutputPageId && read.Version == FluxParticlePageVersion.Updated));
            Assert.IsTrue(velocity.Reads.Any(read => read.PageId == motion.Plan.OutputPageId && read.Version == FluxParticlePageVersion.StepPrevious));
        }

        [Test]
        public void OperationContextContainsOnlyDeclaredValuesAndParameters()
        {
            var source = Generate().Passes.Single(p => p.Plan.UpdateOperations.Any(op => op.Id == "gravity")).Source;
            var begin = source.IndexOf("struct FluxGravityContext", StringComparison.Ordinal);
            var end = source.IndexOf("};", begin, StringComparison.Ordinal);
            var context = source.Substring(begin, end - begin);
            StringAssert.Contains("valueParticleVelocity", context);
            StringAssert.Contains("parameterGravity", context);
            StringAssert.DoesNotContain("valueParticlePosition", context);
            StringAssert.DoesNotContain("parameterDrag", context);
        }

        [Test]
        public void RenderEvaluatesProvidersAndColorWithoutAdditionalPasses()
        {
            var generated = Generate();
            StringAssert.Contains("FluxFX_StartColor(_FluxParameter_n_color_002e_start)", generated.RenderSource);
            StringAssert.Contains("FluxFX_Lifetime(", generated.RenderSource);
            StringAssert.Contains("FluxFX_RenderColor(operationRenderColor)", generated.RenderSource);
            Assert.AreEqual(2, generated.Passes.Count);
        }

        [Test]
        public void IdentifierEncodingSeparatesPunctuationUnderscoresAndLeadingDigits()
        {
            var ids = new[] { "a.b", "a-b", "a_b", "a_002e_b", "1", "日本語" };
            Assert.AreEqual(ids.Length, ids.Select(FluxParticleHlslNames.Encode).Distinct().Count());
            Assert.IsTrue(ids.Select(FluxParticleHlslNames.Encode).All(id => id.StartsWith("n_", StringComparison.Ordinal)));
        }

        [Test]
        public void GeneratedFieldsAndOperationLocalsUseReadableCamelCase()
        {
            Assert.AreEqual("valueParticlePosition", FluxParticleHlslNames.Value("particle.position"));
            Assert.AreEqual("valueParticleSpawnKey", FluxParticleHlslNames.Value("particle.spawn-key"));
            Assert.AreEqual("parameterShapeRadius", FluxParticleHlslNames.Parameter("shape.radius"));
            Assert.AreEqual("operationSpawnPosition", FluxParticleHlslNames.OperationLocal("spawn.position"));
            Assert.AreEqual("FluxSpawnPositionContext", FluxParticleHlslNames.Context("spawn.position"));
        }

        [Test]
        public void RejectValueFieldNameCollision()
        {
            var definitions = CreateDefinitions();
            definitions.AddValue(new FluxParticleValueDefinition("custom.force", FluxParticleValueType.Float,
                FluxParticleWritePolicy.ReadOnly, "test"));
            definitions.AddValue(new FluxParticleValueDefinition("custom-force", FluxParticleValueType.Float,
                FluxParticleWritePolicy.ReadOnly, "test"));
            var exception = Assert.Throws<InvalidOperationException>(() => Generate(definitions));
            StringAssert.Contains("Value field name collision", exception.Message);
        }

        [Test]
        public void RejectParameterFieldNameCollision()
        {
            var definitions = CreateDefinitions();
            definitions.AddParameter(new FluxParticleParameterDefinition("custom.force", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live));
            definitions.AddParameter(new FluxParticleParameterDefinition("custom-force", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live));
            var exception = Assert.Throws<InvalidOperationException>(() => Generate(definitions));
            StringAssert.Contains("Parameter field name collision", exception.Message);
        }

        [TestCase("gravity", "FluxGravityContext")]
        [TestCase("spawn.velocity", "FluxSpawnVelocityContext")]
        [TestCase("custom-force", "FluxCustomForceContext")]
        [TestCase("custom_force", "FluxCustomForceContext")]
        [TestCase("1", "Flux1Context")]
        [TestCase("日本語", "FluxX65E5X672CX8A9EContext")]
        public void ContextTypeUsesPascalCase(string id, string expected)
        {
            Assert.AreEqual(expected, FluxParticleHlslNames.Context(id));
        }

        [TestCase("custom.force", "custom-force")]
        [TestCase("custom_force", "custom.force")]
        [TestCase("gravity", "Gravity")]
        public void RejectContextTypeNameCollisions(string first, string second)
        {
            var definitions = CreateDefinitions();
            foreach (var id in new[] { first, second })
            {
                if (definitions.Operations.ContainsKey(id)) continue;
                definitions.AddOperation(new FluxParticleOperationDefinition(id, "custom", FluxParticlePhase.Render,
                    Array.Empty<FluxParticleValueRead>(), Array.Empty<FluxParticleValueWrite>(), Array.Empty<string>(),
                    "Packages/com.tplab.flux.fx/Editor/Modules/Gravity/Gravity.hlsl", "Custom"));
            }
            var exception = Assert.Throws<InvalidOperationException>(() => Generate(definitions));
            StringAssert.Contains("Context type name collision", exception.Message);
        }

        [Test]
        public void AllTemplateTokensAreResolvedInStepClearAndRender()
        {
            var generated = Generate();
            foreach (var source in generated.Passes.Select(pass => pass.Source).Concat(generated.ClearSources)
                .Concat(new[] { generated.RenderSource }))
            {
                StringAssert.DoesNotContain("{{", source);
                StringAssert.DoesNotContain("}}", source);
            }
            StringAssert.Contains("if (spawn)", generated.Passes[0].Source);
            StringAssert.Contains("else if (alive)", generated.Passes[0].Source);
            StringAssert.Contains("FluxRenderContext FluxFX_EvaluateRender", generated.RenderSource);
        }

        [Test]
        public void ClearInitializesNegativeAgeInItsActualChannel()
        {
            var generated = Generate();
            Assert.IsTrue(generated.Compilation.StateLayout.TryGetBinding("particle.age", out var age));
            var channels = new[] { "0.0", "0.0", "0.0", "0.0" };
            channels[age.ChannelOffset] = "-1.0";
            StringAssert.Contains("float4(" + string.Join(", ", channels) + ")", generated.ClearSources[age.PageId]);
        }

        [Test]
        public void ShaderGenerationUsesCompiledSnapshotAfterBuilderChanges()
        {
            var definitions = CreateDefinitions();
            var compilation = new FluxParticleProgramCompiler().Compile(definitions);
            var generator = new FluxParticleProgramShaderGenerator();
            var first = generator.Generate(compilation, "Hidden/FluxFX/Snapshot", FluxParticleCoreDefinitions.CreateLifecycle());

            definitions.AddValue(new FluxParticleValueDefinition("custom.force", FluxParticleValueType.Float,
                FluxParticleWritePolicy.ReadOnly, "test"));
            definitions.AddValue(new FluxParticleValueDefinition("custom-force", FluxParticleValueType.Float,
                FluxParticleWritePolicy.ReadOnly, "test"));
            definitions.AddParameter(new FluxParticleParameterDefinition("custom.parameter",
                FluxParticleValueType.Float, FluxParticleParameterPolicy.Live));
            definitions.AddProvider(new FluxParticleValueProvider("custom.force", FluxParticleProviderKind.Uniform));
            var gravity = definitions.Operations["gravity"];
            definitions.ReplaceOperation(new FluxParticleOperationDefinition(gravity.Id, gravity.OwnerId, gravity.Phase,
                gravity.Reads, gravity.Writes, gravity.Dependencies, "Changed.hlsl", gravity.EntryPoint,
                gravity.Parameters, gravity.EnabledParameterId));

            var second = generator.Generate(compilation, "Hidden/FluxFX/Snapshot", FluxParticleCoreDefinitions.CreateLifecycle());
            Assert.AreSame(compilation, second.Compilation);
            Assert.IsFalse(compilation.Values.ContainsKey("custom.force"));
            Assert.IsFalse(compilation.Parameters.ContainsKey("custom.parameter"));
            Assert.IsFalse(compilation.Providers.ContainsKey("custom.force"));
            CollectionAssert.AreEqual(first.Passes.Select(pass => pass.Source), second.Passes.Select(pass => pass.Source));
            CollectionAssert.AreEqual(first.ClearSources, second.ClearSources);
            Assert.AreEqual(first.RenderSource, second.RenderSource);
            Assert.Throws<InvalidOperationException>(() => Generate(definitions));
        }

        [Test]
        public void RepeatedGenerationIsDeterministic()
        {
            var a = Generate();
            var b = Generate();
            CollectionAssert.AreEqual(a.Passes.Select(p => p.Source), b.Passes.Select(p => p.Source));
            CollectionAssert.AreEqual(a.ClearSources, b.ClearSources);
            Assert.AreEqual(a.RenderSource, b.RenderSource);
        }

        [Test]
        public void RejectUnknownOperationParameter()
        {
            var definitions = CreateDefinitions();
            var source = definitions.Operations["gravity"];
            var changed = Copy(definitions, "gravity");
            changed.AddOperation(new FluxParticleOperationDefinition(source.Id, source.OwnerId, source.Phase,
                source.Reads, source.Writes, source.Dependencies, source.HlslPath, source.EntryPoint, new[] { "missing" }));
            Assert.Throws<InvalidOperationException>(() => Generate(changed));
        }

        [Test]
        public void RejectLiveRecomputedLifetime()
        {
            var definitions = CreateDefinitions();
            var changed = new FluxParticleDefinitionBuilder();
            foreach (var value in definitions.Values.Values) changed.AddValue(value);
            foreach (var provider in definitions.Providers.Values) changed.AddProvider(provider);
            foreach (var operation in definitions.Operations.Values) changed.AddOperation(operation);
            foreach (var parameter in definitions.Parameters.Values)
            {
                changed.AddParameter(parameter.Id == "lifetime.range"
                    ? new FluxParticleParameterDefinition(parameter.Id, parameter.Type, FluxParticleParameterPolicy.Live) : parameter);
            }
            Assert.Throws<InvalidOperationException>(() => Generate(changed));
        }

        [Test]
        public void RejectCrossPageReadOfOverwrittenIntermediateVersion()
        {
            var definitions = CreateDefinitions();
            var changed = Copy(definitions, "motion");
            var motion = definitions.Operations["motion"];
            var reads = motion.Reads.Select(read => read.ValueId == "particle.velocity"
                ? new FluxParticleValueRead(read.ValueId, FluxParticleReadVersion.AfterOperation, "gravity") : read);
            changed.AddOperation(new FluxParticleOperationDefinition(motion.Id, motion.OwnerId, motion.Phase, reads,
                motion.Writes, motion.Dependencies, motion.HlslPath, motion.EntryPoint));
            var exception = Assert.Throws<InvalidOperationException>(() => Generate(changed));
            StringAssert.Contains("Intermediate cross-Page version", exception.Message);
        }

        [Test]
        public void SamePageIntermediateVersionIsPreservedInOperationLocalContext()
        {
            var definitions = CreateDefinitions();
            var changed = Copy(definitions, "motion");
            var gravity = definitions.Operations["gravity"];
            changed.AddOperation(new FluxParticleOperationDefinition("restore", gravity.OwnerId, FluxParticlePhase.Update,
                new[] { new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.AfterOperation, "gravity") },
                gravity.Writes, new[] { "drag" }, "Restore.hlsl", "Restore"));
            var motion = definitions.Operations["motion"];
            changed.AddOperation(new FluxParticleOperationDefinition(motion.Id, motion.OwnerId, motion.Phase,
                motion.Reads.Select(read => read.ValueId == "particle.velocity"
                    ? new FluxParticleValueRead(read.ValueId, FluxParticleReadVersion.AfterOperation, "restore") : read),
                motion.Writes, new[] { "restore" }, motion.HlslPath, motion.EntryPoint));
            var generated = Generate(changed);
            var source = generated.Passes.Single(p => p.Plan.UpdateOperations.Any(op => op.Id == "restore")).Source;
            StringAssert.Contains("operationRestore.valueParticleVelocity = operationGravity.valueParticleVelocity", source);
        }

        [Test]
        public void RejectUnsafeIncludeAndEntryPoint()
        {
            var definitions = CreateDefinitions();
            var changed = Copy(definitions, "gravity");
            var op = definitions.Operations["gravity"];
            changed.AddOperation(new FluxParticleOperationDefinition(op.Id, op.OwnerId, op.Phase, op.Reads,
                op.Writes, op.Dependencies, "Bad\"Include.hlsl", op.EntryPoint));
            Assert.Throws<InvalidOperationException>(() => Generate(changed));
        }

        [Test]
        public void ProjectedSpawnKeepsFullContextButStoresOnlyDestinationWrites()
        {
            var generated = Generate(CreateMultiPage());
            var projected = generated.Passes.Where(p => p.Plan.SpawnOperations.Any(op => op.Id == "spawn.multi")).ToArray();
            Assert.AreEqual(2, projected.Length);
            foreach (var pass in projected)
            {
                StringAssert.Contains("valueCustomMode", pass.Source);
                StringAssert.Contains("valueParticleVelocity", pass.Source);
                StringAssert.Contains("FluxFX_SpawnMulti(operationSpawnMulti)", pass.Source);
                Assert.IsTrue(pass.Plan.SpawnOperations.Single(op => op.Id == "spawn.multi").Writes.All(write =>
                    generated.Compilation.StateLayout.TryGetBinding(write.ValueId, out var binding) && binding.PageId == pass.Plan.OutputPageId));
            }
        }

        [Test]
        public void ImportedStepClearAndRenderShadersCompile()
        {
            RequireGpu();
            var generated = Generate();
            ImportShaders(generated);
            Import("Render", RenderShader(generated));
        }

        [Test]
        public void UndeclaredOperationContextAccessFailsShaderCompilation()
        {
            RequireGpu();
            var definitions = CreateDefinitions();
            var changed = Copy(definitions, "gravity");
            var op = definitions.Operations["gravity"];
            var path = WriteInclude("Invalid", "void Invalid(inout FluxGravityContext ctx) { ctx.undeclared = 1.0; }");
            changed.AddOperation(new FluxParticleOperationDefinition(op.Id, op.OwnerId, op.Phase, op.Reads,
                op.Writes, op.Dependencies, path, "Invalid", op.Parameters));
            var source = Generate(changed).Passes.Single(p => p.Plan.UpdateOperations.Any(o => o.Id == "gravity")).Source;
            var previous = LogAssert.ignoreFailingMessages;
            try
            {
                LogAssert.ignoreFailingMessages = true;
                var shader = Import("InvalidShader", source, false);
                Assert.IsTrue(ShaderUtil.ShaderHasError(shader));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previous;
            }
        }

        [Test]
        public void GpuSpawnUpdateAndRenderMatchExpectedValues()
        {
            RequireGpu();
            var generated = Generate();
            using (var session = CreateSession(generated))
            {
                Configure(session);
                AssertValue(session, generated, "particle.age", 0, new Vector4(-1, 0, 0, 0));
                var mask = Texture(new Color(1, 0, 0, 0), Color.clear);
                session.Step(mask);
                AssertValue(session, generated, "particle.position", 0, new Vector4(10, 0, 0, 0));
                AssertValue(session, generated, "particle.velocity", 0, new Vector4(2, 0, 0, 0));
                AssertValue(session, generated, "particle.age", 0, Vector4.zero);
                AssertValue(session, generated, "particle.spawn-key", 0, new Vector4(123, 0, 0, 0));
                AssertValue(session, generated, "particle.age", 1, new Vector4(-1, 0, 0, 0));
                session.Step();
                var factor = Mathf.Exp(-0.4f * 0.25f);
                var velocity = new Vector4(2 * factor, -0.5f * factor, 0, 0);
                AssertValue(session, generated, "particle.velocity", 0, velocity);
                AssertValue(session, generated, "particle.position", 0, new Vector4(10, 0, 0, 0) + velocity * 0.25f);
                AssertValue(session, generated, "particle.age", 0, new Vector4(0.25f, 0, 0, 0));
                var shader = Import("Render", RenderShader(generated));
                var material = Own(new Material(shader));
                session.BindRender(material);
                var target = Own(new RenderTexture(2, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear));
                target.Create();
                var active = RenderTexture.active;
                try
                {
                    Graphics.Blit(null, target, material);
                    var pixels = ReadPixels(target);
                    Assert.That(pixels[0].r, Is.EqualTo(0.975f).Within(1e-4));
                    Assert.That(pixels[0].a, Is.EqualTo(0.975f).Within(1e-4));
                    Assert.AreEqual(Color.clear, pixels[1]);
                }
                finally
                {
                    RenderTexture.active = active;
                }
            }
        }

        [Test]
        public void GpuDisabledGravityStillProvidesVelocityForDragAndMotion()
        {
            RequireGpu();
            var generated = Generate();
            using (var session = CreateSession(generated))
            {
                Configure(session);
                session.SetFloat("gravity.enabled", 0);
                session.Step(Texture(new Color(1, 0, 0, 0), Color.clear));
                session.Step();
                AssertValue(session, generated, "particle.velocity", 0, new Vector4(2 * Mathf.Exp(-0.1f), 0, 0, 0));
            }
        }

        [Test]
        public void GpuSpawnRequestCannotReplaceLiveParticleAcrossStatePages()
        {
            RequireGpu();
            var generated = Generate(CreateMultiPage());
            using (var session = CreateSession(generated))
            {
                Configure(session);
                var mask = Texture(Color.white, Color.clear);
                session.Step(mask);
                session.SetSpawnInput("spawn.key", Texture(new Color(999, 0, 0, 0), Color.clear));
                session.SetVector("spawn.position", new Vector4(100, 200, 300, 0));
                session.SetVector("spawn.velocity", new Vector4(100, 200, 300, 0));
                session.Step(mask);
                AssertValue(session, generated, "particle.spawn-key", 0, new Vector4(123, 0, 0, 0));
                AssertValue(session, generated, "particle.age", 0, new Vector4(0.25f, 0, 0, 0));
                AssertValue(session, generated, "custom.mode", 0, new Vector4(7, 0, 0, 0));
                Assert.Less(ReadValue(session, generated, "particle.position", 0).x, 100);
                Assert.Less(ReadValue(session, generated, "particle.velocity", 0).x, 100);
            }
        }
        [Test]
        public void GpuExpirationRespawnAndClearPreserveLifecycleContract()
        {
            RequireGpu();
            var generated = Generate();
            using (var session = CreateSession(generated))
            {
                Configure(session, 0.5f);
                var mask = Texture(new Color(1, 0, 0, 0), Color.clear);
                session.Step(mask);
                session.Step();
                session.Step();
                AssertValue(session, generated, "particle.age", 0, new Vector4(-1, 0, 0, 0));
                var before = ReadValue(session, generated, "particle.position", 0);
                session.Step();
                AssertValue(session, generated, "particle.position", 0, before);
                session.Step(mask);
                AssertValue(session, generated, "particle.age", 0, Vector4.zero);
                AssertValue(session, generated, "particle.position", 0, new Vector4(10, 0, 0, 0));
                session.Clear();
                AssertValue(session, generated, "particle.age", 0, new Vector4(-1, 0, 0, 0));
                Assert.Throws<InvalidOperationException>(() => session.SetVector("lifetime.range", Vector4.one));
            }
        }

        [Test]
        public void GpuPartialSpawnDoesNotResetAnExistingParticle()
        {
            RequireGpu();
            var generated = Generate();
            using (var session = CreateSession(generated))
            {
                Configure(session);
                session.Step(Texture(new Color(1, 0, 0, 0), Color.clear));
                session.Step(Texture(Color.clear, new Color(1, 0, 0, 0)));
                AssertValue(session, generated, "particle.age", 0, new Vector4(0.25f, 0, 0, 0));
                AssertValue(session, generated, "particle.age", 1, Vector4.zero);
                AssertValue(session, generated, "particle.spawn-key", 1, new Vector4(456, 0, 0, 0));
            }
        }

        [Test]
        public void GpuMultiPageSpawnInitializesAllWritesAndSkippedPageKeepsCurrentReference()
        {
            RequireGpu();
            var generated = Generate(CreateMultiPage());
            using (var session = CreateSession(generated))
            {
                Configure(session);
                session.Step(Texture(new Color(1, 0, 0, 0), Color.clear));
                AssertValue(session, generated, "particle.velocity", 0, new Vector4(2, 0, 0, 0));
                AssertValue(session, generated, "particle.spawn-key", 0, new Vector4(123, 0, 0, 0));
                AssertValue(session, generated, "custom.mode", 0, new Vector4(7, 0, 0, 0));
                Assert.IsTrue(generated.Compilation.StateLayout.TryGetBinding("custom.mode", out var binding));
                var previous = session.CurrentPages[binding.PageId];
                var pageWrittenWithoutSpawn = generated.Passes.Any(pass =>
                    pass.Plan.Condition != FluxParticlePassCondition.HasSpawn && pass.Plan.OutputPageId == binding.PageId);
                session.Step();
                if (pageWrittenWithoutSpawn)
                {
                    Assert.AreNotSame(previous, session.CurrentPages[binding.PageId]);
                }
                else
                {
                    Assert.AreSame(previous, session.CurrentPages[binding.PageId]);
                }
                AssertValue(session, generated, "custom.mode", 0, new Vector4(7, 0, 0, 0));
                session.Step(Texture(Color.clear, new Color(1, 0, 0, 0)));
                AssertValue(session, generated, "custom.mode", 0, new Vector4(7, 0, 0, 0));
                AssertValue(session, generated, "custom.mode", 1, new Vector4(7, 0, 0, 0));
            }
        }

        [Test]
        public void GpuBindingsRejectMissingWrongTypeAndStaticParameterChanges()
        {
            RequireGpu();
            var generated = Generate();
            using (var session = CreateSession(generated))
            {
                Assert.Throws<InvalidOperationException>(() => session.Step());
                Configure(session);
                Assert.Throws<ArgumentException>(() => session.SetFloat("gravity", 1));
                Assert.Throws<ArgumentException>(() => session.SetSpawnInput("spawn.key", Texture(Color.white)));
                session.Step();
                Assert.Throws<InvalidOperationException>(() => session.SetVector("color.start", Vector4.zero));
                Assert.DoesNotThrow(() => session.SetFloat("gravity.enabled", 0));
            }
        }

        FluxParticleDefinitionBuilder CreateMultiPage()
        {
            var definitions = CreateDefinitions();
            var changed = Copy(definitions, "spawn.velocity");
            changed.AddValue(new FluxParticleValueDefinition("custom.mode", FluxParticleValueType.Float,
                FluxParticleWritePolicy.SharedMutable, "fluxfx.core"));
            var path = WriteInclude("SpawnMulti", "void FluxFX_SpawnMulti(inout FluxSpawnMultiContext ctx)\n{\n" +
                "    ctx.valueParticleVelocity = ctx.parameterSpawnVelocity;\n" +
                "    ctx.valueParticleSpawnKey = ctx.valueSpawnKey;\n    ctx.valueCustomMode = 7.0;\n}\n");
            var source = definitions.Operations["spawn.velocity"];
            changed.AddOperation(new FluxParticleOperationDefinition("spawn.multi", source.OwnerId, source.Phase,
                source.Reads, source.Writes.Concat(new[] { new FluxParticleValueWrite("custom.mode", FluxParticleWriteKind.Initialize) }),
                source.Dependencies, path, "FluxFX_SpawnMulti", source.Parameters));
            return changed;
        }

        static FluxParticleDefinitionBuilder Copy(FluxParticleDefinitionBuilder source, string exclude)
        {
            var result = new FluxParticleDefinitionBuilder();
            foreach (var value in source.Values.Values) result.AddValue(value);
            foreach (var provider in source.Providers.Values) result.AddProvider(provider);
            foreach (var parameter in source.Parameters.Values) result.AddParameter(parameter);
            foreach (var op in source.Operations.Values.Where(op => op.Id != exclude)) result.AddOperation(op);
            return result;
        }
        static FluxParticleDefinitionBuilder CreateDefinitions()
        {
            var definitions = FluxParticleCoreDefinitions.Create();
            var registry = new FluxParticleModuleRegistry();
            var modules = new FluxParticleModule[]
            {
                new FluxParticleGravityModule(),
                new FluxParticleDragModule(),
                new FluxParticleColorOverLifetimeModule()
            };
            registry.Contribute(modules, new FluxParticleProgramContribution(definitions));
            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            return definitions;
        }


        static FluxParticleShaderProgram Generate(FluxParticleDefinitionBuilder definitions = null)
        {
            return new FluxParticleProgramShaderGenerator().Generate(new FluxParticleProgramCompiler().Compile(definitions ?? CreateDefinitions()),
                "Hidden/FluxFX/Phase2Tests", FluxParticleCoreDefinitions.CreateLifecycle());
        }

        FluxParticleProgramGpuSession CreateSession(FluxParticleShaderProgram generated)
        {
            var shaders = ImportShaders(generated);
            return new FluxParticleProgramGpuSession(generated, shaders.steps, shaders.clears, 2, 1);
        }

        (Shader[] steps, Shader[] clears) ImportShaders(FluxParticleShaderProgram generated)
        {
            var steps = generated.Passes.Select((pass, i) => Import("Step" + i, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, i) => Import("Clear" + i, source)).ToArray();
            return (steps, clears);
        }

        void EnsureTemporaryFolder()
        {
            if (AssetDatabase.IsValidFolder(temporaryFolder)) return;
            AssetDatabase.CreateFolder("Assets/FluxFX/Tests", Path.GetFileName(temporaryFolder));
        }

        Shader Import(string name, string source, bool expectSuccess = true)
        {
            EnsureTemporaryFolder();
            var path = temporaryFolder + "/" + name + ".shader";
            File.WriteAllText(path, source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.IsNotNull(shader, path);
            if (expectSuccess)
            {
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(message => message.message)));
                Assert.IsTrue(shader.isSupported, path);
            }
            return shader;
        }

        string WriteInclude(string name, string source)
        {
            EnsureTemporaryFolder();
            var path = temporaryFolder + "/" + name + ".hlsl";
            File.WriteAllText(path, source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return path;
        }

        static string RenderShader(FluxParticleShaderProgram generated)
        {
            return "Shader \"Hidden/FluxFX/Phase2Tests/Render\" { SubShader { Pass { ZTest Always Cull Off ZWrite Off Blend Off\n" +
                "HLSLPROGRAM\n#pragma target 3.5\n#pragma vertex vert_img\n#pragma fragment frag\n#include \"UnityCG.cginc\"\n" +
                generated.RenderSource + "\nfloat4 frag(v2f_img i) : SV_Target { bool alive; FluxRenderContext ctx = FluxFX_EvaluateRender(i.uv, alive); " +
                "return alive ? ctx.valueRenderColor : float4(0,0,0,0); }\nENDHLSL\n} } Fallback Off }";
        }

        void Configure(FluxParticleProgramGpuSession session, float lifetime = 10)
        {
            session.SetVector("spawn.position", new Vector4(10, 0, 0, 0));
            session.SetVector("spawn.velocity", new Vector4(2, 0, 0, 0));
            session.SetVector("gravity", new Vector4(0, -2, 0, 0));
            session.SetFloat("gravity.enabled", 1);
            session.SetFloat("drag", 0.4f);
            session.SetFloat("drag.enabled", 1);
            session.SetVector("lifetime.range", new Vector4(lifetime, lifetime, 0, 0));
            session.SetVector("color.start", Vector4.one);
            session.SetVector("color.end", Vector4.zero);
            session.SetUniformFloat("step.delta-time", 0.25f);
            session.SetSpawnInput("spawn.key", Texture(new Color(123, 0, 0, 0), new Color(456, 0, 0, 0)));
        }

        Texture2D Texture(params Color[] colors)
        {
            var texture = Own(new Texture2D(colors.Length, 1, TextureFormat.RGBAFloat, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            });
            texture.SetPixels(colors);
            texture.Apply(false, false);
            return texture;
        }

        T Own<T>(T value) where T : UnityEngine.Object
        {
            objects.Add(value);
            return value;
        }

        static void RequireGpu()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat)) Assert.Ignore("GPU with RGBAFloat textures is required. Do not use -nographics.");
        }

        static Color[] ReadPixels(RenderTexture source)
        {
            var active = RenderTexture.active;
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
            try
            {
                RenderTexture.active = source;
                texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                texture.Apply();
                return texture.GetPixels();
            }
            finally
            {
                RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        static Vector4 ReadValue(FluxParticleProgramGpuSession session, FluxParticleShaderProgram generated, string id, int particle)
        {
            Assert.IsTrue(generated.Compilation.StateLayout.TryGetBinding(id, out var binding));
            var pixel = (Vector4)ReadPixels(session.CurrentPages[binding.PageId])[particle];
            var value = Vector4.zero;
            for (var i = 0; i < binding.ComponentCount; i++) value[i] = pixel[binding.ChannelOffset + i];
            return value;
        }

        static void AssertValue(FluxParticleProgramGpuSession session, FluxParticleShaderProgram generated, string id, int particle, Vector4 expected)
        {
            var actual = ReadValue(session, generated, id, particle);
            Assert.IsTrue(generated.Compilation.StateLayout.TryGetBinding(id, out var binding));
            for (var i = 0; i < binding.ComponentCount; i++) Assert.That(actual[i], Is.EqualTo(expected[i]).Within(1e-4), id + "[" + i + "]");
        }
    }
}
