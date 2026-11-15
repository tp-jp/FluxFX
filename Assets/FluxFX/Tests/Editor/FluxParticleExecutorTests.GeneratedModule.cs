using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Editor.ModuleCreation;
using TpLab.Flux.FX.Editor.Modules;
using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [Test]
        public void GeneratedModulesAreDiscoveredAddedAndSaved()
        {
            var definitions = GeneratedDefinitions();
            var registry = new FluxParticleInspectorModuleRegistry();
            var inspector = UnityEditor.Editor.CreateEditor(authoring);
            try
            {
                foreach (var definition in definitions)
                {
                    Assert.IsTrue(registry.TryGet(definition.ModuleType, out var metadata));
                    Assert.IsTrue(metadata.IsCustom);
                    inspector.GetType().GetMethod("AddModule", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(inspector, new object[] { metadata });
                }
                Assert.AreEqual(2, authoring.Modules.Count);
                var path = folder + "/Generated.prefab";
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                var loaded = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    CollectionAssert.AreEquivalent(definitions.Select(definition => definition.ModuleType),
                        loaded.GetComponent<FluxParticleAuthoring>().Modules.Select(module => module.GetType()));
                }
                finally { PrefabUtility.UnloadPrefabContents(loaded); }
                Assert.IsNotEmpty(FluxParticleModuleCreator.Validate("GeneratedAccelerationA", "FluxFXValidation", "Assets"));
            }
            finally { Object.DestroyImmediate(inspector); }
        }

        [Test]
        public void GeneratedModulesComposeAndRuntimeHandleUpdatesGpu()
        {
            RequireGraphics();
            var definitions = GeneratedDefinitions();
            SetModules(definitions.Select(definition => (FluxParticleModule)Activator.CreateInstance(definition.ModuleType)).ToArray());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            Assert.IsTrue(runtime.SetFloat("spawn.speed", 0));
            runtime.Step(0, 0, 1, 0);
            runtime.Step(0.5f, 0, 0, 0);
            AssertVector(new Vector4(0, 2, 0, 0), ReadValue(runtime, compiled, "particle.velocity", 0));
            var handleType = TypeCache.GetTypesDerivedFrom<TpLab.Flux.FX.Udon.Modules.FluxParticleModuleHandle>()
                .Single(type => type.FullName == "FluxFXValidation.GeneratedAccelerationB.Udon.GeneratedAccelerationBHandle");
            var extensions = handleType.Assembly.GetType("FluxFXValidation.GeneratedAccelerationB.Udon.GeneratedAccelerationBHandleExtensions", true);
            var modules = runtime.GetComponentInParent<FluxParticleSystem>().Modules;
            var handle = extensions.GetMethod("GeneratedAccelerationB").Invoke(null, new object[] { modules });
            Assert.IsNotNull(handle);
            Assert.IsTrue((bool)extensions.GetMethod("SetStrength").Invoke(null, new[] { handle, (object)4f }));
            Assert.IsTrue((bool)extensions.GetMethod("SetDirection").Invoke(null, new[] { handle, (object)Vector3.right }));
            Assert.IsFalse((bool)extensions.GetMethod("SetStrength").Invoke(null, new[] { handle, (object)float.NaN }));
            runtime.Step(0.5f, 0, 0, 0);
            AssertVector(new Vector4(2, 3, 0, 0), ReadValue(runtime, compiled, "particle.velocity", 0));
        }

        [Test]
        public void GeneratedModuleHlslEditChangesRebuiltPreview()
        {
            RequireGraphics();
            var definition = GeneratedDefinitions()[0];
            SetModules((FluxParticleModule)Activator.CreateInstance(definition.ModuleType));
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("initialVelocity.speed").floatValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var build = FluxParticleBuildPipeline.Build(authoring);
            var operation = build.Shaders.Compilation.Operations.Single(item => item.OwnerId == definition.ModuleId);
            var path = operation.HlslPath;
            var original = File.ReadAllText(path);
            var first = GeneratedPreviewVelocity(build);
            Assert.Greater(first.y, 0);
            try
            {
                File.WriteAllText(path, original.Replace("ctx.valueParticleVelocity += acceleration", "ctx.valueParticleVelocity -= acceleration"));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var changed = GeneratedPreviewVelocity(FluxParticleBuildPipeline.Build(authoring));
                Assert.AreEqual(-first.y, changed.y, 0.001f);
            }
            finally
            {
                File.WriteAllText(path, original);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        Vector4 GeneratedPreviewVelocity(FluxParticleBuildResult build)
        {
            using (var preview = new FluxParticleEditorPreview(build, authoring, 4))
            {
                preview.Step(0.5f);
                preview.Step(0.5f);
                var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview)
                    .GetField("session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
                Assert.IsTrue(build.Shaders.Compilation.StateLayout.TryGetBinding("particle.velocity", out var binding));
                var raw = ReadTexture(session.CurrentPages[binding.PageId], 0);
                var value = Vector4.zero;
                for (var i = 0; i < binding.ComponentCount; i++) value[i] = raw[binding.ChannelOffset + i];
                return value;
            }
        }

        static IFluxParticleModuleDefinition[] GeneratedDefinitions()
        {
            var definitions = new FluxParticleModuleRegistry().GetAll().Where(definition =>
                definition.ModuleId == "custom.fluxfxvalidation.generatedaccelerationa" ||
                definition.ModuleId == "custom.fluxfxvalidation.generatedaccelerationb").OrderBy(definition => definition.ModuleId).ToArray();
            if (definitions.Length != 2) Assert.Ignore("Generate GeneratedAccelerationA (minimal) and GeneratedAccelerationB (Runtime API) in namespace FluxFXValidation before running generated-source integration tests.");
            return definitions;
        }
    }
}
