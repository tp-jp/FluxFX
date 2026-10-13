using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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

        [TearDown]
        public void TearDown()
        {
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
            var program = new FluxParticleAuthoringProgram(authoring);
            Assert.AreEqual(2, program.Program.Program.StateLayout.Pages.Count);
            Assert.AreEqual(2, program.Program.Passes.Count);
            Assert.AreEqual(1, program.InitialValues["spawn.speed"].x);
            Assert.AreEqual(0, program.InitialValues["gravity.enabled"].x);
            Assert.IsFalse(program.Program.Program.Operations.Any(op => op.Id == "render.color"));
        }

        [TestCase("simulationSpace", 1)]
        [TestCase("shape.type", 1)]
        [TestCase("render.mode", 1)]
        [TestCase("render.startColorMode", 1)]
        [TestCase("initialVelocity.mode", 1)]
        public void UnsupportedSettingsFailBeforeGeneratingAssets(string path, int value)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty(path).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            StringAssert.Contains("Program Backend", Assert.Throws<InvalidOperationException>(() => new FluxParticleAuthoringProgram(authoring)).Message);
            Assert.IsFalse(AssetDatabase.IsValidFolder(folder));
        }

        [Test]
        public void UnsupportedAndDuplicateModulesAreRejected()
        {
            SetModules(new FluxParticleNoiseModule());
            Assert.Throws<InvalidOperationException>(() => new FluxParticleAuthoringProgram(authoring));
            SetModules(new FluxParticleGravityModule(), new FluxParticleGravityModule());
            Assert.Throws<InvalidOperationException>(() => new FluxParticleAuthoringProgram(authoring));
        }

        [Test]
        public void RuntimeManifestPreservesResolvedBindingsAndImportsRenderer()
        {
            RequireGraphics();
            var compiled = new FluxParticleAuthoringProgram(authoring);
            var runtime = CreateRuntime(compiled);
            CollectionAssert.AreEqual(compiled.Program.Passes.Select(p => p.Plan.OutputPageId), (int[])runtime.GetProgramVariable("outputPages"));
            CollectionAssert.AreEqual(compiled.Program.Passes.SelectMany(p => p.Reads).Select(r => FluxParticleHlslNames.PageProperty(r.PageId, r.Version)),
                (string[])runtime.GetProgramVariable("readProperties"));
            var kernels = (FluxKernel[])runtime.GetProgramVariable("stepKernels");
            Assert.AreEqual(compiled.Program.Passes.Count, kernels.Length);
            foreach (var kernel in kernels)
            {
                Assert.IsFalse(ShaderUtil.ShaderHasError(kernel.Shader));
                Assert.IsTrue(kernel.Shader.isSupported);
            }
            var renderer = (Material)runtime.GetProgramVariable("renderMaterial");
            Assert.IsFalse(ShaderUtil.ShaderHasError(renderer.shader));
            Assert.IsTrue(renderer.shader.isSupported);
        }

        [Test]
        public void SpawnThenGravityUpdatesVelocityPositionAndAge()
        {
            RequireGraphics();
            SetModules(new FluxParticleGravityModule());
            var compiled = new FluxParticleAuthoringProgram(authoring);
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
            var compiled = new FluxParticleAuthoringProgram(authoring);
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
            var compiled = new FluxParticleAuthoringProgram(authoring);
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
            Assert.IsTrue(first.SetFloat("gravity.enabled", 1));
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
            var compiled = new FluxParticleAuthoringProgram(authoring);
            var runtime = CreateRuntime(compiled);
            // Mark a Page conditional to exercise the Runtime commit rule independently of Authoring.
            var conditions = (bool[])runtime.GetProgramVariable("spawnOnly");
            conditions[0] = true;
            runtime.SetProgramVariable("spawnOnly", conditions);
            Assert.IsTrue(runtime.Initialize(4));
            var page = ((int[])runtime.GetProgramVariable("outputPages"))[0];
            var before = runtime.GetCurrentPage(page);
            runtime.Step(0.1f, 0, 0, 0);
            Assert.AreSame(before, runtime.GetCurrentPage(page));
            runtime.Step(0.1f, 0, 1, 0);
            Assert.AreNotSame(before, runtime.GetCurrentPage(page));
        }

        [Test]
        public void SpawnInputsAreGeneratedInFluxBuffersAndClearedBetweenSteps()
        {
            RequireGraphics();
            var compiled = new FluxParticleAuthoringProgram(authoring);
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
            var compiled = new FluxParticleAuthoringProgram(authoring);
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

        FluxParticleExecutor CreateRuntime(FluxParticleAuthoringProgram compiled, int capacity = 4)
        {
            var go = new GameObject("Runtime");
            go.transform.SetParent(root.transform, false);
            var runtime = go.AddUdonSharpComponent<FluxParticleExecutor>();
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

        static Vector4 ReadValue(FluxParticleExecutor runtime, FluxParticleAuthoringProgram compiled, string id, int slot)
        {
            Assert.IsTrue(compiled.Program.Program.StateLayout.TryGetBinding(id, out var binding));
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
