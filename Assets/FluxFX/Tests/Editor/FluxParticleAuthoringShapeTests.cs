using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleAuthoringShapeTests
    {
        GameObject _root;
        FluxParticleAuthoring _authoring;
        string _temporaryFolder;
        readonly List<Object> _objects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Sphere Shape Test");
            _authoring = _root.AddComponent<FluxParticleAuthoring>();
            _temporaryFolder = "Assets/FluxFX/Tests/ShapeTemporary_" + Guid.NewGuid().ToString("N");
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

        [Test]
        public void SphereUsesSpawnKeyWithoutAdditionalStatePages()
        {
            var point = new FluxParticleAuthoringProgram(_authoring);
            SetShape(FluxParticleShapeType.Sphere, 2.5f);
            var sphere = new FluxParticleAuthoringProgram(_authoring);

            Assert.AreEqual(point.Program.Program.StateLayout.Pages.Count, sphere.Program.Program.StateLayout.Pages.Count);
            Assert.AreEqual(point.Program.Passes.Count, sphere.Program.Passes.Count);
            Assert.IsFalse(point.InitialValues.ContainsKey("shape.radius"));
            Assert.AreEqual(2.5f, sphere.InitialValues["shape.radius"].x);
            Assert.IsFalse(sphere.Program.Program.StateLayout.TryGetBinding("shape.radius", out _));

            var position = sphere.Program.Program.Operations.Single(operation => operation.Id == "spawn.position");
            Assert.IsTrue(position.Reads.Any(read => read.ValueId == "spawn.key" && read.Version == FluxParticleReadVersion.Provided));
            CollectionAssert.Contains(position.Parameters.ToArray(), "shape.radius");
            StringAssert.EndsWith("/AuthoringSphereSpawnPosition.hlsl", position.HlslPath);
            StringAssert.EndsWith("/SpawnPosition.hlsl", point.Program.Program.Operations.Single(operation => operation.Id == "spawn.position").HlslPath);
        }

        [Test]
        public void RandomStartSpeedUsesSpawnKeyWithoutAdditionalStatePages()
        {
            var constant = new FluxParticleAuthoringProgram(_authoring);
            var serialized = new SerializedObject(_authoring);
            var velocity = serialized.FindProperty("initialVelocity");
            velocity.FindPropertyRelative("mode").enumValueIndex = (int)FluxParticleStartSpeedMode.RandomBetweenTwoConstants;
            velocity.FindPropertyRelative("min").floatValue = 0.5f;
            velocity.FindPropertyRelative("max").floatValue = 2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var random = new FluxParticleAuthoringProgram(_authoring);
            Assert.AreEqual(constant.Program.Program.StateLayout.Pages.Count, random.Program.Program.StateLayout.Pages.Count);
            Assert.AreEqual(constant.Program.Passes.Count, random.Program.Passes.Count);
            Assert.IsFalse(random.InitialValues.ContainsKey("spawn.speed"));
            Assert.AreEqual(new Vector4(0.5f, 2f, 0, 0), random.InitialValues["spawn.speed-range"]);
            Assert.IsFalse(random.Program.Program.StateLayout.TryGetBinding("spawn.speed-range", out _));
            var spawn = random.Program.Program.Operations.Single(op => op.Id == "spawn.velocity");
            CollectionAssert.Contains(spawn.Parameters.ToArray(), "spawn.speed-range");
            StringAssert.EndsWith("/AuthoringRandomSpawnVelocity.hlsl", spawn.HlslPath);
        }

        [Test]
        public void RandomStartSpeedRejectsNonFiniteRange()
        {
            var serialized = new SerializedObject(_authoring);
            var velocity = serialized.FindProperty("initialVelocity");
            velocity.FindPropertyRelative("mode").enumValueIndex = (int)FluxParticleStartSpeedMode.RandomBetweenTwoConstants;
            velocity.FindPropertyRelative("min").floatValue = float.NaN;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => new FluxParticleAuthoringProgram(_authoring));
        }

        [TestCase(FluxParticleShapeType.Hemisphere, "AuthoringHemisphereSpawnPosition.hlsl", "shape.radius")]
        [TestCase(FluxParticleShapeType.Circle, "AuthoringCircleSpawnPosition.hlsl", "shape.radius")]
        [TestCase(FluxParticleShapeType.Box, "AuthoringBoxSpawnPosition.hlsl", "shape.size")]
        public void AdditionalShapesUseDeterministicSpawnWithoutNewState(
            FluxParticleShapeType type, string hlslName, string parameter)
        {
            var point = new FluxParticleAuthoringProgram(_authoring);
            SetShape(type, 2.5f);
            var compiled = new FluxParticleAuthoringProgram(_authoring);

            Assert.AreEqual(point.Program.Program.StateLayout.Pages.Count, compiled.Program.Program.StateLayout.Pages.Count);
            Assert.AreEqual(point.Program.Passes.Count, compiled.Program.Passes.Count);
            Assert.IsFalse(compiled.Program.Program.StateLayout.TryGetBinding(parameter, out _));
            var position = compiled.Program.Program.Operations.Single(operation => operation.Id == "spawn.position");
            Assert.IsTrue(position.Reads.Any(read => read.ValueId == "spawn.key" && read.Version == FluxParticleReadVersion.Provided));
            CollectionAssert.Contains(position.Parameters.ToArray(), parameter);
            StringAssert.EndsWith("/" + hlslName, position.HlslPath);
            Assert.IsTrue(compiled.InitialValues.ContainsKey(parameter));
        }

        [TestCase(FluxParticleShapeType.Sphere)]
        [TestCase(FluxParticleShapeType.Hemisphere)]
        public void RadialShapeSharesDerivedSpawnDirectionWithoutAdditionalPages(FluxParticleShapeType shape)
        {
            var point = new FluxParticleAuthoringProgram(_authoring);
            SetShape(shape, 2.5f);
            var compiled = new FluxParticleAuthoringProgram(_authoring);
            var program = compiled.Program.Program;

            Assert.AreEqual(point.Program.Program.StateLayout.Pages.Count, program.StateLayout.Pages.Count);
            Assert.AreEqual(point.Program.Passes.Count, compiled.Program.Passes.Count);
            Assert.IsFalse(program.StateLayout.TryGetBinding("spawn.direction", out _));
            var position = program.Operations.Single(operation => operation.Id == "spawn.position");
            var velocity = program.Operations.Single(operation => operation.Id == "spawn.velocity");
            Assert.IsTrue(position.Reads.Any(read => read.ValueId == "spawn.key" &&
                read.Version == FluxParticleReadVersion.Provided));
            Assert.IsTrue(velocity.Reads.Any(read => read.ValueId == "spawn.direction" &&
                read.Version == FluxParticleReadVersion.Provided));
            Assert.IsTrue(position.HlslPath.Contains("SpawnPosition.hlsl"));
            Assert.IsTrue(velocity.HlslPath.Contains("Radial"));
        }

        [TestCase(FluxParticleStartSpeedMode.Constant)]
        [TestCase(FluxParticleStartSpeedMode.RandomBetweenTwoConstants)]
        public void ConeUsesShapeParametersAndDerivedVelocityDirectionWithoutExtraPages(FluxParticleStartSpeedMode mode)
        {
            var point = new FluxParticleAuthoringProgram(_authoring);
            SetShape(FluxParticleShapeType.Cone, 2.5f);
            var serialized = new SerializedObject(_authoring);
            serialized.FindProperty("shape").FindPropertyRelative("angle").floatValue = 30f;
            serialized.FindProperty("initialVelocity").FindPropertyRelative("mode").enumValueIndex = (int)mode;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var compiled = new FluxParticleAuthoringProgram(_authoring);
            Assert.AreEqual(point.Program.Program.StateLayout.Pages.Count, compiled.Program.Program.StateLayout.Pages.Count);
            Assert.AreEqual(point.Program.Passes.Count, compiled.Program.Passes.Count);
            Assert.AreEqual(2.5f, compiled.InitialValues["shape.radius"].x);
            Assert.AreEqual(30f, compiled.InitialValues["shape.angle"].x);
            Assert.IsFalse(compiled.Program.Program.StateLayout.TryGetBinding("spawn.direction", out _));
            var position = compiled.Program.Program.Operations.Single(operation => operation.Id == "spawn.position");
            var velocity = compiled.Program.Program.Operations.Single(operation => operation.Id == "spawn.velocity");
            CollectionAssert.Contains(position.Parameters.ToArray(), "shape.radius");
            Assert.IsTrue(velocity.Reads.Any(read => read.ValueId == "spawn.direction" &&
                read.Version == FluxParticleReadVersion.Provided));
            StringAssert.EndsWith("/AuthoringConeSpawnPosition.hlsl", position.HlslPath);
            StringAssert.Contains("AuthoringRadial", velocity.HlslPath);
        }

        [Test]
        public void ConeRejectsInvalidAngle()
        {
            SetShape(FluxParticleShapeType.Cone, 1f);
            var serialized = new SerializedObject(_authoring);
            serialized.FindProperty("shape").FindPropertyRelative("angle").floatValue = 90f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => new FluxParticleAuthoringProgram(_authoring));
        }

        [TestCase(FluxParticleStartColorMode.Constant, FluxParticleStartSizeMode.RandomBetweenTwoConstants)]
        [TestCase(FluxParticleStartColorMode.RandomBetweenTwoConstants, FluxParticleStartSizeMode.Constant)]
        [TestCase(FluxParticleStartColorMode.RandomBetweenTwoConstants, FluxParticleStartSizeMode.RandomBetweenTwoConstants)]
        public void RandomStartVisualsUseSpawnKeyWithoutExtraState(
            FluxParticleStartColorMode colorMode, FluxParticleStartSizeMode sizeMode)
        {
            var point = new FluxParticleAuthoringProgram(_authoring);
            var serialized = new SerializedObject(_authoring);
            var render = serialized.FindProperty("render");
            render.FindPropertyRelative("startColorMode").enumValueIndex = (int)colorMode;
            render.FindPropertyRelative("startSizeMode").enumValueIndex = (int)sizeMode;
            render.FindPropertyRelative("startSizeMin").floatValue = 0.5f;
            render.FindPropertyRelative("startSizeMax").floatValue = 2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var compiled = new FluxParticleAuthoringProgram(_authoring);
            Assert.AreEqual(point.Program.Program.StateLayout.Pages.Count, compiled.Program.Program.StateLayout.Pages.Count);
            Assert.AreEqual(point.Program.Passes.Count, compiled.Program.Passes.Count);
            var shader = compiled.GenerateRenderer();
            if (colorMode == FluxParticleStartColorMode.RandomBetweenTwoConstants)
            {
                Assert.IsTrue(compiled.InitialValues.ContainsKey("color.start-min"));
                Assert.IsTrue(compiled.InitialValues.ContainsKey("color.start-max"));
                StringAssert.Contains("AuthoringRandomStartColor.hlsl", compiled.Program.RenderSource);
            }
            if (sizeMode == FluxParticleStartSizeMode.RandomBetweenTwoConstants)
            {
                Assert.AreEqual(new Vector4(0.5f, 2f, 0, 0), compiled.InitialValues["render.size-range"]);
                StringAssert.Contains("FluxFXRandom01", shader);
                Assert.IsFalse(compiled.Program.Program.StateLayout.TryGetBinding("render.size-range", out _));
            }
        }

        [Test]
        public void SphereRejectsInvalidRadius()
        {
            SetShape(FluxParticleShapeType.Sphere, -1);
            Assert.Throws<InvalidOperationException>(() => new FluxParticleAuthoringProgram(_authoring));
            SetShape(FluxParticleShapeType.Sphere, float.NaN);
            Assert.Throws<InvalidOperationException>(() => new FluxParticleAuthoringProgram(_authoring));
        }

        [Test]
        public void GpuSphereSpawnIsWithinRadiusAndDeterministic()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required. Do not use -nographics.");
            }

            SetShape(FluxParticleShapeType.Sphere, 2.5f);
            var generated = new FluxParticleAuthoringProgram(_authoring).Program;
            var steps = generated.Passes.Select((pass, index) => Import("Step" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => Import("Clear" + index, source)).ToArray();

            using (var session = new FluxParticleProgramGpuSession(generated, steps, clears, 2, 1))
            {
                session.SetVector("spawn.position", Vector4.zero);
                session.SetFloat("spawn.speed", 0);
                session.SetFloat("shape.radius", 2.5f);
                session.SetVector("gravity", Vector4.zero);
                session.SetFloat("gravity.enabled", 0);
                session.SetFloat("drag", 0);
                session.SetFloat("drag.enabled", 0);
                session.SetVector("lifetime.range", new Vector4(3, 3, 0, 0));
                session.SetVector("color.start", Vector4.one);
                session.SetVector("color.end", Vector4.one);
                session.SetFloat("render.size", 1);
                session.SetUniformFloat("step.delta-time", 0);
                session.SetSpawnInput("spawn.key", Texture(new Color(123, 0, 0, 0), new Color(456, 0, 0, 0)));

                var mask = Texture(new Color(1, 0, 0, 0), new Color(1, 0, 0, 0));
                session.Step(mask);
                var first = ReadPosition(session, generated, 0);
                var second = ReadPosition(session, generated, 1);
                Assert.LessOrEqual(first.magnitude, 2.501f);
                Assert.LessOrEqual(second.magnitude, 2.501f);
                Assert.Greater((first - second).sqrMagnitude, 0.0001f);

                session.Clear();
                session.Step(mask);
                Assert.LessOrEqual((first - ReadPosition(session, generated, 0)).sqrMagnitude, 0.000001f);
                Assert.LessOrEqual((second - ReadPosition(session, generated, 1)).sqrMagnitude, 0.000001f);
            }
        }

        [TestCase(FluxParticleShapeType.Sphere, FluxParticleStartSpeedMode.Constant)]
        [TestCase(FluxParticleShapeType.Hemisphere, FluxParticleStartSpeedMode.Constant)]
        [TestCase(FluxParticleShapeType.Cone, FluxParticleStartSpeedMode.Constant)]
        [TestCase(FluxParticleShapeType.Cone, FluxParticleStartSpeedMode.RandomBetweenTwoConstants)]
        [TestCase(FluxParticleShapeType.Hemisphere, FluxParticleStartSpeedMode.RandomBetweenTwoConstants)]
        [TestCase(FluxParticleShapeType.Point, FluxParticleStartSpeedMode.RandomBetweenTwoConstants)]
        public void GpuSpawnDirectionSpeedAndRepeatability(FluxParticleShapeType shape, FluxParticleStartSpeedMode speedMode)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required. Do not use -nographics.");
            }
            SetShape(shape, 2.5f);
            var serialized = new SerializedObject(_authoring);
            serialized.FindProperty("shape").FindPropertyRelative("angle").floatValue = 30f;
            var velocity = serialized.FindProperty("initialVelocity");
            velocity.FindPropertyRelative("mode").enumValueIndex = (int)speedMode;
            velocity.FindPropertyRelative("speed").floatValue = 2f;
            velocity.FindPropertyRelative("min").floatValue = 1f;
            velocity.FindPropertyRelative("max").floatValue = 3f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var generated = new FluxParticleAuthoringProgram(_authoring).Program;
            var steps = generated.Passes.Select((pass, index) => Import("DirectionStep" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => Import("DirectionClear" + index, source)).ToArray();
            using (var session = new FluxParticleProgramGpuSession(generated, steps, clears, 2, 1))
            {
                session.SetVector("spawn.position", Vector4.zero);
                if (speedMode == FluxParticleStartSpeedMode.Constant) session.SetFloat("spawn.speed", 2f);
                else session.SetVector("spawn.speed-range", new Vector4(1, 3, 0, 0));
                if (shape != FluxParticleShapeType.Point) session.SetFloat("shape.radius", 2.5f);
                if (shape == FluxParticleShapeType.Cone) session.SetFloat("shape.angle", 30f);
                session.SetVector("gravity", Vector4.zero);
                session.SetFloat("gravity.enabled", 0);
                session.SetFloat("drag", 0);
                session.SetFloat("drag.enabled", 0);
                session.SetVector("lifetime.range", new Vector4(3, 3, 0, 0));
                session.SetVector("color.start", Vector4.one);
                session.SetVector("color.end", Vector4.one);
                session.SetFloat("render.size", 1);
                session.SetUniformFloat("step.delta-time", 0);
                session.SetSpawnInput("spawn.key", Texture(new Color(123, 0, 0, 0), new Color(456, 0, 0, 0)));
                var mask = Texture(new Color(1, 0, 0, 0), new Color(1, 0, 0, 0));
                session.Step(mask);
                var positions = new Vector3[2];
                var velocities = new Vector3[2];
                for (var i = 0; i < 2; i++)
                {
                    positions[i] = ReadPosition(session, generated, i);
                    velocities[i] = ReadVelocity(session, generated, i);
                    var speed = velocities[i].magnitude;
                    if (speedMode == FluxParticleStartSpeedMode.Constant) Assert.AreEqual(2f, speed, 0.001f);
                    else Assert.That(speed, Is.InRange(0.999f, 3.001f));
                    if (shape == FluxParticleShapeType.Hemisphere)
                    {
                        Assert.GreaterOrEqual(positions[i].y, -0.0001f);
                        Assert.GreaterOrEqual(velocities[i].y, -0.0001f);
                    }
                    if (shape == FluxParticleShapeType.Sphere || shape == FluxParticleShapeType.Hemisphere)
                    {
                        Assert.LessOrEqual(positions[i].magnitude, 2.501f);
                        if (positions[i].sqrMagnitude > 0.000001f)
                            Assert.Greater(Vector3.Dot(positions[i].normalized, velocities[i].normalized), 0.999f);
                    }
                    if (shape == FluxParticleShapeType.Cone)
                    {
                        Assert.AreEqual(0f, positions[i].y, 0.0001f);
                        Assert.LessOrEqual(new Vector2(positions[i].x, positions[i].z).magnitude, 2.501f);
                        Assert.Greater(velocities[i].y, 0f);
                        Assert.GreaterOrEqual(Vector3.Dot(velocities[i].normalized, Vector3.up),
                            Mathf.Cos(30f * Mathf.Deg2Rad) - 0.001f);
                        Assert.GreaterOrEqual(positions[i].x * velocities[i].x + positions[i].z * velocities[i].z, -0.0001f);
                    }
                }
                session.Clear();
                session.Step(mask);
                for (var i = 0; i < 2; i++)
                {
                    Assert.Less((positions[i] - ReadPosition(session, generated, i)).sqrMagnitude, 0.000001f);
                    Assert.Less((velocities[i] - ReadVelocity(session, generated, i)).sqrMagnitude, 0.000001f);
                }
            }
        }

        static Vector3 ReadVelocity(FluxParticleProgramGpuSession session, FluxParticleShaderProgram generated, int particle)
        {
            Assert.IsTrue(generated.Program.StateLayout.TryGetBinding("particle.velocity", out var binding));
            var source = session.CurrentPages[binding.PageId];
            var active = RenderTexture.active;
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
            try
            {
                RenderTexture.active = source;
                texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                texture.Apply();
                var value = (Vector4)texture.GetPixels()[particle];
                return new Vector3(value[binding.ChannelOffset], value[binding.ChannelOffset + 1], value[binding.ChannelOffset + 2]);
            }
            finally
            {
                RenderTexture.active = active;
                Object.DestroyImmediate(texture);
            }
        }
        [Test]
        public void GpuRandomStartColorUsesSpawnKeyAndRemainsStable()
        {
            RequireVisualGpu();
            SetRandomVisualModes(true, true);
            var compiled = new FluxParticleAuthoringProgram(_authoring);
            var generated = compiled.Program;
            var steps = generated.Passes.Select((pass, index) => Import("VisualStep" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => Import("VisualClear" + index, source)).ToArray();
            var renderShader = Import("VisualRender", RenderEvaluationShader(generated));
            using (var session = new FluxParticleProgramGpuSession(generated, steps, clears, 2, 1))
            {
                session.SetVector("spawn.position", Vector4.zero);
                session.SetFloat("spawn.speed", 0);
                session.SetVector("gravity", Vector4.zero);
                session.SetFloat("gravity.enabled", 0);
                session.SetFloat("drag", 0);
                session.SetFloat("drag.enabled", 0);
                session.SetVector("lifetime.range", new Vector4(10, 10, 0, 0));
                session.SetVector("color.start-min", new Vector4(0, 0, 0, 0));
                session.SetVector("color.start-max", new Vector4(1, 1, 1, 1));
                session.SetVector("color.end", Vector4.one);
                session.SetVector("render.size-range", new Vector4(0.5f, 2f, 0, 0));
                session.SetUniformFloat("step.delta-time", 0);
                session.SetSpawnInput("spawn.key", Texture(new Color(123, 0, 0, 0), new Color(456, 0, 0, 0)));
                var mask = Texture(new Color(1, 0, 0, 0), new Color(1, 0, 0, 0));
                session.Step(mask);

                var renderMaterial = new Material(renderShader);
                _objects.Add(renderMaterial);
                var first = EvaluateColors(session, renderMaterial);
                Assert.That(first[0].r, Is.InRange(0f, 1f));
                Assert.That(first[1].r, Is.InRange(0f, 1f));
                Assert.Greater(Mathf.Abs(first[0].r - first[1].r), 0.00001f);
                session.SetUniformFloat("step.delta-time", 1f);
                session.Step();
                var later = EvaluateColors(session, renderMaterial);
                for (var i = 0; i < 2; i++)
                {
                    Assert.AreEqual(first[i].r, later[i].r, 0.0001f);
                    Assert.AreEqual(first[i].a, later[i].a, 0.0001f);
                }
                session.Clear();
                session.SetUniformFloat("step.delta-time", 0);
                session.Step(mask);
                var repeated = EvaluateColors(session, renderMaterial);
                for (var i = 0; i < 2; i++) Assert.AreEqual(first[i].r, repeated[i].r, 0.0001f);
            }
        }

        [Test]
        public void RandomSizeBillboardReadsPersistentSpawnKeyWithoutAdditionalPages()
        {
            var baseline = new FluxParticleAuthoringProgram(_authoring);
            SetRandomVisualModes(true, true);
            var compiled = new FluxParticleAuthoringProgram(_authoring);
            Assert.AreEqual(baseline.Program.Program.StateLayout.Pages.Count, compiled.Program.Program.StateLayout.Pages.Count);
            Assert.AreEqual(baseline.Program.Passes.Count, compiled.Program.Passes.Count);
            Assert.IsTrue(compiled.Program.Program.StateLayout.TryGetBinding("particle.spawn-key", out _));
            Assert.IsFalse(compiled.Program.Program.StateLayout.TryGetBinding("render.size-range", out _));
            var shader = compiled.GenerateRenderer();
            StringAssert.Contains("FluxFXRandom01", shader);
            StringAssert.Contains("FluxFXRandom01", shader);
            StringAssert.Contains("59u + 43u", shader);
        }

        void SetRandomVisualModes(bool color, bool size)
        {
            var serialized = new SerializedObject(_authoring);
            var render = serialized.FindProperty("render");
            if (color)
            {
                render.FindPropertyRelative("startColorMode").enumValueIndex = (int)FluxParticleStartColorMode.RandomBetweenTwoConstants;
                render.FindPropertyRelative("startColorMin").colorValue = Color.black;
                render.FindPropertyRelative("startColorMax").colorValue = Color.white;
            }
            if (size)
            {
                render.FindPropertyRelative("startSizeMode").enumValueIndex = (int)FluxParticleStartSizeMode.RandomBetweenTwoConstants;
                render.FindPropertyRelative("startSizeMin").floatValue = 0.5f;
                render.FindPropertyRelative("startSizeMax").floatValue = 2f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static string RenderEvaluationShader(FluxParticleShaderProgram generated)
        {
            return "Shader \"Hidden/FluxFX/Tests/VisualEval\" { SubShader { Pass { ZTest Always Cull Off ZWrite Off\n" +
                "HLSLPROGRAM\n#pragma target 3.5\n#pragma vertex vert_img\n#pragma fragment frag\n#include \"UnityCG.cginc\"\n" +
                generated.RenderSource + "\nfloat4 frag(v2f_img i) : SV_Target { bool alive; FluxRenderContext ctx = FluxFX_EvaluateRender(i.uv, alive); " +
                "return alive ? ctx.valueRenderColor : float4(0,0,0,0); }\nENDHLSL\n} } Fallback Off }";
        }

        static Color[] EvaluateColors(FluxParticleProgramGpuSession session, Material material)
        {
            session.BindRender(material);
            var target = new RenderTexture(2, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.filterMode = FilterMode.Point;
            target.Create();
            var active = RenderTexture.active;
            var texture = new Texture2D(2, 1, TextureFormat.RGBAFloat, false, true);
            try
            {
                Graphics.Blit(null, target, material);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 2, 1), 0, 0);
                texture.Apply();
                return texture.GetPixels();
            }
            finally
            {
                RenderTexture.active = active;
                Object.DestroyImmediate(texture);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        static void RequireVisualGpu()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures required; do not use -nographics.");
            }
        }
        void SetShape(FluxParticleShapeType type, float radius)
        {
            var serialized = new SerializedObject(_authoring);
            var shape = serialized.FindProperty("shape");
            shape.FindPropertyRelative("type").enumValueIndex = (int)type;
            shape.FindPropertyRelative("radius").floatValue = radius;
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

        static Vector3 ReadPosition(FluxParticleProgramGpuSession session, FluxParticleShaderProgram generated, int particle)
        {
            Assert.IsTrue(generated.Program.StateLayout.TryGetBinding("particle.position", out var binding));
            var source = session.CurrentPages[binding.PageId];
            var active = RenderTexture.active;
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
            try
            {
                RenderTexture.active = source;
                texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                texture.Apply();
                var value = (Vector4)texture.GetPixels()[particle];
                return new Vector3(value[binding.ChannelOffset], value[binding.ChannelOffset + 1], value[binding.ChannelOffset + 2]);
            }
            finally
            {
                RenderTexture.active = active;
                Object.DestroyImmediate(texture);
            }
        }
    }
}
