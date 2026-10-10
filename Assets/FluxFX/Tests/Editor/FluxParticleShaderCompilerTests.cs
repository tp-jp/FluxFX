using NUnit.Framework;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;

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

            StringAssert.Contains("FluxFX_Gravity(velocity, deltaTime);", result.Source);
            Assert.IsFalse(result.Source.Contains("FluxFX_Drag(velocity, deltaTime);"));
        }

        [Test]
        public void DragOnly()
        {
            AddModule(new FluxParticleDragModule());

            var result = Compile();

            StringAssert.Contains("FluxFX_Drag(velocity, deltaTime);", result.Source);
            Assert.IsFalse(result.Source.Contains("FluxFX_Gravity(velocity, deltaTime);"));
        }

        [Test]
        public void GravityAndDragPreserveOrder()
        {
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleGravityModule());

            var result = Compile();

            var gravityIndex = result.Source.IndexOf("FluxFX_Gravity(velocity, deltaTime);");
            var dragIndex = result.Source.IndexOf("FluxFX_Drag(velocity, deltaTime);");

            Assert.GreaterOrEqual(gravityIndex, 0);
            Assert.Greater(dragIndex, gravityIndex);
        }

        [Test]
        public void SameConfigurationProducesSameCacheKey()
        {
            AddModule(new FluxParticleGravityModule());

            var first = Compile();
            var second = Compile();

            Assert.AreEqual(first.CacheKey, second.CacheKey);
            Assert.AreEqual(first.Source, second.Source);
        }

        [Test]
        public void DifferentConfigurationsProduceDifferentCacheKeys()
        {
            AddModule(new FluxParticleGravityModule());
            var first = Compile();

            AddModule(new FluxParticleDragModule());
            var second = Compile();

            Assert.AreNotEqual(first.CacheKey, second.CacheKey);
        }

        [Test]
        public void CacheReusesShaderAsset()
        {
            AddModule(new FluxParticleGravityModule());

            var compilation = Compile();
            var cache = new FluxParticleShaderCache();

            var first = cache.GetOrCreate(compilation);
            var second = cache.GetOrCreate(compilation);

            Assert.IsNotNull(first);
            Assert.AreSame(first, second);
            Assert.AreEqual(compilation.ShaderName, first.name);
        }

        FluxParticleShaderCompilation Compile()
        {
            return _compiler.CompileVelocity(FluxParticleCompilePlan.Create(_authoring));
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
