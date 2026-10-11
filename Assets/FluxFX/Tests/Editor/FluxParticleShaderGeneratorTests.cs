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
        FluxParticleGpuModuleRegistry _registry;
        FluxParticleGpuCompiler _compiler;
        FluxParticleShaderGenerator _generator;
        ShaderArtifactBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Shader Generator Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _registry = new FluxParticleGpuModuleRegistry();
            _compiler = new FluxParticleGpuCompiler(_registry);
            _generator = new FluxParticleShaderGenerator();
            _builder = new ShaderArtifactBuilder();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void EmptyConfigurationMatchesLegacyShader()
        {
            AssertMatchesLegacy();
        }

        [Test]
        public void StandardModulesMatchLegacyShader()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleNoiseModule());
            AddModule(new FluxParticleVortexModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleLimitVelocityModule());

            AssertMatchesLegacy();
        }

        [Test]
        public void EnabledDoesNotAffectShaderArtifact()
        {
            AddModule(new FluxParticleGravityModule());

            var first = Build();

            var serializedObject = new SerializedObject(_authoring);
            serializedObject.FindProperty("modules")
                .GetArrayElementAtIndex(0)
                .FindPropertyRelative("enabled").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

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
        public void DifferentModulesProduceDifferentArtifacts()
        {
            AddModule(new FluxParticleGravityModule());

            var first = Build();

            AddModule(new FluxParticleNoiseModule());

            var second = Build();

            Assert.AreNotEqual(first.ArtifactId, second.ArtifactId);
        }

        [Test]
        public void GeneratorPreservesExecutionOrder()
        {
            AddModule(new FluxParticleLimitVelocityModule());
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleDragModule());

            var source = Generate().Source;

            var gravity = source.IndexOf("FluxFX_Gravity(ctx);");
            var drag = source.IndexOf("FluxFX_Drag(ctx);");
            var limit = source.IndexOf("FluxFX_LimitVelocity(ctx);");

            Assert.GreaterOrEqual(gravity, 0);
            Assert.Greater(drag, gravity);
            Assert.Greater(limit, drag);
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

        void AssertMatchesLegacy()
        {
            var plan = FluxParticleCompilePlan.Create(_authoring);

            var legacy = new FluxParticleShaderCompiler(_registry).CompileVelocity(plan);
            var current = Generate();

            Assert.AreEqual(legacy.Source, current.Source);

            var legacyArtifact = _builder.Build(legacy);
            var currentArtifact = _builder.Build(current);

            Assert.AreEqual(legacyArtifact.ArtifactId, currentArtifact.ArtifactId);
            Assert.AreEqual(legacyArtifact.Source, currentArtifact.Source);
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