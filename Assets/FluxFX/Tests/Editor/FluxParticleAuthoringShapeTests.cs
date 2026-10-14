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
            var point = FluxParticleBuildPipeline.Build(_authoring);
            SetShape(FluxParticleShapeType.Sphere, 2.5f);
            var sphere = FluxParticleBuildPipeline.Build(_authoring);

            Assert.AreEqual(point.Shaders.Compilation.StateLayout.Pages.Count, sphere.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.AreEqual(point.Shaders.Passes.Count, sphere.Shaders.Passes.Count);
            Assert.IsFalse(point.InitialValues.ContainsKey("shape.radius"));
            Assert.AreEqual(2.5f, sphere.InitialValues["shape.radius"].x);
            Assert.IsFalse(sphere.Shaders.Compilation.StateLayout.TryGetBinding("shape.radius", out _));

            var position = sphere.Shaders.Compilation.Operations.Single(operation => operation.Id == "spawn.position");
            Assert.IsTrue(position.Reads.Any(read => read.ValueId == "spawn.key" && read.Version == FluxParticleReadVersion.Provided));
            CollectionAssert.Contains(position.Parameters.ToArray(), "shape.radius");
            StringAssert.EndsWith("/AuthoringSphereSpawnPosition.hlsl", position.HlslPath);
            StringAssert.EndsWith("/SpawnPosition.hlsl", point.Shaders.Compilation.Operations.Single(operation => operation.Id == "spawn.position").HlslPath);
        }

        [Test]
        public void RandomStartSpeedUsesSpawnKeyWithoutAdditionalStatePages()
        {
            var constant = FluxParticleBuildPipeline.Build(_authoring);
            var serialized = new SerializedObject(_authoring);
            var velocity = serialized.FindProperty("initialVelocity");
            velocity.FindPropertyRelative("mode").enumValueIndex = (int)FluxParticleStartSpeedMode.RandomBetweenTwoConstants;
            velocity.FindPropertyRelative("min").floatValue = 0.5f;
            velocity.FindPropertyRelative("max").floatValue = 2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var random = FluxParticleBuildPipeline.Build(_authoring);
            Assert.AreEqual(constant.Shaders.Compilation.StateLayout.Pages.Count, random.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.AreEqual(constant.Shaders.Passes.Count, random.Shaders.Passes.Count);
            Assert.IsFalse(random.InitialValues.ContainsKey("spawn.speed"));
            Assert.AreEqual(new Vector4(0.5f, 2f, 0, 0), random.InitialValues["spawn.speed-range"]);
            Assert.IsFalse(random.Shaders.Compilation.StateLayout.TryGetBinding("spawn.speed-range", out _));
            var spawn = random.Shaders.Compilation.Operations.Single(op => op.Id == "spawn.velocity");
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
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(_authoring));
        }

        [TestCase(FluxParticleShapeType.Hemisphere, "AuthoringHemisphereSpawnPosition.hlsl", "shape.radius")]
        [TestCase(FluxParticleShapeType.Circle, "AuthoringCircleSpawnPosition.hlsl", "shape.radius")]
        [TestCase(FluxParticleShapeType.Box, "AuthoringBoxSpawnPosition.hlsl", "shape.size")]
        public void AdditionalShapesUseDeterministicSpawnWithoutNewState(
            FluxParticleShapeType type, string hlslName, string parameter)
        {
            var point = FluxParticleBuildPipeline.Build(_authoring);
            SetShape(type, 2.5f);
            var compiled = FluxParticleBuildPipeline.Build(_authoring);

            Assert.AreEqual(point.Shaders.Compilation.StateLayout.Pages.Count, compiled.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.AreEqual(point.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            Assert.IsFalse(compiled.Shaders.Compilation.StateLayout.TryGetBinding(parameter, out _));
            var position = compiled.Shaders.Compilation.Operations.Single(operation => operation.Id == "spawn.position");
            Assert.IsTrue(position.Reads.Any(read => read.ValueId == "spawn.key" && read.Version == FluxParticleReadVersion.Provided));
            CollectionAssert.Contains(position.Parameters.ToArray(), parameter);
            StringAssert.EndsWith("/" + hlslName, position.HlslPath);
            Assert.IsTrue(compiled.InitialValues.ContainsKey(parameter));
        }

        [TestCase(FluxParticleShapeType.Sphere)]
        [TestCase(FluxParticleShapeType.Hemisphere)]
        public void RadialShapeSharesDerivedSpawnDirectionWithoutAdditionalPages(FluxParticleShapeType shape)
        {
            var point = FluxParticleBuildPipeline.Build(_authoring);
            SetShape(shape, 2.5f);
            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            var program = compiled.Shaders.Compilation;

            Assert.AreEqual(point.Shaders.Compilation.StateLayout.Pages.Count, program.StateLayout.Pages.Count);
            Assert.AreEqual(point.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
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
            var point = FluxParticleBuildPipeline.Build(_authoring);
            SetShape(FluxParticleShapeType.Cone, 2.5f);
            var serialized = new SerializedObject(_authoring);
            serialized.FindProperty("shape").FindPropertyRelative("angle").floatValue = 30f;
            serialized.FindProperty("initialVelocity").FindPropertyRelative("mode").enumValueIndex = (int)mode;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            Assert.AreEqual(point.Shaders.Compilation.StateLayout.Pages.Count, compiled.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.AreEqual(point.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            Assert.AreEqual(2.5f, compiled.InitialValues["shape.radius"].x);
            Assert.AreEqual(30f, compiled.InitialValues["shape.angle"].x);
            Assert.IsFalse(compiled.Shaders.Compilation.StateLayout.TryGetBinding("spawn.direction", out _));
            var position = compiled.Shaders.Compilation.Operations.Single(operation => operation.Id == "spawn.position");
            var velocity = compiled.Shaders.Compilation.Operations.Single(operation => operation.Id == "spawn.velocity");
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
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(_authoring));
        }

        [TestCase(FluxParticleStartColorMode.Constant, FluxParticleStartSizeMode.RandomBetweenTwoConstants)]
        [TestCase(FluxParticleStartColorMode.RandomBetweenTwoConstants, FluxParticleStartSizeMode.Constant)]
        [TestCase(FluxParticleStartColorMode.RandomBetweenTwoConstants, FluxParticleStartSizeMode.RandomBetweenTwoConstants)]
        public void RandomStartVisualsUseSpawnKeyWithoutExtraState(
            FluxParticleStartColorMode colorMode, FluxParticleStartSizeMode sizeMode)
        {
            var point = FluxParticleBuildPipeline.Build(_authoring);
            var serialized = new SerializedObject(_authoring);
            var render = serialized.FindProperty("render");
            render.FindPropertyRelative("startColorMode").enumValueIndex = (int)colorMode;
            render.FindPropertyRelative("startSizeMode").enumValueIndex = (int)sizeMode;
            render.FindPropertyRelative("startSizeMin").floatValue = 0.5f;
            render.FindPropertyRelative("startSizeMax").floatValue = 2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            Assert.AreEqual(point.Shaders.Compilation.StateLayout.Pages.Count, compiled.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.AreEqual(point.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            var shader = compiled.RendererSource;
            if (colorMode == FluxParticleStartColorMode.RandomBetweenTwoConstants)
            {
                Assert.IsTrue(compiled.InitialValues.ContainsKey("color.start-min"));
                Assert.IsTrue(compiled.InitialValues.ContainsKey("color.start-max"));
                StringAssert.Contains("AuthoringRandomStartColor.hlsl", compiled.Shaders.RenderSource);
            }
            if (sizeMode == FluxParticleStartSizeMode.RandomBetweenTwoConstants)
            {
                Assert.AreEqual(new Vector4(0.5f, 2f, 0, 0), compiled.InitialValues["render.size-range"]);
                StringAssert.Contains("RandomStartSize.hlsl", shader);
                Assert.IsFalse(compiled.Shaders.Compilation.StateLayout.TryGetBinding("render.size-range", out _));
            }
        }

        [Test]
        public void StartRotationZUsesRenderOperationWithoutAdditionalState()
        {
            var baseline = FluxParticleBuildPipeline.Build(_authoring);
            var baselineShader = baseline.RendererSource;
            Assert.IsFalse(baseline.Shaders.Compilation.Operations.Any(operation => operation.Id == "render.rotation-start"));
            StringAssert.DoesNotContain("sincos(angle, s, c)", baselineShader);

            var serialized = new SerializedObject(_authoring);
            serialized.FindProperty("render").FindPropertyRelative("startRotation").vector3Value = new Vector3(0, 0, 45);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var rotated = FluxParticleBuildPipeline.Build(_authoring);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, rotated.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.AreEqual(baseline.Shaders.Passes.Count, rotated.Shaders.Passes.Count);
            Assert.AreEqual(new Vector4(0, 0, 45, 0), rotated.InitialValues["rotation.start"]);
            Assert.IsFalse(rotated.Shaders.Compilation.StateLayout.TryGetBinding("render.rotation", out _));
            var operation = rotated.Shaders.Compilation.Operations.Single(value => value.Id == "render.rotation-start");
            Assert.AreEqual(FluxParticlePhase.Render, operation.Phase);
            CollectionAssert.Contains(operation.Parameters.ToArray(), "rotation.start");
            StringAssert.EndsWith("/AuthoringStartRotation.hlsl", operation.HlslPath);
            var shader = rotated.RendererSource;
            StringAssert.Contains("render.valueRenderRotation.z", shader);
            StringAssert.Contains("sincos(angle, s, c)", shader);
            Import("StartRotationBillboard", shader);
        }

        [TestCase(45f, 1f, 0f)]
        [TestCase(45f, 0f, -1f)]
        [TestCase(float.NaN, 0f, 0f)]
        [TestCase(float.PositiveInfinity, 0f, 0f)]
        public void StartRotationRejectsUnsupportedAxesOrNonFiniteAngle(float z, float x, float y)
        {
            var serialized = new SerializedObject(_authoring);
            serialized.FindProperty("render").FindPropertyRelative("startRotation").vector3Value = new Vector3(x, y, z);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            StringAssert.Contains("Program Backend", Assert.Throws<InvalidOperationException>(() =>
                FluxParticleBuildPipeline.Build(_authoring)).Message);
        }

        [Test]
        public void SphereRejectsInvalidRadius()
        {
            SetShape(FluxParticleShapeType.Sphere, -1);
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(_authoring));
            SetShape(FluxParticleShapeType.Sphere, float.NaN);
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(_authoring));
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
            var generated = FluxParticleBuildPipeline.Build(_authoring).Shaders;
            var steps = generated.Passes.Select((pass, index) => Import("Step" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => Import("Clear" + index, source)).ToArray();

            using (var session = new FluxParticleProgramGpuSession(generated, steps, clears, 2, 1))
            {
                session.SetVector("spawn.position", Vector4.zero);
                session.SetFloat("spawn.speed", 0);
                session.SetFloat("shape.radius", 2.5f);
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

            var generated = FluxParticleBuildPipeline.Build(_authoring).Shaders;
            var steps = generated.Passes.Select((pass, index) => Import("DirectionStep" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => Import("DirectionClear" + index, source)).ToArray();
            using (var session = new FluxParticleProgramGpuSession(generated, steps, clears, 2, 1))
            {
                session.SetVector("spawn.position", Vector4.zero);
                if (speedMode == FluxParticleStartSpeedMode.Constant) session.SetFloat("spawn.speed", 2f);
                else session.SetVector("spawn.speed-range", new Vector4(1, 3, 0, 0));
                if (shape != FluxParticleShapeType.Point) session.SetFloat("shape.radius", 2.5f);
                if (shape == FluxParticleShapeType.Cone) session.SetFloat("shape.angle", 30f);
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
            Assert.IsTrue(generated.Compilation.StateLayout.TryGetBinding("particle.velocity", out var binding));
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
            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            var generated = compiled.Shaders;
            var steps = generated.Passes.Select((pass, index) => Import("VisualStep" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => Import("VisualClear" + index, source)).ToArray();
            var renderShader = Import("VisualRender", RenderEvaluationShader(generated));
            using (var session = new FluxParticleProgramGpuSession(generated, steps, clears, 2, 1))
            {
                session.SetVector("spawn.position", Vector4.zero);
                session.SetFloat("spawn.speed", 0);
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
            var baseline = FluxParticleBuildPipeline.Build(_authoring);
            SetRandomVisualModes(true, true);
            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, compiled.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.AreEqual(baseline.Shaders.Passes.Count, compiled.Shaders.Passes.Count);
            Assert.IsTrue(compiled.Shaders.Compilation.StateLayout.TryGetBinding("particle.spawn-key", out _));
            Assert.IsFalse(compiled.Shaders.Compilation.StateLayout.TryGetBinding("render.size-range", out _));
            var shader = compiled.RendererSource;
            StringAssert.Contains("RandomStartSize.hlsl", shader);
            var provider = compiled.Shaders.Compilation.Providers["particle.start-size"];
            CollectionAssert.Contains(provider.Inputs, "particle.spawn-key");
            StringAssert.Contains("59u + 43u", File.ReadAllText(provider.HlslPath));
        }

        [Test]
        public void GpuRandomSizeBillboardRendersStablePerParticleWidths()
        {
            RequireVisualGpu();
            SetRandomVisualModes(false, true);
            var compiled = FluxParticleBuildPipeline.Build(_authoring);
            var generated = compiled.Shaders;
            var steps = generated.Passes.Select((pass, index) => Import("SizeStep" + index, pass.Source)).ToArray();
            var clears = generated.ClearSources.Select((source, index) => Import("SizeClear" + index, source)).ToArray();
            var billboard = Import("SizeBillboard", compiled.RendererSource);
            using (var session = new FluxParticleProgramGpuSession(generated, steps, clears, 2, 1))
            {
                session.SetVector("spawn.position", Vector4.zero);
                session.SetFloat("spawn.speed", 0);
                session.SetVector("lifetime.range", new Vector4(10, 10, 0, 0));
                session.SetVector("color.start", Vector4.one);
                session.SetVector("color.end", Vector4.one);
                session.SetVector("render.size-range", new Vector4(0.5f, 2f, 0, 0));
                session.SetUniformFloat("step.delta-time", 0);
                session.SetSpawnInput("spawn.key", Texture(new Color(123, 0, 0, 0), new Color(456, 0, 0, 0)));
                var mask = Texture(new Color(1, 0, 0, 0), new Color(1, 0, 0, 0));
                session.Step(mask);
                var material = new Material(billboard);
                _objects.Add(material);
                material.SetTexture("_MainTex", Texture2D.whiteTexture);
                material.SetVector("_MainTex_ST", new Vector4(1, 1, 0, 0));
                material.SetVector("_FluxStateSize", new Vector4(2, 1, 2, 0));
                material.SetFloat("_SrcBlend", 1);
                material.SetFloat("_DstBlend", 0);
                material.SetFloat("_ZWrite", 0);
                var first = new int[2];
                for (var i = 0; i < 2; i++) first[i] = DrawBillboardWidth(session, material, i);
                foreach (var width in first) Assert.That(width, Is.InRange(16, 132));
                Assert.AreNotEqual(first[0], first[1], "Different SpawnKeys should produce different visible sizes.");

                session.SetUniformFloat("step.delta-time", 1f);
                session.Step();
                for (var i = 0; i < 2; i++) Assert.AreEqual(first[i], DrawBillboardWidth(session, material, i));
                session.Clear();
                session.SetUniformFloat("step.delta-time", 0);
                session.Step(mask);
                for (var i = 0; i < 2; i++) Assert.AreEqual(first[i], DrawBillboardWidth(session, material, i));
            }
        }

        static int DrawBillboardWidth(FluxParticleProgramGpuSession session, Material material, int particleIndex)
        {
            var mesh = new Mesh();
            var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            target.filterMode = FilterMode.Point;
            target.Create();
            var pixels = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
            var previous = RenderTexture.active;
            var cameraObject = new GameObject("FluxFX Billboard GPU Test Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var commands = new CommandBuffer { name = "FluxFX Billboard GPU Test" };
            try
            {
                mesh.vertices = new[] {
                    new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0),
                    new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
                mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                mesh.uv2 = Enumerable.Repeat(new Vector2(particleIndex, 0), 4).ToArray();
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };

                session.BindRender(material);
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = 2;
                camera.aspect = 1;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 20;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.transform.rotation = Quaternion.identity;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.cullingMask = 0;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.targetTexture = target;
                commands.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);
                camera.AddCommandBuffer(CameraEvent.AfterEverything, commands);
                camera.Render();

                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                pixels.Apply();
                var row = pixels.GetPixels(0, 128, 256, 1);
                var left = -1;
                var right = -1;
                for (var x = 0; x < row.Length; x++)
                {
                    if (row[x].maxColorComponent < 0.2f) continue;
                    if (left < 0) left = x;
                    right = x;
                }
                Assert.GreaterOrEqual(left, 0, "Billboard produced no visible pixels.");
                return right - left + 1;
            }
            finally
            {
                camera.RemoveCommandBuffer(CameraEvent.AfterEverything, commands);
                camera.targetTexture = null;
                commands.Release();
                RenderTexture.active = previous;
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(pixels);
                target.Release();
                Object.DestroyImmediate(target);
            }
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
            Assert.IsTrue(generated.Compilation.StateLayout.TryGetBinding("particle.position", out var binding));
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
