using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Editor.Compilation.Modules;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleExecutorTests
    {
        GameObject root;
        FluxParticleAuthoring authoring;
        readonly List<FluxParticleExecutor> runtimes = new List<FluxParticleExecutor>();
        readonly List<Material> kernelMaterials = new List<Material>();
        string folder;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Particle Executor Test");
            authoring = root.AddComponent<FluxParticleAuthoring>();
            folder = "Assets/FluxFX/Tests/ParticleExecutor_" + Guid.NewGuid().ToString("N");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // UdonSharp queues backing UdonBehaviour initialization via EditorApplication.delayCall.
            // Wait for callbacks queued by AddUdonSharpComponent before destroying their GameObjects.
            var delayedCallbacksCompleted = false;
            EditorApplication.delayCall += () => delayedCallbacksCompleted = true;
            while (!delayedCallbacksCompleted)
            {
                yield return null;
            }

            foreach (var runtime in runtimes)
            {
                if (runtime != null)
                {
                    runtime.Release();
                }
            }
            runtimes.Clear();
            foreach (var material in kernelMaterials)
            {
                if (material != null) Object.DestroyImmediate(material);
            }
            kernelMaterials.Clear();
            Object.DestroyImmediate(root);
            if (AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void AuthoringUsesTwoPagesAndMapsInitialValues()
        {
            var program = FluxParticleBuildPipeline.Build(authoring);
            Assert.AreEqual(2, program.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.AreEqual(2, program.Shaders.Passes.Count);
            Assert.AreEqual(1, program.InitialValues["spawn.speed"].x);
            Assert.IsFalse(program.InitialValues.ContainsKey("gravity.enabled"));
            Assert.IsFalse(program.Shaders.Compilation.Operations.Any(op => op.Id == "render.color"));
        }

        [TestCase("simulationSpace", 1)]
        [TestCase("render.mode", 1)]

        public void UnsupportedSettingsFailBeforeGeneratingAssets(string path, int value)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty(path).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            StringAssert.Contains("Program Backend", Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring)).Message);
            Assert.IsFalse(AssetDatabase.IsValidFolder(folder));
        }

        [Test]
        public void UnsupportedAndDuplicateModulesAreRejected()
        {
            SetModules(new FluxParticleNoiseModule());
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
            SetModules(new FluxParticleGravityModule(), new FluxParticleGravityModule());
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
        }

        [Test]
        public void RuntimeManifestPreservesResolvedBindingsAndImportsRenderer()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            CollectionAssert.AreEqual(compiled.Shaders.Passes.Select(p => p.Plan.OutputPageId), (int[])runtime.GetProgramVariable("outputPages"));
            CollectionAssert.AreEqual(compiled.Shaders.Passes.SelectMany(p => p.Reads).Select(r => FluxParticleHlslNames.PageProperty(r.PageId, r.Version)),
                (string[])runtime.GetProgramVariable("readProperties"));
            var reads = compiled.Shaders.Passes.SelectMany(pass => pass.Reads).ToArray();
            var offsets = new List<int> { 0 };
            foreach (var pass in compiled.Shaders.Passes)
            {
                offsets.Add(offsets.Last() + pass.Reads.Count);
            }
            CollectionAssert.AreEqual(offsets, (int[])runtime.GetProgramVariable("readOffsets"));
            CollectionAssert.AreEqual(reads.Select(read => read.PageId), (int[])runtime.GetProgramVariable("readPages"));
            CollectionAssert.AreEqual(reads.Select(read => read.Version == FluxParticlePageVersion.Updated),
                (bool[])runtime.GetProgramVariable("readUpdated"));
            CollectionAssert.AreEqual(compiled.Shaders.Passes.Select(pass => pass.Plan.Condition == FluxParticlePassCondition.HasSpawn),
                (bool[])runtime.GetProgramVariable("spawnOnly"));
            var parameters = compiled.Shaders.Compilation.Parameters.Values.OrderBy(parameter => parameter.Id, StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(parameters.Select(parameter => parameter.Id), (string[])runtime.GetProgramVariable("parameterIds"));
            CollectionAssert.AreEqual(parameters.Select(parameter => FluxParticleHlslNames.ParameterProperty(parameter.Id)),
                (string[])runtime.GetProgramVariable("parameterProperties"));
            CollectionAssert.AreEqual(parameters.Select(parameter => (int)parameter.Type), (int[])runtime.GetProgramVariable("parameterTypes"));
            CollectionAssert.AreEqual(parameters.Select(parameter => parameter.Policy == FluxParticleParameterPolicy.Static),
                (bool[])runtime.GetProgramVariable("staticParameters"));
            CollectionAssert.AreEqual(parameters.Select(parameter => compiled.InitialValues[parameter.Id]),
                (Vector4[])runtime.GetProgramVariable("parameterValues"));
            var kernels = (FluxKernel[])runtime.GetProgramVariable("stepKernels");
            Assert.AreEqual(compiled.Shaders.Passes.Count, kernels.Length);
            foreach (var kernel in kernels)
            {
                Assert.IsFalse(ShaderUtil.ShaderHasError(kernel.Shader));
                Assert.IsTrue(kernel.Shader.isSupported);
            }
            var renderer = (Material)runtime.GetProgramVariable("renderMaterial");
            Assert.IsFalse(ShaderUtil.ShaderHasError(renderer.shader));
            Assert.IsTrue(renderer.shader.isSupported);
            foreach (var parameter in parameters)
            {
                var property = FluxParticleHlslNames.ParameterProperty(parameter.Id);
                Assert.IsTrue(renderer.HasProperty(property), $"Render Shader is missing Parameter property: {property}");
                var index = renderer.shader.FindPropertyIndex(property);
                Assert.AreEqual(parameter.Type == FluxParticleValueType.Float ? ShaderPropertyType.Float : ShaderPropertyType.Vector,
                    renderer.shader.GetPropertyType(index), property);
            }
            foreach (var page in compiled.Shaders.Compilation.StateLayout.Pages)
            {
                var property = FluxParticleHlslNames.PageProperty(page.Id, FluxParticlePageVersion.Updated);
                Assert.IsTrue(renderer.HasProperty(property), $"Render Shader is missing Page property: {property}");
            }
        }

        [Test]
        public void ReconfigureReplacesGeneratedHierarchyAndReusesShaderAssets()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            var previous = runtime.transform.Find("__FluxFXGenerated").gameObject;
            var previousSteps = (FluxKernel[])runtime.GetProgramVariable("stepKernels");
            var stepShaders = previousSteps.Select(kernel => kernel.Shader).ToArray();
            var render = (Material)runtime.GetProgramVariable("renderMaterial");
            var renderIdentity = GetAssetIdentity(render);
            var assets = AssetDatabase.FindAssets("", new[] { folder });

            FluxParticleProgramBuild.Configure(runtime, compiled, authoring, folder, 9);

            Assert.IsTrue(previous == null);
            Assert.AreEqual(1, runtime.transform.Cast<Transform>().Count(child => child.name == "__FluxFXGenerated"));
            var steps = (FluxKernel[])runtime.GetProgramVariable("stepKernels");
            CollectionAssert.AreEqual(stepShaders, steps.Select(kernel => kernel.Shader));
            Assert.IsTrue(previousSteps.All(kernel => kernel == null));
            var rebuiltRender = (Material)runtime.GetProgramVariable("renderMaterial");
            Assert.AreEqual(renderIdentity, GetAssetIdentity(rebuiltRender));
            CollectionAssert.AreEquivalent(assets, AssetDatabase.FindAssets("", new[] { folder }));
            foreach (var buffer in ((FluxBuffer[])runtime.GetProgramVariable("pageBuffersA"))
                .Concat((FluxBuffer[])runtime.GetProgramVariable("pageBuffersB"))
                .Concat(new[] { (FluxBuffer)runtime.GetProgramVariable("spawnMaskBuffer"),
                    (FluxBuffer)runtime.GetProgramVariable("spawnKeyBuffer") }))
            {
                Assert.AreEqual(9, (int)buffer.GetProgramVariable("initialCount"));
            }
        }

        [Test]
        public void ReconfigureRestoresCachedRenderMaterialSettings()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            var render = (Material)runtime.GetProgramVariable("renderMaterial");
            var renderIdentity = GetAssetIdentity(render);
            var shaderIdentity = GetAssetIdentity(render.shader);
            var texture = render.mainTexture;
            var scale = render.mainTextureScale;
            var offset = render.mainTextureOffset;
            var sourceBlend = render.GetFloat("_SrcBlend");
            var destinationBlend = render.GetFloat("_DstBlend");
            var zWrite = render.GetFloat("_ZWrite");
            var queue = render.renderQueue;
            render.mainTexture = Texture2D.whiteTexture;
            render.mainTextureScale = new Vector2(3, 4);
            render.mainTextureOffset = new Vector2(0.2f, 0.3f);
            render.SetFloat("_SrcBlend", 99);
            render.SetFloat("_DstBlend", 99);
            render.SetFloat("_ZWrite", 99);
            render.renderQueue = 1234;

            FluxParticleProgramBuild.Configure(runtime, compiled, authoring, folder);

            var rebuiltRender = (Material)runtime.GetProgramVariable("renderMaterial");
            Assert.AreEqual(renderIdentity, GetAssetIdentity(rebuiltRender));
            Assert.AreEqual(shaderIdentity, GetAssetIdentity(rebuiltRender.shader));
            Assert.AreEqual(texture, rebuiltRender.mainTexture);
            Assert.AreEqual(scale, rebuiltRender.mainTextureScale);
            Assert.AreEqual(offset, rebuiltRender.mainTextureOffset);
            Assert.AreEqual(sourceBlend, rebuiltRender.GetFloat("_SrcBlend"));
            Assert.AreEqual(destinationBlend, rebuiltRender.GetFloat("_DstBlend"));
            Assert.AreEqual(zWrite, rebuiltRender.GetFloat("_ZWrite"));
            Assert.AreEqual(queue, rebuiltRender.renderQueue);
        }

        [Test]
        public void MissingSpawnConfigurationPreservesExistingGeneratedHierarchy()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            var generated = runtime.transform.Find("__FluxFXGenerated");
            var steps = (FluxKernel[])runtime.GetProgramVariable("stepKernels");
            var spawn = runtime.transform.Find("Spawn");
            var originalName = spawn.name;
            spawn.name = "MissingSpawn";
            try
            {
                Assert.IsNull(runtime.transform.Find("Spawn"));
                StringAssert.Contains("Spawn GameObject is missing",
                    Assert.Throws<InvalidOperationException>(() =>
                        FluxParticleProgramBuild.Configure(runtime, compiled, authoring, folder)).Message);
                Assert.AreSame(generated, runtime.transform.Find("__FluxFXGenerated"));
                CollectionAssert.AreEqual(steps, (FluxKernel[])runtime.GetProgramVariable("stepKernels"));
                Assert.IsTrue(steps.All(kernel => kernel != null));
            }
            finally
            {
                spawn.name = originalName;
            }
        }

        [TestCase(0)]
        [TestCase(1048577)]
        public void InvalidBufferCapacityPreservesExistingGeneratedHierarchy(int capacity)
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            var generated = runtime.transform.Find("__FluxFXGenerated");
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                FluxParticleProgramBuild.Configure(runtime, compiled, authoring, folder, capacity));
            Assert.AreSame(generated, runtime.transform.Find("__FluxFXGenerated"));
        }
        [Test]
        public void SpawnThenGravityUpdatesVelocityPositionAndAge()
        {
            RequireGraphics();
            SetModules(new FluxParticleGravityModule());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0.5f, 0, 1, 7);
            Assert.AreEqual(0, ReadValue(runtime, compiled, "particle.age", 0).x, 0.0001f);
            Assert.AreEqual(7, ReadValue(runtime, compiled, "particle.spawn-key", 0).x, 0.0001f);
            var initialVelocity = ReadValue(runtime, compiled, "particle.velocity", 0);
            Assert.AreEqual(1, initialVelocity.magnitude, 0.0001f);
            runtime.Step(0.5f, 1, 0, 8);
            Assert.AreEqual(initialVelocity.y - 0.5f, ReadValue(runtime, compiled, "particle.velocity", 0).y, 0.0001f);
            Assert.AreEqual((initialVelocity.y - 0.5f) * 0.5f, ReadValue(runtime, compiled, "particle.position", 0).y, 0.0001f);
            Assert.AreEqual(initialVelocity.z * 0.5f, ReadValue(runtime, compiled, "particle.position", 0).z, 0.0001f);
            Assert.AreEqual(0.5f, ReadValue(runtime, compiled, "particle.age", 0).x, 0.0001f);
            Assert.AreEqual(-1, ReadValue(runtime, compiled, "particle.age", 1).x, 0.0001f);
        }

        [Test]
        public void SpawnOverflowKeepsLastOccurrenceAndWrapsTwentyBitKeys()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0.1f, 3, 6, 1048574);
            var expected = new[] { 3f, 0f, 1f, 2f };
            for (var slot = 0; slot < 4; slot++)
            {
                Assert.AreEqual(expected[slot], ReadValue(runtime, compiled, "particle.spawn-key", slot).x, 0.0001f);
            }
            runtime.Step(0.1f, 1, 1, 50);
            Assert.AreEqual(0, ReadValue(runtime, compiled, "particle.age", 1).x, 0.0001f);
            Assert.AreEqual(0.1f, ReadValue(runtime, compiled, "particle.age", 0).x, 0.0001f);
        }

        [Test]
        public void StaticLockSurvivesClearAndInstancesOwnTheirResources()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var first = CreateRuntime(compiled);
            var second = CreateRuntime(compiled);
            Assert.IsTrue(first.Initialize(4));
            Assert.IsTrue(second.Initialize(4));
            Assert.AreNotSame(first.GetCurrentPage(0), second.GetCurrentPage(0));
            Assert.AreNotSame(first.RenderMaterial, second.RenderMaterial);
            Assert.AreSame(first.GetProgramVariable("renderMaterial"), first.RenderMaterial);
            Assert.AreSame(second.GetProgramVariable("renderMaterial"), second.RenderMaterial);
            Assert.IsTrue(first.SetVector("color.start", new Vector4(1, 0, 0, 0.25f)));
            first.Step(0.1f, 0, 1, 1);
            first.Clear();
            Assert.IsFalse(first.SetVector("color.start", Vector4.one));
            Assert.IsFalse(first.SetFloat("gravity.enabled", 1));
            Assert.IsFalse(first.SetFloat("color.start", 1));
            Assert.IsFalse(first.SetFloat("unknown", 1));
            Assert.AreEqual(-1, ReadValue(first, compiled, "particle.age", 0).x, 0.0001f);
            Assert.AreEqual(-1, ReadValue(second, compiled, "particle.age", 0).x, 0.0001f);
            var material = first.RenderMaterial;
            first.Release();
            Assert.IsNotNull(material);
            Assert.AreSame(material, first.RenderMaterial);
            Assert.AreSame(material, (Material)first.GetProgramVariable("renderMaterial"));
            Assert.IsTrue(second.SetVector("color.end", new Vector4(0, 1, 0, 1)));
        }

        [Test]
        public void SkippedPassRetainsItsCurrentTexture()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            // Mark a Page conditional to exercise the Runtime commit rule independently of Authoring.
            var conditions = (bool[])runtime.GetProgramVariable("spawnOnly");
            conditions[0] = true;
            runtime.SetProgramVariable("spawnOnly", conditions);
            Assert.IsTrue(runtime.Initialize(4));
            var page = ((int[])runtime.GetProgramVariable("outputPages"))[0];
            var property = ((string[])runtime.GetProgramVariable("renderPageProperties"))[page];
            var before = runtime.GetCurrentPage(page);
            runtime.Step(0.1f, 0, 0, 0);
            Assert.AreSame(before, runtime.GetCurrentPage(page));
            Assert.AreSame(before, runtime.RenderMaterial.GetTexture(property));
            runtime.Step(0.1f, 0, 1, 0);
            Assert.AreNotSame(before, runtime.GetCurrentPage(page));
            Assert.AreSame(runtime.GetCurrentPage(page), runtime.RenderMaterial.GetTexture(property));
        }

        [Test]
        public void SpawnInputsAreGeneratedInFluxBuffersAndClearedBetweenSteps()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));

            var mask = (FluxBuffer)runtime.GetProgramVariable("spawnMaskBuffer");
            var keys = (FluxBuffer)runtime.GetProgramVariable("spawnKeyBuffer");
            Assert.IsInstanceOf<RenderTexture>(mask.Texture);
            Assert.IsInstanceOf<RenderTexture>(keys.Texture);
            runtime.Step(0.1f, 3, 6, 1048574);
            for (var slot = 0; slot < 4; slot++)
            {
                Assert.AreEqual(1, ReadTexture(mask.Texture, slot).r, 0.0001f);
            }
            CollectionAssert.AreEqual(new[] { 3f, 0f, 1f, 2f },
                new[] { ReadTexture(keys.Texture, 0).r, ReadTexture(keys.Texture, 1).r,
                    ReadTexture(keys.Texture, 2).r, ReadTexture(keys.Texture, 3).r });

            runtime.Step(0.1f, 0, 0, 0);
            for (var slot = 0; slot < 4; slot++)
            {
                Assert.AreEqual(0, ReadTexture(mask.Texture, slot).r, 0.0001f);
            }
        }

        [Test]
        public void SpawnGpuGenerationDoesNotRequireFluxUploadCapacity()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled, 1024);
            Assert.IsTrue(runtime.Initialize(1024));
            runtime.Step(0.1f, 500, 600, 1048500);

            var mask = (FluxBuffer)runtime.GetProgramVariable("spawnMaskBuffer");
            var keys = (FluxBuffer)runtime.GetProgramVariable("spawnKeyBuffer");
            Assert.AreEqual(1, ReadTexture(mask.Texture, 500).r, 0.0001f);
            Assert.AreEqual(1, ReadTexture(mask.Texture, 75).r, 0.0001f);
            Assert.AreEqual(0, ReadTexture(mask.Texture, 499).r, 0.0001f);
            Assert.AreEqual(1048500, ReadTexture(keys.Texture, 500).r, 0.0001f);
            Assert.AreEqual(23, ReadTexture(keys.Texture, 599).r, 0.0001f);
        }

        [Test]
        public void RenderMaterialTracksPageCommitsAndClear()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled, 5);
            Assert.IsTrue(runtime.Initialize(5));

            var material = runtime.RenderMaterial;
            var properties = (string[])runtime.GetProgramVariable("renderPageProperties");
            var initial = new RenderTexture[properties.Length];
            var stateSize = material.GetVector("_FluxStateSize");
            Assert.AreEqual(3, stateSize.x);
            Assert.AreEqual(3, stateSize.y);
            Assert.AreEqual(5, stateSize.z);

            for (var page = 0; page < properties.Length; page++)
            {
                initial[page] = runtime.GetCurrentPage(page);
                Assert.AreSame(initial[page], material.GetTexture(properties[page]));
            }

            runtime.Step(0.25f, 4, 2, 10);
            for (var page = 0; page < properties.Length; page++)
            {
                Assert.AreNotSame(initial[page], runtime.GetCurrentPage(page));
                Assert.AreSame(runtime.GetCurrentPage(page), material.GetTexture(properties[page]));
            }

            runtime.Clear();
            for (var page = 0; page < properties.Length; page++)
            {
                Assert.AreSame(runtime.GetCurrentPage(page), material.GetTexture(properties[page]));
            }
            Assert.AreEqual(-1, ReadValue(runtime, compiled, "particle.age", 4).x, 0.0001f);
        }

        [Test]
        public void SpawnInputsDoNotActivateUnusedTexturePixels()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled, 5);
            Assert.IsTrue(runtime.Initialize(5));

            runtime.Step(0.1f, 4, 3, 1048575);
            var mask = (FluxBuffer)runtime.GetProgramVariable("spawnMaskBuffer");
            var keys = (FluxBuffer)runtime.GetProgramVariable("spawnKeyBuffer");
            Assert.AreEqual(9, mask.Capacity);
            for (var slot = 0; slot < mask.Capacity; slot++)
            {
                var expected = slot == 0 || slot == 1 || slot == 4 ? 1f : 0f;
                Assert.AreEqual(expected, ReadTexture(mask.Texture, slot).r, 0.0001f, $"SpawnMask slot {slot}");
            }
            Assert.AreEqual(1048575, ReadTexture(keys.Texture, 4).r, 0.0001f);
            Assert.AreEqual(0, ReadTexture(keys.Texture, 0).r, 0.0001f);
            Assert.AreEqual(1, ReadTexture(keys.Texture, 1).r, 0.0001f);
            Assert.AreEqual(-1, ReadValue(runtime, compiled, "particle.age", 3).x, 0.0001f);
        }

        [Test]
        public void RenderParametersAndTexturesRemainInstanceLocal()
        {
            RequireGraphics();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var first = CreateRuntime(compiled);
            var second = CreateRuntime(compiled);
            Assert.IsTrue(first.Initialize(4));
            Assert.IsTrue(second.Initialize(4));

            var parameterIds = (string[])first.GetProgramVariable("parameterIds");
            var parameterProperties = (string[])first.GetProgramVariable("parameterProperties");
            var index = Array.IndexOf(parameterIds, "color.start");
            Assert.GreaterOrEqual(index, 0);
            var property = parameterProperties[index];
            var secondColor = second.RenderMaterial.GetVector(property);
            var changed = new Vector4(0.2f, 0.4f, 0.6f, 0.8f);
            Assert.IsTrue(first.SetVector("color.start", changed));
            Assert.AreEqual(changed, first.RenderMaterial.GetVector(property));
            Assert.AreEqual(secondColor, second.RenderMaterial.GetVector(property));

            first.Step(0.2f, 0, 1, 1);
            var firstPages = (string[])first.GetProgramVariable("renderPageProperties");
            var secondPages = (string[])second.GetProgramVariable("renderPageProperties");
            for (var page = 0; page < firstPages.Length; page++)
            {
                Assert.AreSame(first.GetCurrentPage(page), first.RenderMaterial.GetTexture(firstPages[page]));
                Assert.AreSame(second.GetCurrentPage(page), second.RenderMaterial.GetTexture(secondPages[page]));
                Assert.AreNotSame(first.RenderMaterial.GetTexture(firstPages[page]), second.RenderMaterial.GetTexture(secondPages[page]));
            }
        }

        [Test]
        public void CurveLookupAssetIsSharedAndBoundToRuntimeRenderMaterials()
        {
            RequireGraphics();
            if (!SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("RGBAFloat texture support is required.");
            }
            SetModules(new FluxParticleRotationOverLifetimeModule(), new FluxParticleSizeOverLifetimeModule());
            var curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
            var serialized = new SerializedObject(authoring);
            var module = serialized.FindProperty("modules").GetArrayElementAtIndex(0);
            module.FindPropertyRelative("mode").enumValueIndex = (int)FluxParticleRotationOverLifetimeMode.Curve;
            module.FindPropertyRelative("endRotation").vector3Value = new Vector3(0, 0, 180);
            module.FindPropertyRelative("curve").animationCurveValue = curve;
            var sizeModule = serialized.FindProperty("modules").GetArrayElementAtIndex(1);
            sizeModule.FindPropertyRelative("mode").intValue = (int)FluxParticleSizeOverLifetimeMode.Curve;
            sizeModule.FindPropertyRelative("curve").animationCurveValue = curve;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var first = CreateRuntime(compiled);
            var second = CreateRuntime(compiled);
            var firstRender = (Material)first.GetProgramVariable("renderMaterial");
            var secondRender = (Material)second.GetProgramVariable("renderMaterial");
            var property = FluxParticleHlslNames.ProviderProperty("rotation.curve");
            var texture = firstRender.GetTexture(property);
            Assert.IsNotNull(texture);
            Assert.AreEqual(128, texture.width);
            Assert.AreEqual(1, texture.height);
            Assert.AreEqual(FilterMode.Bilinear, texture.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode);
            Assert.AreSame(texture, secondRender.GetTexture(property));
            Assert.AreSame(texture, firstRender.GetTexture(FluxParticleHlslNames.ProviderProperty("size.curve")));
            Assert.IsTrue(AssetDatabase.Contains(texture));
            Assert.AreNotSame(firstRender, secondRender);
            Assert.IsTrue(first.Initialize(4));
            Assert.IsTrue(first.SetVector("lifetime.range", new Vector4(10, 10, 0, 0)));
            first.Step(0, 0, 1, 1);
            first.Step(2, 0, 0, 2);
            Assert.AreSame(texture, first.RenderMaterial.GetTexture(property));
            AssertCustomRender(first, compiled, new Color(0, 0, 180 * curve.Evaluate(0.2f), 1), true);
            AssertCustomRender(first, compiled, new Color(curve.Evaluate(0.2f), 0, 0, 1), size: true);
        }
        [TestCase(false)]
        [TestCase(true)]
        public void CustomStateModuleRunsThroughAuthoringAndRuntime(bool reverse)
        {
            RequireGraphics();
            var baseline = FluxParticleBuildPipeline.Build(authoring);
            var custom = new TestStateModule();
            if (reverse)
            {
                SetModules(new FluxParticleDragModule(), custom, new FluxParticleGravityModule());
            }
            else
            {
                SetModules(new FluxParticleGravityModule(), custom, new FluxParticleDragModule());
            }
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var program = compiled.Shaders.Compilation;
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count + 1, program.StateLayout.Pages.Count);
            Assert.AreEqual(baseline.Shaders.Passes.Count + 1, compiled.Shaders.Passes.Count);
            Assert.IsTrue(program.StateLayout.TryGetBinding(TestStateDefinition.State, out var binding));
            Assert.AreEqual(4, binding.ComponentCount);
            Assert.IsFalse(program.StateLayout.TryGetBinding(TestStateDefinition.Rate, out _));
            Assert.IsFalse(program.StateLayout.TryGetBinding(TestStateDefinition.Strength, out _));
            var pass = compiled.Shaders.Passes.Single(value => value.Plan.OutputPageId == binding.PageId);
            Assert.IsTrue(pass.Plan.SpawnOperations.Any(operation => operation.Id == "test.state-spawn"));
            Assert.IsTrue(pass.Plan.UpdateOperations.Any(operation => operation.Id == "test.state-update"));
            StringAssert.Contains("FluxFX_TestStateRate", pass.Source);
            StringAssert.Contains("FluxFX_TestStateRender", compiled.Shaders.RenderSource);
            Assert.AreEqual(new Vector4(2, 0, 0, 0), compiled.InitialValues[TestStateDefinition.Strength]);

            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0.25f, 0, 1, 7);
            AssertVector(new Vector4(1, 2, 3, 4), ReadValue(runtime, compiled, TestStateDefinition.State, 0));
            AssertVector(Vector4.zero, ReadValue(runtime, compiled, TestStateDefinition.State, 1));
            runtime.Step(0.25f, 1, 0, 8);
            AssertVector(new Vector4(2, 3, 4, 5), ReadValue(runtime, compiled, TestStateDefinition.State, 0));

            Assert.IsTrue(runtime.SetFloat(TestStateDefinition.Strength, 4));
            runtime.Step(0.25f, 1, 1, 9);
            AssertVector(new Vector4(4, 5, 6, 7), ReadValue(runtime, compiled, TestStateDefinition.State, 0));
            AssertVector(new Vector4(1, 2, 3, 4), ReadValue(runtime, compiled, TestStateDefinition.State, 1));
            Assert.AreEqual(0.5f, ReadValue(runtime, compiled, "particle.age", 0).x, 0.0001f);
            AssertCustomRender(runtime, compiled, new Color(0.4f, 0.5f, 0.6f, 0.7f));
            runtime.Clear();
            AssertVector(Vector4.zero, ReadValue(runtime, compiled, TestStateDefinition.State, 0));
        }

        [Test]
        public void DisabledCustomStateModuleAddsNoStateOrPass()
        {
            var baseline = FluxParticleBuildPipeline.Build(authoring);
            var module = new TestStateModule();
            typeof(FluxParticleModule).GetField("enabled", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(module, false);
            SetModules(module);
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, compiled.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.AreEqual(baseline.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            Assert.IsFalse(compiled.Shaders.Compilation.StateLayout.TryGetBinding(TestStateDefinition.State, out _));
            Assert.IsFalse(compiled.InitialValues.ContainsKey(TestStateDefinition.Strength));
        }

        void AssertCustomRender(FluxParticleExecutor runtime, FluxParticleBuildResult compiled, Color expected, bool rotation = false, bool size = false)
        {
            var source = string.Join(Environment.NewLine, new[]
            {
                @"Shader ""Hidden/FluxFX/Tests/CustomStateRender"" { Properties {",
                FluxParticleProgramShaderGenerator.GenerateParameterProperties(compiled.Shaders.Compilation),
                "} SubShader { Pass { ZTest Always Cull Off ZWrite Off Blend Off",
                "HLSLPROGRAM",
                "#pragma target 3.5",
                "#pragma vertex vert_img",
                "#pragma fragment frag",
                @"#include ""UnityCG.cginc""",
                compiled.Shaders.RenderSource,
                "float4 frag(v2f_img input) : SV_Target { bool alive; FluxRenderContext value = FluxFX_EvaluateRender(input.uv, alive);",
                size ? "return alive ? float4(value.valueRenderSize, 0, 0, 1) : float4(0, 0, 0, 0); }" :
                rotation
                    ? "return alive ? float4(value.valueRenderRotation, 1) : float4(0, 0, 0, 0); }"
                    : "return alive ? value.valueRenderColor : float4(0, 0, 0, 0); }",
                "ENDHLSL",
                "} } Fallback Off }"
            });
            var path = folder + "/CustomStateRender.shader";
            source = source.Replace("\r\n", "\n").Replace("\r", "\n");
            System.IO.File.WriteAllText(path, source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.IsNotNull(shader);
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader),
                string.Join(Environment.NewLine, ShaderUtil.GetShaderMessages(shader).Select(message => message.message)));
            Assert.IsTrue(shader.isSupported);
            var material = new Material(shader);
            var page = runtime.GetCurrentPage(0);
            var target = new RenderTexture(page.width, page.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var active = RenderTexture.active;
            try
            {
                foreach (var statePage in compiled.Shaders.Compilation.StateLayout.Pages)
                {
                    material.SetTexture(FluxParticleHlslNames.PageProperty(statePage.Id, FluxParticlePageVersion.Updated),
                        runtime.GetCurrentPage(statePage.Id));
                }
                foreach (var provider in compiled.Shaders.Compilation.Providers.Values.Where(value => value.Kind == FluxParticleProviderKind.LookupTable))
                {
                    var property = FluxParticleHlslNames.ProviderProperty(provider.ValueId);
                    material.SetTexture(property, runtime.RenderMaterial.GetTexture(property));
                }
                var parameterProperties = (string[])runtime.GetProgramVariable("parameterProperties");
                var parameterTypes = (int[])runtime.GetProgramVariable("parameterTypes");
                for (var i = 0; i < parameterProperties.Length; i++)
                {
                    var property = parameterProperties[i];
                    if (parameterTypes[i] == 1)
                    {
                        material.SetFloat(property, runtime.RenderMaterial.GetFloat(property));
                    }
                    else
                    {
                        material.SetVector(property, runtime.RenderMaterial.GetVector(property));
                    }
                }
                target.Create();
                Graphics.Blit(null, target, material);
                AssertVector(expected, ReadTexture(target, 0), rotation ? 0.05f : size ? 0.002f : 0.0001f);
                AssertVector(Vector4.zero, ReadTexture(target, 2));
            }
            finally
            {
                RenderTexture.active = active;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(material);
            }
        }
        static void AssertVector(Vector4 expected, Vector4 actual, float tolerance = 0.0001f)
        {
            for (var component = 0; component < 4; component++)
            {
                Assert.AreEqual(expected[component], actual[component], tolerance, $"Component {component}");
            }
        }

        [Serializable]
        public sealed class TestStateModule : FluxParticleModule
        {
            public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.VelocityUpdate;
            public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.None;
            public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.None;
        }

        public sealed class TestStateDefinition : IFluxParticleModuleDefinition
        {
            public const string State = "test.state";
            public const string Rate = "test.rate";
            public const string Strength = "test.strength";
            const string Id = "fluxfx.tests.state";
            const string Hlsl = "Assets/FluxFX/Tests/Editor/Hlsl/";

            public Type ModuleType => typeof(TestStateModule);
            public string ModuleId => Id;

            public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
            {
                context.AddValue(new FluxParticleValueDefinition(State, FluxParticleValueType.Float4,
                    FluxParticleWritePolicy.OwnerManaged, Id));
                context.AddValue(new FluxParticleValueDefinition(Rate, FluxParticleValueType.Float,
                    FluxParticleWritePolicy.ReadOnly, Id));
                context.AddParameter(new FluxParticleParameterDefinition(Strength, FluxParticleValueType.Float,
                    FluxParticleParameterPolicy.Live), new Vector4(2, 0, 0, 0));
                context.AddProvider(new FluxParticleValueProvider(Rate, FluxParticleProviderKind.Derived,
                    parameters: new[] { Strength }, hlslPath: Hlsl + "TestStateRate.hlsl", entryPoint: "FluxFX_TestStateRate"));
                context.AddOperation(new FluxParticleOperationDefinition("test.state-spawn", Id, FluxParticlePhase.Spawn,
                    Array.Empty<FluxParticleValueRead>(),
                    new[] { new FluxParticleValueWrite(State, FluxParticleWriteKind.Initialize) }, null,
                    Hlsl + "TestStateSpawn.hlsl", "FluxFX_TestStateSpawn"));
                context.AddOperation(new FluxParticleOperationDefinition("test.state-update", Id, FluxParticlePhase.Update,
                    new[]
                    {
                        new FluxParticleValueRead(State, FluxParticleReadVersion.StepPrevious),
                        new FluxParticleValueRead(Rate, FluxParticleReadVersion.Provided),
                        new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                    },
                    new[] { new FluxParticleValueWrite(State, FluxParticleWriteKind.Modify) }, null,
                    Hlsl + "TestStateUpdate.hlsl", "FluxFX_TestStateUpdate"));
                context.AddOperation(new FluxParticleOperationDefinition("test.state-render", Id, FluxParticlePhase.Render,
                    new[] { new FluxParticleValueRead(State, FluxParticleReadVersion.Committed) },
                    new[] { new FluxParticleValueWrite("render.color", FluxParticleWriteKind.Replace) },
                    new[] { "render.start" }, Hlsl + "TestStateRender.hlsl", "FluxFX_TestStateRender"));
            }
        }
        [Test]
        public void VelocityOverLifetimeUsesMotionPageWithoutAdditionalStateOrPass()
        {
            var baseline = FluxParticleBuildPipeline.Build(authoring);
            SetModules(CreateVelocityModule(new Vector3(2, 0, 0), new Vector3(6, 0, 0)));
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            Assert.AreEqual(baseline.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, compiled.Shaders.Compilation.StateLayout.Pages.Count);
            var pass = compiled.Shaders.Passes.Single(value => value.Plan.UpdateOperations.Any(operation => operation.Id == "motion"));
            CollectionAssert.AreEqual(new[] { "motion", "velocity-over-lifetime" }, pass.Plan.UpdateOperations.Select(operation => operation.Id));
            Assert.IsFalse(compiled.Shaders.Compilation.StateLayout.TryGetBinding("velocity-over-lifetime.center-offset", out _));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VelocityOverLifetimeCombinesWithGravityAndDrag(bool reverse)
        {
            RequireGraphics();
            var velocity = CreateVelocityModule(new Vector3(2, 0, 0), new Vector3(2, 0, 0));
            if (reverse) SetModules(new FluxParticleDragModule(), velocity, new FluxParticleGravityModule());
            else SetModules(new FluxParticleGravityModule(), velocity, new FluxParticleDragModule());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            Assert.IsTrue(runtime.SetVector("gravity", new Vector4(0, -2, 0, 0)));
            Assert.IsTrue(runtime.SetFloat("drag", 0.4f));
            runtime.Step(0.5f, 0, 1, 1);
            var initialVelocity = ReadValue(runtime, compiled, "particle.velocity", 0);
            runtime.Step(0.5f, 0, 0, 2);
            var expectedVelocity = (initialVelocity + new Vector4(0, -1, 0, 0)) * Mathf.Exp(-0.2f);
            AssertVector(expectedVelocity, ReadValue(runtime, compiled, "particle.velocity", 0));
            AssertVector(expectedVelocity * 0.5f + new Vector4(1, 0, 0, 0),
                ReadValue(runtime, compiled, "particle.position", 0));
            Assert.AreEqual(0.5f, ReadValue(runtime, compiled, "particle.age", 0).x, 0.0001f);
        }

        [Test]
        public void VelocityOverLifetimeEvaluatesTimeOrbitalRadialAndLiveChanges()
        {
            RequireGraphics();
            var start = new Vector3(2, 0, 0);
            var end = new Vector3(6, 0, 0);
            var orbital = new Vector3(0, 0, 1);
            var offset = new Vector3(1, 0, 0);
            SetModules(CreateVelocityModule(start, end, orbital, offset, 3));
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            Assert.IsTrue(runtime.SetFloat("spawn.speed", 0));
            Assert.IsTrue(runtime.SetVector("spawn.position", new Vector4(2, 0, 0, 0)));
            Assert.IsTrue(runtime.SetVector("lifetime.range", new Vector4(10, 10, 0, 0)));
            runtime.Step(0, 0, 1, 1);
            AssertVector(new Vector4(2, 0, 0, 0), ReadValue(runtime, compiled, "particle.position", 0));
            runtime.Step(1, 0, 0, 2);
            AssertVector(new Vector4(7, 1, 0, 0), ReadValue(runtime, compiled, "particle.position", 0));
            var centerOffset = new Vector3(7, 1, 0) - offset;
            var extra = Vector3.Lerp(start, end, 0.1f) + Vector3.Cross(orbital, centerOffset) + centerOffset.normalized * 3;
            runtime.Step(1, 0, 0, 3);
            var expected = new Vector3(7, 1, 0) + extra;
            AssertVector(expected, ReadValue(runtime, compiled, "particle.position", 0));
            AssertVector(Vector4.zero, ReadValue(runtime, compiled, "particle.velocity", 0));
            Assert.IsTrue(runtime.SetFloat("velocity-over-lifetime.enabled", 0));
            runtime.Step(1, 0, 0, 4);
            AssertVector(expected, ReadValue(runtime, compiled, "particle.position", 0));
            Assert.AreEqual(3, ReadValue(runtime, compiled, "particle.age", 0).x, 0.0001f);

            Assert.IsTrue(runtime.SetVector("velocity-over-lifetime.start", new Vector4(10, 0, 0, 0)));
            Assert.IsTrue(runtime.SetVector("velocity-over-lifetime.end", new Vector4(10, 0, 0, 0)));
            Assert.IsTrue(runtime.SetVector("velocity-over-lifetime.orbital", Vector4.zero));
            Assert.IsTrue(runtime.SetFloat("velocity-over-lifetime.radial", 0));
            Assert.IsTrue(runtime.SetFloat("velocity-over-lifetime.enabled", 1));
            runtime.Step(1, 0, 0, 5);
            AssertVector(expected + new Vector3(10, 0, 0), ReadValue(runtime, compiled, "particle.position", 0));
            runtime.Clear();
            runtime.Step(0, 0, 1, 1);
            AssertVector(new Vector4(2, 0, 0, 0), ReadValue(runtime, compiled, "particle.position", 0));
        }

        [Test]
        public void VelocityOverLifetimeAtRadialCenterRemainsFinite()
        {
            RequireGraphics();
            SetModules(CreateVelocityModule(Vector3.zero, Vector3.zero, new Vector3(0, 0, 1), radial: 4));
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            Assert.IsTrue(runtime.SetFloat("spawn.speed", 0));
            runtime.Step(0, 0, 1, 1);
            runtime.Step(1, 0, 0, 2);
            AssertVector(Vector4.zero, ReadValue(runtime, compiled, "particle.position", 0));
        }

        [Test]
        public void DisabledVelocityOverLifetimeAddsNoOperationOrParameters()
        {
            var module = CreateVelocityModule(Vector3.one, Vector3.one);
            typeof(FluxParticleModule).GetField("enabled", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(module, false);
            SetModules(module);
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            Assert.IsFalse(compiled.Shaders.Compilation.Operations.Any(operation => operation.Id == "velocity-over-lifetime"));
            Assert.IsFalse(compiled.InitialValues.ContainsKey("velocity-over-lifetime.start"));
        }
        [Test]
        public void VelocityOverLifetimeRejectsNonFiniteParameters()
        {
            SetModules(CreateVelocityModule(new Vector3(float.NaN, 0, 0), Vector3.zero));
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
        }

        static FluxParticleVelocityOverLifetimeModule CreateVelocityModule(Vector3 start, Vector3 end,
            Vector3 orbital = default, Vector3 offset = default, float radial = 0)
        {
            var module = new FluxParticleVelocityOverLifetimeModule();
            var type = typeof(FluxParticleVelocityOverLifetimeModule);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("start", flags).SetValue(module, start);
            type.GetField("end", flags).SetValue(module, end);
            type.GetField("orbital", flags).SetValue(module, orbital);
            type.GetField("offset", flags).SetValue(module, offset);
            type.GetField("radial", flags).SetValue(module, radial);
            return module;
        }
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void ForceDependencyOrderIsIndependentOfModuleRegistration(int permutation)
        {
            var baseline = FluxParticleBuildPipeline.Build(authoring);
            var modules = new FluxParticleModule[] { new FluxParticleGravityModule(), new FluxParticleForceModule(), new FluxParticleDragModule() };
            var orders = new[] { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
                new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
            SetModules(orders[permutation].Select(index => modules[index]).ToArray());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, compiled.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.AreEqual(baseline.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            var writers = compiled.Shaders.Compilation.Operations.Where(operation => operation.Phase == FluxParticlePhase.Update &&
                operation.Writes.Any(write => write.ValueId == "particle.velocity")).ToArray();
            CollectionAssert.AreEqual(new[] { "gravity", "force", "drag" }, writers.Select(operation => operation.Id));
            Assert.AreEqual("force", writers[2].Reads.Single(read => read.ValueId == "particle.velocity").ProducerOperationId);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void ForceUpdatesGpuVelocityAndSupportsLiveChanges(bool gravity, bool drag)
        {
            RequireGraphics();
            var force = new FluxParticleForceModule();
            typeof(FluxParticleForceModule).GetField("force", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(force, new Vector3(2, 0, 0));
            var modules = new List<FluxParticleModule> { force };
            if (drag)
            {
                modules.Add(new FluxParticleDragModule());
            }
            if (gravity)
            {
                modules.Add(new FluxParticleGravityModule());
            }
            SetModules(modules.ToArray());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            Assert.IsTrue(runtime.SetFloat("spawn.speed", 0));
            if (gravity)
            {
                Assert.IsTrue(runtime.SetVector("gravity", new Vector4(0, -2, 0, 0)));
            }
            if (drag)
            {
                Assert.IsTrue(runtime.SetFloat("drag", 0.4f));
            }
            runtime.Step(0, 0, 1, 1);
            var gravityDelta = gravity ? new Vector4(0, -1, 0, 0) : Vector4.zero;
            var attenuation = drag ? Mathf.Exp(-0.2f) : 1;
            var expectedVelocity = (new Vector4(1, 0, 0, 0) + gravityDelta) * attenuation;
            runtime.Step(0.5f, 0, 0, 2);
            AssertVector(expectedVelocity, ReadValue(runtime, compiled, "particle.velocity", 0));
            var expectedPosition = expectedVelocity * 0.5f;
            AssertVector(expectedPosition, ReadValue(runtime, compiled, "particle.position", 0));

            Assert.IsTrue(runtime.SetVector("force", new Vector4(4, 0, 0, 0)));
            expectedVelocity = (expectedVelocity + new Vector4(2, 0, 0, 0) + gravityDelta) * attenuation;
            runtime.Step(0.5f, 0, 0, 3);
            expectedPosition += expectedVelocity * 0.5f;
            AssertVector(expectedVelocity, ReadValue(runtime, compiled, "particle.velocity", 0));
            AssertVector(expectedPosition, ReadValue(runtime, compiled, "particle.position", 0));

            Assert.IsTrue(runtime.SetFloat("force.enabled", 0));
            expectedVelocity = (expectedVelocity + gravityDelta) * attenuation;
            runtime.Step(0.5f, 0, 0, 4);
            AssertVector(expectedVelocity, ReadValue(runtime, compiled, "particle.velocity", 0));
            runtime.Clear();
            runtime.Step(0, 0, 1, 1);
            AssertVector(Vector4.zero, ReadValue(runtime, compiled, "particle.velocity", 0));
        }

        [Test]
        public void DisabledForceAddsNoOperationAndNonFiniteForceIsRejected()
        {
            var module = new FluxParticleForceModule();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(FluxParticleModule).GetField("enabled", flags).SetValue(module, false);
            SetModules(module);
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            Assert.IsFalse(compiled.Shaders.Compilation.Operations.Any(operation => operation.Id == "force"));
            Assert.IsFalse(compiled.InitialValues.ContainsKey("force"));
            typeof(FluxParticleModule).GetField("enabled", flags).SetValue(module, true);
            typeof(FluxParticleForceModule).GetField("force", flags).SetValue(module, new Vector3(float.NaN, 0, 0));
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
        }
        static string GetAssetIdentity(Object asset)
        {
            Assert.IsNotNull(asset);
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long localId));
            return guid + ":" + localId;
        }
        FluxParticleExecutor CreateRuntime(FluxParticleBuildResult compiled, int capacity = 4)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
            Assert.IsNotNull(prefab);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(root.transform, false);
            var runtime = go.GetComponentInChildren<FluxParticleExecutor>(true);
            Assert.IsNotNull(runtime, "Run Tools/FluxFX/Setup Program Backend Prefabs before executing these tests.");
            runtimes.Add(runtime);
            FluxParticleProgramBuild.Configure(runtime, compiled, authoring, folder, capacity);
            foreach (var kernel in runtime.GetComponentsInChildren<FluxKernel>(true))
            {
                var material = new Material(kernel.Shader);
                kernel.SetProgramVariable("material", material);
                kernelMaterials.Add(material);
            }
            return runtime;
        }

        void SetModules(params FluxParticleModule[] modules)
        {
            typeof(FluxParticleAuthoring).GetField("modules", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(authoring, modules.ToList());
        }

        static Color ReadTexture(RenderTexture texture, int slot)
        {
            var previous = RenderTexture.active;
            var readback = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, false, true);
            try
            {
                RenderTexture.active = texture;
                readback.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                readback.Apply();
                return readback.GetPixels()[slot];
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(readback);
            }
        }

        static Vector4 ReadValue(FluxParticleExecutor runtime, FluxParticleBuildResult compiled, string id, int slot)
        {
            Assert.IsTrue(compiled.Shaders.Compilation.StateLayout.TryGetBinding(id, out var binding));
            var texture = runtime.GetCurrentPage(binding.PageId);
            var previous = RenderTexture.active;
            var readback = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, false, true);
            try
            {
                RenderTexture.active = texture;
                readback.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                readback.Apply();
                var color = readback.GetPixels()[slot];
                var value = Vector4.zero;
                for (var i = 0; i < binding.ComponentCount; i++) value[i] = color[binding.ChannelOffset + i];
                return value;
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(readback);
            }
        }

        static void RequireGraphics()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
            {
                Assert.Ignore("A graphics device with ARGBFloat support is required.");
            }
        }
    }
}
