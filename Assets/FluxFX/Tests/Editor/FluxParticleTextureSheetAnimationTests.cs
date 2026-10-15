using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleTextureSheetAnimationTests
    {
        GameObject root;
        FluxParticleAuthoring authoring;
        string folder;
        readonly List<Object> objects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Texture Sheet Test");
            authoring = root.AddComponent<FluxParticleAuthoring>();
            folder = "Assets/FluxFX/Tests/SheetTemporary_" + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var value in objects)
            {
                if (value != null) Object.DestroyImmediate(value);
            }
            objects.Clear();
            if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void SheetUsesRenderOnlyAndDisabledModuleKeepsIdentityUv()
        {
            var baseline = FluxParticleBuildPipeline.Build(authoring);
            Configure(4, 2, 1);
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            Assert.AreEqual(baseline.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, compiled.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.IsFalse(compiled.Shaders.Compilation.StateLayout.TryGetBinding("render.uv-transform", out _));
            Assert.AreEqual(FluxParticlePhase.Render, compiled.Shaders.Compilation.Operations.Single(
                operation => operation.Id == "render.texture-sheet-animation").Phase);
            StringAssert.Contains("render.valueRenderUvTransform.xy", compiled.RendererSource);
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("modules").GetArrayElementAtIndex(0).FindPropertyRelative("enabled").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            compiled = FluxParticleBuildPipeline.Build(authoring);
            Assert.IsFalse(compiled.InitialValues.ContainsKey("texture-sheet.tiles"));
            Assert.IsFalse(compiled.Shaders.Compilation.Operations.Any(operation => operation.Id == "render.texture-sheet-animation"));
        }

        [TestCase(0, 2, 1f)]
        [TestCase(2, -1, 1f)]
        [TestCase(4097, 4096, 1f)]
        [TestCase(2, 2, -1f)]
        [TestCase(2, 2, float.NaN)]
        [TestCase(2, 2, float.PositiveInfinity)]
        public void RejectsInvalidSettings(int x, int y, float cycles)
        {
            Configure(x, y, cycles);
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
        }

        [Test]
        public void GpuUvUsesTopLeftFramesWrapsCyclesAndRestartsAfterClear()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            Configure(4, 2, 1);
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var generated = compiled.Shaders;
            var steps = generated.Passes.Select((pass, index) => Import("Step" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => Import("Clear" + index, source)).ToArray();
            Import("Billboard", compiled.RendererSource);
            var shader = Import("Evaluate", string.Join(Environment.NewLine, new[]
            {
                @"Shader ""Hidden/FluxFX/Tests/SheetEvaluation"" { Properties {",
                FluxParticleProgramShaderGenerator.GenerateParameterProperties(generated.Compilation),
                "} SubShader { Pass { ZTest Always Cull Off ZWrite Off Blend Off",
                "HLSLPROGRAM",
                "#pragma target 3.5",
                "#pragma vertex vert_img",
                "#pragma fragment frag",
                @"#include ""UnityCG.cginc""",
                generated.RenderSource,
                "float4 frag(v2f_img input) : SV_Target { bool alive; FluxRenderContext value = FluxFX_EvaluateRender(input.uv, alive);",
                "return value.valueRenderUvTransform; }",
                "ENDHLSL",
                "} } Fallback Off }"
            }));
            var material = Own(new Material(shader));
            using (var session = new FluxParticleProgramGpuSession(generated, steps, clears, 2, 1))
            {
                foreach (var parameter in generated.Compilation.Parameters.Values)
                {
                    var value = compiled.InitialValues[parameter.Id];
                    if (parameter.Type == FluxParticleValueType.Float) session.SetFloat(parameter.Id, value.x);
                    else session.SetVector(parameter.Id, value);
                }
                session.SetVector("lifetime.range", new Vector4(8, 8, 0, 0));
                session.SetFloat("spawn.speed", 0);
                session.SetSpawnInput("spawn.key", Texture(Color.clear, Color.clear));
                session.SetUniformFloat("step.delta-time", 0);
                var mask = Texture(Color.white, Color.white);
                session.Step(mask);
                AssertUv(Evaluate(session, material)[0], 0, 0.5f);
                session.SetUniformFloat("step.delta-time", 1);
                session.Step();
                AssertUv(Evaluate(session, material)[0], 0.25f, 0.5f);
                session.SetUniformFloat("step.delta-time", 3);
                session.Step();
                AssertUv(Evaluate(session, material)[0], 0, 0);
                session.SetFloat("texture-sheet.cycles", 2);
                AssertUv(Evaluate(session, material)[0], 0, 0.5f);
                session.SetFloat("texture-sheet.cycles", 0);
                AssertUv(Evaluate(session, material)[0], 0, 0.5f);
                session.Clear();
                session.SetFloat("texture-sheet.cycles", 1);
                session.SetUniformFloat("step.delta-time", 0);
                session.Step(mask);
                AssertUv(Evaluate(session, material)[0], 0, 0.5f);
            }
        }

        static void AssertUv(Color actual, float x, float y)
        {
            Assert.AreEqual(0.25f, actual.r, 0.0001f);
            Assert.AreEqual(0.5f, actual.g, 0.0001f);
            Assert.AreEqual(x, actual.b, 0.0001f);
            Assert.AreEqual(y, actual.a, 0.0001f);
        }

        void Configure(int x, int y, float cycles)
        {
            var serialized = new SerializedObject(authoring);
            var modules = serialized.FindProperty("modules");
            modules.arraySize = 1;
            modules.GetArrayElementAtIndex(0).managedReferenceValue = new FluxParticleTextureSheetAnimationModule();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            var module = serialized.FindProperty("modules").GetArrayElementAtIndex(0);
            module.FindPropertyRelative("tilesX").intValue = x;
            module.FindPropertyRelative("tilesY").intValue = y;
            module.FindPropertyRelative("cycles").floatValue = cycles;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        Shader Import(string name, string source)
        {
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/FluxFX/Tests", Path.GetFileName(folder));
            var path = folder + "/" + name + ".shader";
            File.WriteAllText(path, source.Replace("\r\n", "\n").Replace("\r", "\n"));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.IsNotNull(shader);
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader),
                string.Join(Environment.NewLine, ShaderUtil.GetShaderMessages(shader).Select(message => message.message)));
            Assert.IsTrue(shader.isSupported);
            return shader;
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

        T Own<T>(T value) where T : Object
        {
            objects.Add(value);
            return value;
        }

        static Color[] Evaluate(FluxParticleProgramGpuSession session, Material material)
        {
            session.BindRender(material);
            var target = new RenderTexture(2, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(2, 1, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                Graphics.Blit(null, target, material);
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 2, 1), 0, 0);
                readback.Apply();
                return readback.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback);
            }
        }
    }
}
