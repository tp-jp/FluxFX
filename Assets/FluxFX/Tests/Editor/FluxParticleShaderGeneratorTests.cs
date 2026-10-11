using NUnit.Framework;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Editor.Shaders.Artifacts;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class FluxParticleShaderGeneratorTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;
        FluxParticleGpuCompiler _compiler;
        FluxParticleShaderGenerator _generator;
        ShaderArtifactBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Shader Generator Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _compiler = new FluxParticleGpuCompiler(new FluxParticleGpuModuleRegistry());
            _generator = new FluxParticleShaderGenerator();
            _builder = new ShaderArtifactBuilder();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void EmptyConfigurationContainsNoModuleCalls()
        {
            var source = Generate().Source;

            Assert.IsFalse(source.Contains("FluxFX_Gravity(ctx);"));
            Assert.IsFalse(source.Contains("FluxFX_Force(ctx);"));
            Assert.IsFalse(source.Contains("FluxFX_Noise(ctx);"));
            Assert.IsFalse(source.Contains("FluxFX_Vortex(ctx);"));
            Assert.IsFalse(source.Contains("FluxFX_Drag(ctx);"));
            Assert.IsFalse(source.Contains("FluxFX_LimitVelocity(ctx);"));

            Assert.IsFalse(source.Contains("{{MODULE_PROPERTIES}}"));
            Assert.IsFalse(source.Contains("{{MODULE_UNIFORMS}}"));
            Assert.IsFalse(source.Contains("{{MODULE_INCLUDES}}"));
            Assert.IsFalse(source.Contains("{{MODULE_CALLS}}"));
        }

        [Test]
        public void StandardModulesProduceExpectedShader()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleNoiseModule());
            AddModule(new FluxParticleVortexModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleLimitVelocityModule());

            var source = Generate().Source;

            AssertModule(source, "Gravity", "_Gravity", "Vector");
            AssertModule(source, "Force", "_Force", "Vector");
            AssertModule(source, "Noise", "_NoiseStrength", "Float");
            AssertModule(source, "Vortex", "_VortexCenter", "Vector");
            AssertModule(source, "Drag", "_Drag", "Float");
            AssertModule(source, "LimitVelocity", "_MaxSpeed", "Float");

            StringAssert.Contains("_NoiseScale (\"_NoiseScale\", Float)", source);
            StringAssert.Contains("_NoiseSpeed (\"_NoiseSpeed\", Float)", source);
            StringAssert.Contains("_VortexAxis (\"_VortexAxis\", Vector)", source);
            StringAssert.Contains("_VortexStrength (\"_VortexStrength\", Float)", source);

            Assert.IsFalse(source.Contains("_NoiseTime"));
            Assert.IsFalse(source.Contains("FluxFX_LegacyForce"));
            Assert.IsFalse(source.Contains("FluxFX_LegacyNoise"));
            Assert.IsFalse(source.Contains("FluxFX_LegacyVortex"));
            Assert.IsFalse(source.Contains("FluxFX_LegacyLimitVelocity"));
        }

        [Test]
        public void UnregisteredModulesAreNotGenerated()
        {
            AddModule(new FluxParticleGravityModule());

            var source = Generate().Source;

            StringAssert.Contains("FluxFX_Gravity(ctx);", source);
            Assert.IsFalse(source.Contains("FluxFX_Vortex(ctx);"));
            Assert.IsFalse(source.Contains("_VortexCenter"));
            Assert.IsFalse(source.Contains("_VortexAxis"));
            Assert.IsFalse(source.Contains("_VortexStrength"));
        }

        [Test]
        public void GeneratorPreservesExecutionOrder()
        {
            AddModule(new FluxParticleLimitVelocityModule());
            AddModule(new FluxParticleVortexModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleNoiseModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleGravityModule());

            var source = Generate().Source;

            AssertExecutionOrder(source,
                "FluxFX_Gravity(ctx);",
                "FluxFX_Force(ctx);",
                "FluxFX_Noise(ctx);",
                "FluxFX_Vortex(ctx);",
                "FluxFX_Drag(ctx);",
                "FluxFX_LimitVelocity(ctx);");
        }

        [Test]
        public void DisabledModuleIsStillGeneratedWithEnabledGuard()
        {
            AddModule(new FluxParticleForceModule());
            SetModuleEnabled(0, false);

            var source = Generate().Source;
            var enabledName = FluxParticleGpuModuleEnabled.GetParameterName("tplab.fluxfx.force");

            StringAssert.Contains("FluxFX_Force(ctx);", source);
            StringAssert.Contains("_Force (\"_Force\", Vector)", source);
            StringAssert.Contains($"if ({enabledName} > 0.5)", source);
        }

        [Test]
        public void GeneratedShaderUsesResolvedIncludeArtifact()
        {
            AddModule(new FluxParticleForceModule());

            var artifact = Build();
            var sourcePath = "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Force.hlsl";
            var generatedPath = artifact.Includes.Get(sourcePath).AssetPath;

            StringAssert.Contains($"#include \"{generatedPath}\"", artifact.Source);
            Assert.IsFalse(artifact.Source.Contains($"#include \"{sourcePath}\""));
        }

        [Test]
        public void EnabledDoesNotAffectShaderArtifact()
        {
            AddModule(new FluxParticleGravityModule());

            var first = Build();

            SetModuleEnabled(0, false);

            var second = Build();

            Assert.AreEqual(first.ArtifactId, second.ArtifactId);
            Assert.AreEqual(first.Source, second.Source);
        }

        [Test]
        public void InitialValueDoesNotAffectShaderArtifact()
        {
            AddModule(new FluxParticleGravityModule());

            var first = Build();

            var serializedObject = new SerializedObject(_authoring);
            serializedObject.FindProperty("modules")
                .GetArrayElementAtIndex(0)
                .FindPropertyRelative("gravity").vector3Value = new Vector3(3, -9, 2);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var second = Build();

            Assert.AreEqual(first.ArtifactId, second.ArtifactId);
            Assert.AreEqual(first.Source, second.Source);
        }

        [Test]
        public void SameConfigurationProducesSameArtifact()
        {
            AddModule(new FluxParticleGravityModule());

            var first = Build();
            var second = Build();

            Assert.AreEqual(first.ArtifactId, second.ArtifactId);
            Assert.AreEqual(first.Source, second.Source);
        }

        [Test]
        public void DifferentModulesProduceDifferentArtifacts()
        {
            AddModule(new FluxParticleGravityModule());

            var first = Build();

            AddModule(new FluxParticleNoiseModule());

            var second = Build();

            Assert.AreNotEqual(first.ArtifactId, second.ArtifactId);
        }

        [Test]
        public void CompilationCanBeReusedWithoutRecompiling()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleNoiseModule());

            var compilation = Compile();

            var first = _builder.Build(_generator.GenerateVelocity(compilation));
            var second = _builder.Build(_generator.GenerateVelocity(compilation));

            Assert.AreEqual(first.ArtifactId, second.ArtifactId);
            Assert.AreEqual(first.Source, second.Source);
        }

        [Test]
        public void ShaderPipelineReusesGeneratedShaderAsset()
        {
            AddModule(new FluxParticleGravityModule());

            var pipeline = new FluxParticleShaderPipeline();
            var artifact = pipeline.BuildVelocity(Compile());

            var first = pipeline.GetOrCreate(artifact);
            var second = pipeline.GetOrCreate(artifact);

            Assert.IsNotNull(first);
            Assert.AreSame(first, second);
            Assert.AreEqual(artifact.ShaderName, first.name);
        }

        static void AssertModule(string source, string entryPoint, string parameter, string parameterType)
        {
            StringAssert.Contains($"FluxFX_{entryPoint}(ctx);", source);
            StringAssert.Contains($"{parameter} (\"{parameter}\", {parameterType})", source);
        }

        static void AssertExecutionOrder(string source, params string[] calls)
        {
            var previous = -1;

            foreach (var call in calls)
            {
                var index = source.IndexOf(call);

                Assert.Greater(index, previous, $"Invalid or missing execution order: {call}");

                previous = index;
            }
        }

        FluxParticleGpuCompilation Compile()
        {
            var result = _compiler.Compile(FluxParticleCompilePlan.Create(_authoring));

            Assert.IsTrue(result.Success, result.Validation.GetReport());

            return result.Compilation;
        }

        FluxParticleShaderDraft Generate()
        {
            return _generator.GenerateVelocity(Compile());
        }

        ShaderArtifact Build()
        {
            return _builder.Build(Generate());
        }

        void SetModuleEnabled(int index, bool enabled)
        {
            var serializedObject = new SerializedObject(_authoring);
            serializedObject.FindProperty("modules")
                .GetArrayElementAtIndex(index)
                .FindPropertyRelative("enabled").boolValue = enabled;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        void AddModule(FluxParticleModule module)
        {
            var serializedObject = new SerializedObject(_authoring);
            var modules = serializedObject.FindProperty("modules");

            var index = modules.arraySize;
            modules.arraySize++;
            modules.GetArrayElementAtIndex(index).managedReferenceValue = module;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}