using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleMeshRenderTests
    {
        GameObject root;
        FluxParticleAuthoring authoring;
        Mesh source;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Mesh Particle Test");
            authoring = root.AddComponent<FluxParticleAuthoring>();
            source = new Mesh
            {
                vertices = new[] { new Vector3(1, 2, 3), new Vector3(1, 2, 3), new Vector3(1, 2, 3) },
                uv = new[] { Vector2.zero, new Vector2(2, 0), new Vector2(0, 2) },
                triangles = new[] { 0, 1, 2 }
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(root);
        }

        void Configure(FluxParticleSimulationSpace space)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("render.mode").intValue = (int)FluxParticleRenderMode.Mesh;
            serialized.FindProperty("render.mesh").objectReferenceValue = source;
            serialized.FindProperty("render.startRotation").vector3Value = new Vector3(20, 40, 60);
            serialized.FindProperty("render.startSize").floatValue = 2;
            serialized.FindProperty("initialVelocity.speed").floatValue = 0;
            serialized.FindProperty("lifetime.lifetime").floatValue = 4;
            serialized.FindProperty("emission.rate").floatValue = 0;
            var bursts = serialized.FindProperty("emission.bursts");
            bursts.arraySize = 1;
            bursts.GetArrayElementAtIndex(0).FindPropertyRelative("time").floatValue = 0;
            bursts.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue = 4;
            serialized.FindProperty("simulationSpace").intValue = (int)space;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void MeshRequiresReadableTriangleGeometry()
        {
            Configure(FluxParticleSimulationSpace.Local);
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("render.mesh").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
            serialized.FindProperty("render.mesh").objectReferenceValue = source;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            source.SetIndices(new[] { 0, 1 }, MeshTopology.Lines, 0);
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
            source.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0);
            source.UploadMeshData(true);
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
        }

        [TestCase(FluxParticleSimulationSpace.Local, -1)]
        [TestCase(FluxParticleSimulationSpace.World, -1)]
        [TestCase(FluxParticleSimulationSpace.Local, (int)FluxParticleRotationOverLifetimeMode.Linear)]
        [TestCase(FluxParticleSimulationSpace.World, (int)FluxParticleRotationOverLifetimeMode.Linear)]
        [TestCase(FluxParticleSimulationSpace.Local, (int)FluxParticleRotationOverLifetimeMode.Curve)]
        [TestCase(FluxParticleSimulationSpace.World, (int)FluxParticleRotationOverLifetimeMode.Curve)]
        public void MeshVertexUsesSizeRotationAndSimulationSpace(FluxParticleSimulationSpace space, int rotationMode)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            var baseline = FluxParticleBuildPipeline.Build(authoring);
            Configure(space);
            var endRotation = new Vector3(90, -120, 180);
            var curve = AnimationCurve.Linear(0, -0.25f, 1, 1.25f);
            if (rotationMode >= 0)
            {
                var serialized = new SerializedObject(authoring);
                var modules = serialized.FindProperty("modules");
                modules.arraySize = 1;
                modules.GetArrayElementAtIndex(0).managedReferenceValue = new FluxParticleRotationOverLifetimeModule();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();
                var module = serialized.FindProperty("modules").GetArrayElementAtIndex(0);
                module.FindPropertyRelative("mode").intValue = rotationMode;
                module.FindPropertyRelative("endRotation").vector3Value = endRotation;
                module.FindPropertyRelative("curve").animationCurveValue = curve;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            root.transform.SetPositionAndRotation(new Vector3(10, 20, 30), Quaternion.Euler(17, 29, 41));
            root.transform.localScale = new Vector3(2, 3, 4);
            var build = FluxParticleBuildPipeline.Build(authoring);
            Assert.AreEqual(baseline.Shaders.Passes.Count, build.Shaders.Passes.Count);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, build.Shaders.Compilation.StateLayout.Pages.Count);
            Assert.IsFalse(build.Shaders.Compilation.StateLayout.TryGetBinding("render.rotation", out _));

            Shader shader = null;
            Material material = null;
            var target = new RenderTexture(2, 2, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(2, 2, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                using (var preview = new FluxParticleEditorPreview(build, authoring, 4))
                {
                    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    var mesh = (Mesh)typeof(FluxParticleEditorPreview).GetField("mesh", flags).GetValue(preview);
                    Assert.AreEqual(12, mesh.vertexCount);
                    CollectionAssert.AreEqual(Enumerable.Range(0, 12).ToArray(), mesh.triangles);
                    for (var vertex = 0; vertex < 12; vertex++)
                    {
                        Assert.AreEqual(new Vector3(1, 2, 3), mesh.vertices[vertex]);
                        Assert.AreEqual(new Vector2(vertex / 3, 0), mesh.uv2[vertex]);
                    }
                    preview.Step(0.25f);
                    if (rotationMode >= 0)
                    {
                        preview.Step(1);
                    }
                    var probe = build.RendererSource.Replace("output.vertex = UnityWorldToClipPos(vertex);",
                        "output.vertex = float4(input.uv * 2.0 - 1.0, 0, 1);")
                        .Replace("output.color = render.valueRenderColor;", "output.color = float4(vertex, 1);");
                    shader = ShaderUtil.CreateShaderAsset(probe, true);
                    Assert.IsFalse(ShaderUtil.ShaderHasError(shader),
                        string.Join(Environment.NewLine, ShaderUtil.GetShaderMessages(shader).Select(message => message.message)));
                    material = new Material(shader);
                    material.SetVector("_FluxStateSize", new Vector4(2, 2, 4, 0));
                    var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview).GetField("session", flags).GetValue(preview);
                    target.Create();
                    for (var phase = 0; phase < (rotationMode >= 0 ? 3 : 1); phase++)
                    {
                        if (phase == 1)
                        {
                            endRotation = new Vector3(-45, 60, 135);
                            session.SetVector("rotation.end", endRotation);
                        }
                        if (phase == 2)
                        {
                            preview.Restart();
                            preview.Step(0.25f);
                        }
                        session.BindRender(material);
                        RenderTexture.active = target;
                        GL.Clear(false, true, Color.clear);
                        Assert.IsTrue(material.SetPass(0));
                        Graphics.DrawMeshNow(mesh, root.transform.localToWorldMatrix);
                        readback.ReadPixels(new Rect(0, 0, 2, 2), 0, 0);
                        readback.Apply();
                        var actual = readback.GetPixel(0, 0);
                        var rotation = authoring.Render.StartRotation;
                        if (rotationMode >= 0 && phase != 2)
                        {
                            rotation += endRotation * (rotationMode == (int)FluxParticleRotationOverLifetimeMode.Curve
                                ? curve.Evaluate(0.25f) : 0.25f);
                        }
                        else if (rotationMode == (int)FluxParticleRotationOverLifetimeMode.Curve)
                        {
                            rotation += endRotation * curve.Evaluate(0);
                        }
                        var local = Quaternion.Euler(rotation) * new Vector3(1, 2, 3) * 2;
                        var expected = space == FluxParticleSimulationSpace.Local
                            ? root.transform.localToWorldMatrix.MultiplyPoint3x4(local) : root.transform.position + local;
                        for (var component = 0; component < 3; component++)
                        {
                            Assert.AreEqual(expected[component], actual[component], 0.02f, "Phase " + phase + " component " + component);
                        }
                    }
                }
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback);
                if (material != null) Object.DestroyImmediate(material);
                if (shader != null) Object.DestroyImmediate(shader);
            }
        }
    }
}