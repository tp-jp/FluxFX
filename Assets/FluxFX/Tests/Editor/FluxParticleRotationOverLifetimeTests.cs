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

        [TestCase(false)]
        [TestCase(true)]
        public void RandomStartRotationUsesSpawnKeyWithoutNewStateOrPass(bool mesh)
        {
            var baseline = FluxParticleBuildPipeline.Build(_authoring);
            ConfigureRandomRotation(mesh, false);
            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            Assert.AreEqual(baseline.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, compiled.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.IsFalse(compiled.Shaders.Compilation.StateLayout.TryGetBinding("particle.start-rotation", out _));
            Assert.IsFalse(compiled.InitialValues.ContainsKey("rotation.start"));
            Assert.AreEqual(FluxParticleParameterPolicy.Static, compiled.Shaders.Compilation.Parameters["rotation.start-min"].Policy);
            Assert.AreEqual(mesh ? new Vector4(-90, -30, -180, 0) : new Vector4(0, 0, -180, 0),
                compiled.InitialValues["rotation.start-min"]);
            Assert.AreEqual(mesh ? new Vector4(90, 120, 180, 0) : new Vector4(0, 0, 180, 0),
                compiled.InitialValues["rotation.start-max"]);
            CollectionAssert.Contains(compiled.Shaders.Compilation.Providers["particle.start-rotation"].Inputs, "particle.spawn-key");
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void GpuRandomRotationIsStablePerParticleComposesWithLifetimeAndReplays(bool mesh, bool lifetime)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            ConfigureRandomRotation(mesh, lifetime);
            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            var generated = compiled.Shaders;
            var steps = generated.Passes.Select((pass, index) => Import("RandomStep" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => Import("RandomClear" + index, source)).ToArray();
            var shader = Import("RandomEvaluation", RenderEvaluationShader(generated));
            Import("RandomRenderer", compiled.RendererSource);
            var material = new Material(shader);
            _objects.Add(material);
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
                var first = EvaluateRotations(session, material);
                Assert.Greater(Mathf.Abs(first[0].b - first[1].b), 0.001f);
                var min = compiled.InitialValues["rotation.start-min"];
                var max = compiled.InitialValues["rotation.start-max"];
                for (var particle = 0; particle < 2; particle++)
                {
                    for (var axis = 0; axis < 3; axis++)
                    {
                        Assert.That(first[particle][axis], Is.InRange(min[axis] - 0.001f, max[axis] + 0.001f));
                    }
                }
                session.SetUniformFloat("step.delta-time", 2);
                session.Step();
                var middle = EvaluateRotations(session, material);
                var delta = lifetime ? (Vector3)_authoring.Modules.OfType<FluxParticleRotationOverLifetimeModule>().Single().EndRotation * 0.2f
                    : Vector3.zero;
                for (var particle = 0; particle < 2; particle++)
                {
                    for (var axis = 0; axis < 3; axis++)
                    {
                        Assert.AreEqual(first[particle][axis] + delta[axis], middle[particle][axis], 0.002f);
                    }
                }
                Assert.Throws<InvalidOperationException>(() => session.SetVector("rotation.start-min", Vector4.one));
                session.Clear();
                session.SetUniformFloat("step.delta-time", 0);
                session.Step(mask);
                var replay = EvaluateRotations(session, material);
                for (var particle = 0; particle < 2; particle++)
                {
                    for (var axis = 0; axis < 3; axis++)
                    {
                        Assert.AreEqual(first[particle][axis], replay[particle][axis], 0.002f);
                    }
                }
            }
        }

        [TestCase("startRotationMin", 1f, 0f, 0f)]
        [TestCase("startRotationMax", 0f, 1f, 0f)]
        [TestCase("startRotationMin", 0f, 0f, float.NaN)]
        [TestCase("startRotationMax", 0f, 0f, float.PositiveInfinity)]
        public void RandomBillboardRotationRejectsUnsupportedRanges(string field, float x, float y, float z)
        {
            ConfigureRandomRotation(false, false);
            var serialized = new SerializedObject(_authoring);
            serialized.FindProperty("render").FindPropertyRelative(field).vector3Value = new Vector3(x, y, z);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(_authoring));
        }

        [Test]
        public void UnknownStartRotationModeIsRejected()
        {
            var serialized = new SerializedObject(_authoring);
            serialized.FindProperty("render.startRotationMode").intValue = 99;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(_authoring));
        }
        void ConfigureRandomRotation(bool mesh, bool lifetime)
        {
            var serialized = new SerializedObject(_authoring);
            var render = serialized.FindProperty("render");
            render.FindPropertyRelative("startRotationMode").intValue = (int)FluxParticleStartRotationMode.RandomBetweenTwoConstants;
            render.FindPropertyRelative("startRotationMin").vector3Value = mesh ? new Vector3(90, -30, 180) : new Vector3(0, 0, 180);
            render.FindPropertyRelative("startRotationMax").vector3Value = mesh ? new Vector3(-90, 120, -180) : new Vector3(0, 0, -180);
            render.FindPropertyRelative("startRotation").vector3Value = new Vector3(float.NaN, 0, 0);
            if (mesh)
            {
                var source = new Mesh
                {
                    vertices = new[] { Vector3.zero, Vector3.right, Vector3.up },
                    triangles = new[] { 0, 1, 2 }
                };
                _objects.Add(source);
                render.FindPropertyRelative("mode").intValue = (int)FluxParticleRenderMode.Mesh;
                render.FindPropertyRelative("mesh").objectReferenceValue = source;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (lifetime)
            {
                AddRotationModule(mesh ? new Vector3(30, 60, 90) : new Vector3(0, 0, 90), FluxParticleRotationOverLifetimeMode.Linear);
            }
        }

        static Color[] EvaluateRotations(FluxParticleProgramGpuSession session, Material material)
        {
            session.BindRender(material);
            var target = new RenderTexture(2, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var texture = new Texture2D(2, 1, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                Graphics.Blit(null, target, material);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 2, 1), 0, 0);
                texture.Apply();
                return texture.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
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
            File.WriteAllText(path, source.Replace("\r\n", "\n").Replace("\r", "\n"));
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
            return "Shader \"Hidden/FluxFX/Tests/RotationEvaluation\" { Properties {\n" +
                FluxParticleProgramShaderGenerator.GenerateParameterProperties(generated.Compilation) + "\n} SubShader { Pass { ZTest Always Cull Off ZWrite Off\n" +
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
