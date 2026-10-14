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
    public sealed class FluxParticleRotationOverLifetimeTests
    {
        GameObject _root;
        FluxParticleAuthoring _authoring;
        string _temporaryFolder;
        readonly List<Object> _objects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Rotation over Lifetime Test");
            _authoring = _root.AddComponent<FluxParticleAuthoring>();
            _temporaryFolder = "Assets/FluxFX/Tests/RotationTemporary_" + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var value in _objects)
            {
                if (value != null) Object.DestroyImmediate(value);
            }
            _objects.Clear();
            if (AssetDatabase.IsValidFolder(_temporaryFolder)) AssetDatabase.DeleteAsset(_temporaryFolder);
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [TestCase(0f)]
        [TestCase(45f)]
        public void LinearRotationUsesRenderOperationsWithoutNewState(float startAngle)
        {
            var baseline = FluxParticleBuildPipeline.Build(_authoring);
            SetStartRotation(startAngle);
            AddRotationModule(new Vector3(0, 0, 180), FluxParticleRotationOverLifetimeMode.Linear);

            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            var program = compiled.Shaders.Compilation;
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, program.StateLayout.Pages.Count);
            Assert.AreEqual(baseline.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            Assert.IsFalse(program.StateLayout.TryGetBinding("render.rotation", out _));
            Assert.AreEqual(new Vector4(0, 0, startAngle, 0), compiled.InitialValues["rotation.start"]);
            Assert.AreEqual(new Vector4(0, 0, 180, 0), compiled.InitialValues["rotation.end"]);
            var start = program.Operations.Single(op => op.Id == "render.rotation-start");
            var update = program.Operations.Single(op => op.Id == "render.rotation-over-lifetime");
            Assert.AreEqual(FluxParticlePhase.Render, start.Phase);
            Assert.AreEqual(FluxParticlePhase.Render, update.Phase);
            CollectionAssert.Contains(update.Dependencies.ToArray(), start.Id);
            Assert.IsTrue(update.Reads.Any(read => read.ValueId == "render.rotation" &&
                read.Version == FluxParticleReadVersion.AfterOperation && read.ProducerOperationId == start.Id));
            Assert.IsTrue(update.Reads.Any(read => read.ValueId == "particle.age" &&
                read.Version == FluxParticleReadVersion.Committed));
            Assert.IsTrue(update.Reads.Any(read => read.ValueId == "particle.lifetime" &&
                read.Version == FluxParticleReadVersion.Provided));
            CollectionAssert.Contains(update.Parameters.ToArray(), "rotation.end");
            StringAssert.EndsWith("/AuthoringRotationOverLifetimeLinear.hlsl", update.HlslPath);
            StringAssert.Contains("FluxFX_RenderRotationOverLifetime", compiled.Shaders.RenderSource);
            StringAssert.Contains("render.valueRenderRotation.z", compiled.RendererSource);
        }

        [Test]
        public void DisabledModuleDoesNotAddRotationEvaluation()
        {
            AddRotationModule(new Vector3(0, 0, 180), FluxParticleRotationOverLifetimeMode.Curve, false);
            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            Assert.IsFalse(compiled.Shaders.Compilation.Operations.Any(op => op.Id == "render.rotation-start"));
            Assert.IsFalse(compiled.Shaders.Compilation.Operations.Any(op => op.Id == "render.rotation-over-lifetime"));
            Assert.IsFalse(compiled.InitialValues.ContainsKey("rotation.end"));
        }

        [TestCase((FluxParticleRotationOverLifetimeMode)99, 0f, 0f, 180f)]
        [TestCase(FluxParticleRotationOverLifetimeMode.Linear, 1f, 0f, 180f)]
        [TestCase(FluxParticleRotationOverLifetimeMode.Linear, 0f, -1f, 180f)]
        [TestCase(FluxParticleRotationOverLifetimeMode.Linear, 0f, 0f, float.PositiveInfinity)]
        public void RejectsUnsupportedRotationSettings(FluxParticleRotationOverLifetimeMode mode, float x, float y, float z)
        {
            AddRotationModule(new Vector3(x, y, z), mode);
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(_authoring));
        }

        [TestCase(FluxParticleRotationOverLifetimeMode.Linear)]
        [TestCase(FluxParticleRotationOverLifetimeMode.Curve)]
        public void GpuRotationTracksAgeAndReplaysAfterClear(FluxParticleRotationOverLifetimeMode mode)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required. Do not use -nographics.");
            }

            SetStartRotation(45f);
            AddRotationModule(new Vector3(0, 0, 180), mode);
            var curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
            SetCurve(curve);
            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            var generated = compiled.Shaders;
            var steps = generated.Passes.Select((pass, index) => Import("RotationStep" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => Import("RotationClear" + index, source)).ToArray();
            var renderShader = Import("RotationEvaluation", RenderEvaluationShader(generated));
            Import("RotationBillboard", compiled.RendererSource);
            using (var session = new FluxParticleProgramGpuSession(generated, steps, clears, 2, 1))
            {
                session.SetVector("spawn.position", Vector4.zero);
                session.SetFloat("spawn.speed", 0f);
                session.SetVector("lifetime.range", new Vector4(10, 10, 0, 0));
                session.SetVector("color.start", Vector4.one);
                session.SetVector("color.end", Vector4.one);
                session.SetFloat("render.size", 1f);
                session.SetVector("rotation.start", new Vector4(0, 0, 45, 0));
                session.SetVector("rotation.end", new Vector4(0, 0, 180, 0));
                session.SetUniformFloat("step.delta-time", 0f);
                session.SetSpawnInput("spawn.key", Texture(new Color(123, 0, 0, 0), new Color(456, 0, 0, 0)));
                var mask = Texture(new Color(1, 0, 0, 0), new Color(1, 0, 0, 0));
                session.Step(mask);

                var renderMaterial = new Material(renderShader);
                _objects.Add(renderMaterial);
                Assert.AreEqual(45f, EvaluateRotation(session, renderMaterial), 0.01f);
                session.SetUniformFloat("step.delta-time", 2f);
                session.Step();
                Assert.AreEqual(45 + 180 * (mode == FluxParticleRotationOverLifetimeMode.Curve ? curve.Evaluate(0.2f) : 0.2f),
                    EvaluateRotation(session, renderMaterial), 0.05f);
                session.SetUniformFloat("step.delta-time", 3f);
                session.Step();
                Assert.AreEqual(45 + 180 * (mode == FluxParticleRotationOverLifetimeMode.Curve ? curve.Evaluate(0.5f) : 0.5f),
                    EvaluateRotation(session, renderMaterial), 0.05f);
                session.Clear();
                session.SetUniformFloat("step.delta-time", 0f);
                session.Step(mask);
                Assert.AreEqual(45f, EvaluateRotation(session, renderMaterial), 0.01f);
            }
        }

        [Test]
        public void CurveBakesFixedLookupWithoutNewStateOrPass()
        {
            var baseline = FluxParticleBuildPipeline.Build(_authoring);
            AddRotationModule(new Vector3(0, 0, 180), FluxParticleRotationOverLifetimeMode.Curve);
            var curve = AnimationCurve.Linear(0, -0.5f, 1, 1.5f);
            SetCurve(curve);
            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            var program = compiled.Shaders.Compilation;
            var provider = program.Providers["rotation.curve"];
            Assert.AreEqual(FluxParticleProviderKind.LookupTable, provider.Kind);
            Assert.AreEqual(128, provider.LookupSamples.Count);
            Assert.AreEqual(-0.5f, provider.LookupSamples[0]);
            Assert.AreEqual(1.5f, provider.LookupSamples.Last());
            for (var i = 0; i < provider.LookupSamples.Count; i++)
            {
                Assert.AreEqual(curve.Evaluate(i / (float)(provider.LookupSamples.Count - 1)), provider.LookupSamples[i], 0.00001f);
            }
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, program.StateLayout.Pages.Count);
            Assert.AreEqual(baseline.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            Assert.IsFalse(program.StateLayout.TryGetBinding("rotation.curve", out _));
            Assert.IsFalse(compiled.Shaders.ExternalProviders.Any(value => value.ValueId == "rotation.curve"));
            StringAssert.Contains("tex2Dlod", compiled.Shaders.RenderSource);
            SetCurve(AnimationCurve.Linear(0, 1, 1, 0));
            Assert.AreEqual(-0.5f, provider.LookupSamples[0]);
        }

        [Test]
        public void RejectsEmptyRotationCurve()
        {
            AddRotationModule(new Vector3(0, 0, 180), FluxParticleRotationOverLifetimeMode.Curve);
            SetCurve(new AnimationCurve());
            StringAssert.Contains("finite keys", Assert.Throws<InvalidOperationException>(() =>
                FluxParticleBuildPipeline.Build(_authoring)).Message);
        }

        void SetCurve(AnimationCurve curve)
        {
            var serialized = new SerializedObject(_authoring);
            serialized.FindProperty("modules").GetArrayElementAtIndex(0).FindPropertyRelative("curve").animationCurveValue = curve;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        void SetStartRotation(float z)
        {
            var serialized = new SerializedObject(_authoring);
            serialized.FindProperty("render").FindPropertyRelative("startRotation").vector3Value = new Vector3(0, 0, z);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        void AddRotationModule(Vector3 endRotation, FluxParticleRotationOverLifetimeMode mode, bool enabled = true)
        {
            var serialized = new SerializedObject(_authoring);
            var modules = serialized.FindProperty("modules");
            var index = modules.arraySize;
            modules.arraySize++;
            modules.GetArrayElementAtIndex(index).managedReferenceValue = new FluxParticleRotationOverLifetimeModule();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            var module = serialized.FindProperty("modules").GetArrayElementAtIndex(index);
            module.FindPropertyRelative("enabled").boolValue = enabled;
            module.FindPropertyRelative("mode").intValue = (int)mode;
            module.FindPropertyRelative("endRotation").vector3Value = endRotation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        Shader Import(string name, string source)
        {
            if (!AssetDatabase.IsValidFolder(_temporaryFolder))
            {
                AssetDatabase.CreateFolder("Assets/FluxFX/Tests", Path.GetFileName(_temporaryFolder));
            }
            var path = _temporaryFolder + "/" + name + ".shader";
            File.WriteAllText(path, source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.IsNotNull(shader, path);
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), path);
            return shader;
        }

        Texture2D Texture(params Color[] colors)
        {
            var texture = new Texture2D(colors.Length, 1, TextureFormat.RGBAFloat, false, true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(colors);
            texture.Apply(false, false);
            _objects.Add(texture);
            return texture;
        }

        static string RenderEvaluationShader(FluxParticleShaderProgram generated)
        {
            return "Shader \"Hidden/FluxFX/Tests/RotationEvaluation\" { SubShader { Pass { ZTest Always Cull Off ZWrite Off\n" +
                "HLSLPROGRAM\n#pragma target 3.5\n#pragma vertex vert_img\n#pragma fragment frag\n#include \"UnityCG.cginc\"\n" +
                generated.RenderSource + "\nfloat4 frag(v2f_img i) : SV_Target { bool alive; FluxRenderContext ctx = FluxFX_EvaluateRender(i.uv, alive); " +
                "return alive ? float4(ctx.valueRenderRotation, 1) : float4(0,0,0,0); }\nENDHLSL\n} } Fallback Off }";
        }

        static float EvaluateRotation(FluxParticleProgramGpuSession session, Material material)
        {
            session.BindRender(material);
            var target = new RenderTexture(2, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.filterMode = FilterMode.Point;
            target.Create();
            var previous = RenderTexture.active;
            var texture = new Texture2D(2, 1, TextureFormat.RGBAFloat, false, true);
            try
            {
                Graphics.Blit(null, target, material);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 2, 1), 0, 0);
                texture.Apply();
                return texture.GetPixel(0, 0).b;
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(texture);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }
    }
}
