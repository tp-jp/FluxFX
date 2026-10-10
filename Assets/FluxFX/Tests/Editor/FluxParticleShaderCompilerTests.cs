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
    public class FluxParticleShaderCompilerTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;
        FluxParticleShaderCompiler _compiler;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("FluxFX Shader Compiler Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _compiler = new FluxParticleShaderCompiler(new FluxParticleGpuModuleRegistry());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void GravityOnly()
        {
            AddModule(new FluxParticleGravityModule());

            var result = Compile();

            StringAssert.Contains("FluxFX_Gravity(ctx);", result.Source);
            StringAssert.Contains("_Gravity (\"_Gravity\", Vector)", result.Source);
            Assert.IsFalse(result.Source.Contains("FluxFX_Drag(ctx);"));
        }

        [Test]
        public void DragOnly()
        {
            AddModule(new FluxParticleDragModule());

            var result = Compile();

            StringAssert.Contains("FluxFX_Drag(ctx);", result.Source);
            StringAssert.Contains("_Drag (\"_Drag\", Float)", result.Source);
            Assert.IsFalse(result.Source.Contains("FluxFX_Gravity(ctx);"));
        }

        [Test]
        public void GravityAndDragPreserveOrder()
        {
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleGravityModule());

            var result = Compile();

            var gravityIndex = result.Source.IndexOf("FluxFX_Gravity(ctx);");
            var dragIndex = result.Source.IndexOf("FluxFX_Drag(ctx);");

            Assert.GreaterOrEqual(gravityIndex, 0);
            Assert.Greater(dragIndex, gravityIndex);
        }

        [Test]
        public void ForceOnly()
        {
            AddModule(new FluxParticleForceModule());

            var result = Compile();
            var sourcePath = "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Force.hlsl";
            var includePath = result.Includes.Get(sourcePath).AssetPath;

            StringAssert.Contains("FluxFX_Force(ctx);", result.Source);
            StringAssert.Contains("_Force (\"_Force\", Vector)", result.Source);
            StringAssert.Contains($"#include \"{includePath}\"", result.Source);
            Assert.IsFalse(result.Source.Contains($"#include \"{sourcePath}\""));
            Assert.IsFalse(result.Source.Contains("FluxFX_LegacyForce(ctx);"));
        }

        [Test]
        public void ForceDisabled()
        {
            AddModule(new FluxParticleForceModule());

            var serializedObject = new SerializedObject(_authoring);
            var modules = serializedObject.FindProperty("modules");
            modules.GetArrayElementAtIndex(0).FindPropertyRelative("enabled").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var result = Compile();

            Assert.IsFalse(result.Source.Contains("FluxFX_Force(ctx);"));
            Assert.IsFalse(result.Source.Contains("_Force (\"_Force\", Vector)"));
            Assert.IsFalse(result.Source.Contains("#include \"Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Force.hlsl\""));
        }

        [Test]
        public void SameConfigurationProducesSameArtifactId()
        {
            AddModule(new FluxParticleGravityModule());

            var first = Compile();
            var second = Compile();

            Assert.AreEqual(first.ArtifactId, second.ArtifactId);
            Assert.AreEqual(first.Source, second.Source);
        }

        [Test]
        public void DifferentConfigurationsProduceDifferentArtifactIds()
        {
            AddModule(new FluxParticleGravityModule());
            var first = Compile();

            AddModule(new FluxParticleDragModule());
            var second = Compile();

            Assert.AreNotEqual(first.ArtifactId, second.ArtifactId);
        }

        [Test]
        public void CacheReusesShaderAsset()
        {
            AddModule(new FluxParticleGravityModule());

            var pipeline = new FluxParticleShaderPipeline(new FluxParticleGpuModuleRegistry());
            var artifact = pipeline.BuildVelocity(FluxParticleCompilePlan.Create(_authoring));

            var first = pipeline.GetOrCreate(artifact);
            var second = pipeline.GetOrCreate(artifact);

            Assert.IsNotNull(first);
            Assert.AreSame(first, second);
            Assert.AreEqual(artifact.ShaderName, first.name);
        }

        [Test]
        public void LimitVelocityOnly()
        {
            AddModule(new FluxParticleLimitVelocityModule());

            var result = Compile();
            var sourcePath = "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/LimitVelocity.hlsl";
            var includePath = result.Includes.Get(sourcePath).AssetPath;

            StringAssert.Contains("FluxFX_LimitVelocity(ctx);", result.Source);
            StringAssert.Contains("_MaxSpeed (\"_MaxSpeed\", Float)", result.Source);
            StringAssert.Contains("float _MaxSpeed;", result.Source);
            StringAssert.Contains($"#include \"{includePath}\"", result.Source);
            Assert.IsFalse(result.Source.Contains($"#include \"{sourcePath}\""));
            Assert.IsFalse(result.Source.Contains("FluxFX_LegacyLimitVelocity"));
        }

        [Test]
        public void LimitVelocityDisabled()
        {
            AddModule(new FluxParticleLimitVelocityModule());

            var serializedObject = new SerializedObject(_authoring);
            var modules = serializedObject.FindProperty("modules");
            modules.GetArrayElementAtIndex(0).FindPropertyRelative("enabled").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var result = Compile();

            Assert.IsFalse(result.Source.Contains("FluxFX_LimitVelocity(ctx);"));
            Assert.IsFalse(result.Source.Contains("_MaxSpeed (\"_MaxSpeed\", Float)"));
            Assert.IsFalse(result.Source.Contains("float _MaxSpeed;"));
            Assert.IsFalse(result.Source.Contains("Modules/LimitVelocity.hlsl"));
        }

        [Test]
        public void LimitVelocityRunsAfterDrag()
        {
            AddModule(new FluxParticleLimitVelocityModule());
            AddModule(new FluxParticleDragModule());

            var result = Compile();

            var dragIndex = result.Source.IndexOf("FluxFX_Drag(ctx);");
            var limitIndex = result.Source.IndexOf("FluxFX_LimitVelocity(ctx);");

            Assert.GreaterOrEqual(dragIndex, 0);
            Assert.Greater(limitIndex, dragIndex);
        }

        ShaderArtifact Compile()
        {
            var pipeline = new FluxParticleShaderPipeline(new FluxParticleGpuModuleRegistry());
            return pipeline.BuildVelocity(FluxParticleCompilePlan.Create(_authoring));
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