using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleSizeOverLifetimeTests
    {
        GameObject root;
        FluxParticleAuthoring authoring;
        readonly List<Object> objects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Size over Lifetime Test");
            authoring = root.AddComponent<FluxParticleAuthoring>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var value in objects)
            {
                if (value != null) Object.DestroyImmediate(value);
            }
            objects.Clear();
            Object.DestroyImmediate(root);
        }

        [TestCase(FluxParticleSizeOverLifetimeMode.Linear)]
        [TestCase(FluxParticleSizeOverLifetimeMode.Curve)]
        public void SizeUsesRenderOperationsWithoutAdditionalStateOrPass(FluxParticleSizeOverLifetimeMode mode)
        {
            var baseline = FluxParticleBuildPipeline.Build(authoring);
            Configure(mode, false);
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            Assert.AreEqual(baseline.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, compiled.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.IsFalse(compiled.Shaders.Compilation.StateLayout.TryGetBinding("render.size", out _));
            var operation = compiled.Shaders.Compilation.Operations.Single(value => value.Id == "render.size-over-lifetime");
            Assert.AreEqual(FluxParticlePhase.Render, operation.Phase);
            CollectionAssert.Contains(operation.Dependencies, "render.size-start");
            StringAssert.Contains("render.valueRenderSize", compiled.RendererSource);
            Assert.AreEqual(mode == FluxParticleSizeOverLifetimeMode.Linear, compiled.InitialValues.ContainsKey("size.end"));
            if (mode == FluxParticleSizeOverLifetimeMode.Curve)
            {
                Assert.AreEqual(128, compiled.Shaders.Compilation.Providers["size.curve"].LookupSamples.Count);
            }
        }

        [Test]
        public void DisabledSizeModuleAddsNoLifetimeEvaluation()
        {
            Configure(FluxParticleSizeOverLifetimeMode.Curve, false);
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("modules").GetArrayElementAtIndex(0).FindPropertyRelative("enabled").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            Assert.IsFalse(compiled.Shaders.Compilation.Operations.Any(operation => operation.Id == "render.size-over-lifetime"));
            Assert.IsFalse(compiled.Shaders.Compilation.Providers.ContainsKey("size.curve"));
            Assert.IsTrue(compiled.Shaders.Compilation.Operations.Any(operation => operation.Id == "render.size-start"));
        }

        [Test]
        public void RejectsUnknownModeAndEmptyCurve()
        {
            Configure((FluxParticleSizeOverLifetimeMode)99, false);
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
            Configure(FluxParticleSizeOverLifetimeMode.Curve, false);
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("modules").GetArrayElementAtIndex(0).FindPropertyRelative("curve").animationCurveValue = new AnimationCurve();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
        }
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void RejectsInvalidLinearEndSize(float end)
        {
            Configure(FluxParticleSizeOverLifetimeMode.Linear, false);
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("modules").GetArrayElementAtIndex(0).FindPropertyRelative("endSize").floatValue = end;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
        }

        [TestCase(FluxParticleSizeOverLifetimeMode.Linear, false)]
        [TestCase(FluxParticleSizeOverLifetimeMode.Linear, true)]
        [TestCase(FluxParticleSizeOverLifetimeMode.Curve, false)]
        [TestCase(FluxParticleSizeOverLifetimeMode.Curve, true)]
        public void GpuSizeTracksAgeAndPreservesRandomStartAcrossClear(FluxParticleSizeOverLifetimeMode mode, bool random)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            var curve = Configure(mode, random);
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var generated = compiled.Shaders;
            var steps = generated.Passes.Select((pass, index) => CreateShader("Step" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => CreateShader("Clear" + index, source)).ToArray();
            CreateShader("Billboard", compiled.RendererSource);
            var evaluation = CreateShader("Evaluate", string.Join(Environment.NewLine, new[]
            {
                @"Shader ""Hidden/FluxFX/Tests/SizeEvaluation"" { SubShader { Pass { ZTest Always Cull Off ZWrite Off Blend Off",
                "HLSLPROGRAM",
                "#pragma target 3.5",
                "#pragma vertex vert_img",
                "#pragma fragment frag",
                @"#include ""UnityCG.cginc""",
                generated.RenderSource,
                "float4 frag(v2f_img input) : SV_Target { bool alive; FluxRenderContext value = FluxFX_EvaluateRender(input.uv, alive);",
                "return alive ? float4(value.valueRenderSize, 0, 0, 1) : float4(0, 0, 0, 0); }",
                "ENDHLSL",
                "} } Fallback Off }"
            }));
            var material = Own(new Material(evaluation));
            using (var session = new FluxParticleProgramGpuSession(generated, steps, clears, 2, 1))
            {
                foreach (var parameter in generated.Compilation.Parameters.Values)
                {
                    var value = compiled.InitialValues[parameter.Id];
                    if (parameter.Type == FluxParticleValueType.Float) session.SetFloat(parameter.Id, value.x);
                    else session.SetVector(parameter.Id, value);
                }
                session.SetVector("lifetime.range", new Vector4(10, 10, 0, 0));
                session.SetFloat("spawn.speed", 0);
                session.SetUniformFloat("step.delta-time", 0);
                session.SetSpawnInput("spawn.key", Texture(new Color(123, 0, 0, 0), new Color(456, 0, 0, 0)));
                var mask = Texture(Color.white, Color.white);
                session.Step(mask);
                var start = Evaluate(session, material);
                if (random)
                {
                    Assert.That(start[0].r, Is.InRange(1f, 3f));
                    Assert.Greater(Mathf.Abs(start[0].r - start[1].r), 0.00001f);
                }
                else
                {
                    Assert.AreEqual(2, start[0].r, 0.0001f);
                }
                session.SetUniformFloat("step.delta-time", 5);
                session.Step();
                var middle = Evaluate(session, material);
                for (var i = 0; i < 2; i++)
                {
                    var expected = mode == FluxParticleSizeOverLifetimeMode.Linear
                        ? Mathf.Lerp(start[i].r, 4, 0.5f) : start[i].r * curve.Evaluate(0.5f);
                    Assert.AreEqual(expected, middle[i].r, 0.002f);
                }
                if (mode == FluxParticleSizeOverLifetimeMode.Linear)
                {
                    session.SetFloat("size.end", 6);
                    var changed = Evaluate(session, material);
                    Assert.AreEqual(Mathf.Lerp(start[0].r, 6, 0.5f), changed[0].r, 0.002f);
                }
                session.Clear();
                session.SetUniformFloat("step.delta-time", 0);
                session.Step(mask);
                var replay = Evaluate(session, material);
                for (var i = 0; i < 2; i++) Assert.AreEqual(start[i].r, replay[i].r, 0.0001f);
            }
        }

        AnimationCurve Configure(FluxParticleSizeOverLifetimeMode mode, bool random)
        {
            var curve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.5f, 0.25f), new Keyframe(1, 1));
            var serialized = new SerializedObject(authoring);
            var render = serialized.FindProperty("render");
            render.FindPropertyRelative("startSize").floatValue = 2;
            render.FindPropertyRelative("startSizeMode").intValue = (int)(random
                ? FluxParticleStartSizeMode.RandomBetweenTwoConstants : FluxParticleStartSizeMode.Constant);
            render.FindPropertyRelative("startSizeMin").floatValue = 1;
            render.FindPropertyRelative("startSizeMax").floatValue = 3;
            var modules = serialized.FindProperty("modules");
            modules.arraySize = 1;
            modules.GetArrayElementAtIndex(0).managedReferenceValue = new FluxParticleSizeOverLifetimeModule();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            var module = serialized.FindProperty("modules").GetArrayElementAtIndex(0);
            module.FindPropertyRelative("mode").intValue = (int)mode;
            module.FindPropertyRelative("endSize").floatValue = 4;
            module.FindPropertyRelative("curve").animationCurveValue = curve;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return curve;
        }

        Shader CreateShader(string name, string source)
        {
            var shader = ShaderUtil.CreateShaderAsset(source, true);
            objects.Add(shader);
            Assert.IsNotNull(shader, name);
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader),
                name + ": " + string.Join(Environment.NewLine, ShaderUtil.GetShaderMessages(shader).Select(message => message.message)));
            Assert.IsTrue(shader.isSupported, name);
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
