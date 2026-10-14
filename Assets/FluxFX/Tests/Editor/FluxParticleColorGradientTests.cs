using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleColorGradientTests
    {
        GameObject root;
        FluxParticleAuthoring authoring;
        string folder;
        readonly List<Object> objects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Gradient Test");
            authoring = root.AddComponent<FluxParticleAuthoring>();
            folder = "Assets/FluxFX/Tests/GradientTemporary_" + Guid.NewGuid().ToString("N");
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
        public void GradientAddsOneRgbaLookupWithoutStateOrSimulationPass()
        {
            var baseline = FluxParticleBuildPipeline.Build(authoring);
            Configure();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            Assert.AreEqual(baseline.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, compiled.Shaders.Compilation.StateLayout.Pages.Count);
            var provider = compiled.Shaders.Compilation.Providers["color.gradient"];
            Assert.AreEqual(128, provider.LookupSampleCount);
            Assert.AreEqual(128, provider.LookupVectorSamples.Count);
            Assert.AreEqual(1, compiled.Shaders.Compilation.Providers.Values.Count(value => value.Kind == FluxParticleProviderKind.LookupTable));
            Assert.IsFalse(compiled.Shaders.Compilation.StateLayout.TryGetBinding("color.gradient", out _));
            var texture = Own(FluxParticleLookupTexture.Create(provider));
            Assert.AreEqual(FilterMode.Bilinear, texture.filterMode);
            Assert.AreEqual(0.2f, texture.GetPixel(0, 0).a, 0.0001f);
            Assert.AreEqual(0.8f, texture.GetPixel(127, 0).a, 0.0001f);
        }

        [Test]
        public void LookupCopiesSamplesAndRejectsMixedOrNonFiniteSamples()
        {
            var samples = new[] { Vector4.one, Vector4.zero };
            var provider = new FluxParticleValueProvider("test", FluxParticleProviderKind.LookupTable,
                new[] { "input" }, lookupVectorSamples: samples);
            samples[0] = Vector4.zero;
            Assert.AreEqual(Vector4.one, provider.LookupVectorSamples[0]);
            Assert.Throws<ArgumentException>(() => new FluxParticleValueProvider("test", FluxParticleProviderKind.LookupTable,
                new[] { "input" }, lookupSamples: new[] { 0f, 1f }, lookupVectorSamples: samples));
            Assert.Throws<ArgumentException>(() => new FluxParticleValueProvider("test", FluxParticleProviderKind.LookupTable,
                new[] { "input" }, lookupVectorSamples: new[] { Vector4.zero, new Vector4(0, 0, float.NaN, 0) }));
        }

        [Test]
        public void FixedGradientIsRejected()
        {
            var gradient = Configure();
            gradient.mode = GradientMode.Fixed;
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("modules").GetArrayElementAtIndex(0).FindPropertyRelative("gradient").gradientValue = gradient;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
        }

        [Test]
        public void GpuGradientMultipliesStartColorAndRestartsAfterClear()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            var gradient = Configure();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var generated = compiled.Shaders;
            var steps = generated.Passes.Select((pass, index) => Import("Step" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => Import("Clear" + index, source)).ToArray();
            Import("Billboard", compiled.RendererSource);
            var shader = Import("Evaluate", string.Join(Environment.NewLine, new[]
            {
                @"Shader ""Hidden/FluxFX/Tests/GradientEvaluation"" { Properties {",
                FluxParticleProgramShaderGenerator.GenerateParameterProperties(generated.Compilation),
                "} SubShader { Pass { ZTest Always Cull Off ZWrite Off Blend Off",
                "HLSLPROGRAM",
                "#pragma target 3.5",
                "#pragma vertex vert_img",
                "#pragma fragment frag",
                @"#include ""UnityCG.cginc""",
                generated.RenderSource,
                "float4 frag(v2f_img input) : SV_Target { bool alive; FluxRenderContext value = FluxFX_EvaluateRender(input.uv, alive);",
                "return value.valueRenderColor; }",
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
                session.SetVector("color.start", new Vector4(0.5f, 0.75f, 0.25f, 0.5f));
                session.SetFloat("spawn.speed", 0);
                session.SetSpawnInput("spawn.key", Texture(Color.clear, Color.clear));
                session.SetUniformFloat("step.delta-time", 0);
                var mask = Texture(Color.white, Color.white);
                session.Step(mask);
                AssertColor(Evaluate(session, material)[0], gradient.Evaluate(0));
                session.SetUniformFloat("step.delta-time", 4);
                session.Step();
                AssertColor(Evaluate(session, material)[0], gradient.Evaluate(0.5f));
                session.SetUniformFloat("step.delta-time", 2);
                session.Step();
                AssertColor(Evaluate(session, material)[0], gradient.Evaluate(0.75f));
                session.Clear();
                session.SetUniformFloat("step.delta-time", 0);
                session.Step(mask);
                AssertColor(Evaluate(session, material)[0], gradient.Evaluate(0));
            }
        }

        static void AssertColor(Color actual, Color gradient)
        {
            var expected = gradient * new Color(0.5f, 0.75f, 0.25f, 0.5f);
            for (var i = 0; i < 4; i++)
            {
                Assert.AreEqual(expected[i], actual[i], 0.002f, "Channel " + i);
            }
        }

        Gradient Configure()
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                new GradientColorKey(new Color(1, 0.2f, 0.1f), 0),
                new GradientColorKey(new Color(0.2f, 0.4f, 1), 1)
            }, new[] { new GradientAlphaKey(0.2f, 0), new GradientAlphaKey(0.8f, 1) });
            var serialized = new SerializedObject(authoring);
            var modules = serialized.FindProperty("modules");
            modules.arraySize = 1;
            modules.GetArrayElementAtIndex(0).managedReferenceValue = new FluxParticleColorOverLifetimeModule();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            var module = serialized.FindProperty("modules").GetArrayElementAtIndex(0);
            module.FindPropertyRelative("mode").intValue = (int)FluxParticleColorOverLifetimeMode.Gradient;
            module.FindPropertyRelative("gradient").gradientValue = gradient;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return gradient;
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
