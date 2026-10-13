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
