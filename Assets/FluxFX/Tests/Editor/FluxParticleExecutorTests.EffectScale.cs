using System;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void EffectScaleRejectsInvalidValuesWithoutChangingSettings(float value)
        {
            Assert.AreEqual(1, authoring.EffectScale);
            Assert.IsFalse(authoring.SetEffectScale(value));
            Assert.AreEqual(1, authoring.EffectScale);
            using var settings = new SerializedObject(authoring);
            settings.FindProperty("effectScale").floatValue = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
        }

        [Test]
        public void EffectScaleWorldRequiresOneAndLocalInversePreservesTransform()
        {
            root.transform.SetPositionAndRotation(new Vector3(4, 5, 6), Quaternion.Euler(20, 30, 40));
            root.transform.localScale = new Vector3(2, 3, 4);
            var matrix = root.transform.localToWorldMatrix;
            var position = new Vector3(0.2f, -0.4f, 0.6f);
            Assert.IsTrue(authoring.SetEffectScale(200));
            Assert.Less(Vector3.Distance(authoring.ReferenceToWorld(position), matrix.MultiplyPoint3x4(position * 200)), 0.0001f);
            Assert.Less(Vector3.Distance(authoring.WorldToReference(authoring.ReferenceToWorld(position)), position), 0.0001f);
            Assert.AreEqual(matrix, root.transform.localToWorldMatrix);
            Assert.AreEqual(200, authoring.ReferenceLengthToDisplay(1));
            Assert.AreEqual(600, FluxParticleCoordinates.DistanceToWorld(1, 200, 3, FluxParticleDistanceMode.FollowEffect));
            Assert.AreEqual(1, FluxParticleCoordinates.DistanceToWorld(1, 200, 3, FluxParticleDistanceMode.FixedWorld));
            using var settings = new SerializedObject(authoring);
            settings.FindProperty("simulationSpace").intValue = (int)FluxParticleSimulationSpace.World;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
            Assert.IsFalse(authoring.SetEffectScale(2));
            Assert.IsTrue(authoring.SetEffectScale(1));
            Assert.AreEqual(position, authoring.ReferenceToWorld(position));
            Assert.DoesNotThrow(() => FluxParticleBuildPipeline.Build(authoring));
        }

        [TestCase(0.5f)]
        [TestCase(200f)]
        public void EffectScaleKeepsGpuStateAndScalesTrailHistoryOnce(float scale)
        {
            RequireGraphics();
            SetModules(CreateTrails(4, 4, 0.3f));
            var baseline = FluxParticleBuildPipeline.Build(authoring);
            var original = CreateRuntime(baseline);
            Assert.IsTrue(original.Initialize(4));
            original.Step(0, 0, 1, 7);
            for (var i = 0; i < 3; i++) original.Step(0.11f, 0, 0, 0);
            var position = ReadValue(original, baseline, "particle.position", 0);
            var velocity = ReadValue(original, baseline, "particle.velocity", 0);
            var age = ReadValue(original, baseline, "particle.age", 0);
            var history = new Color[4];
            for (var i = 0; i < 4; i++) history[i] = ReadTexture(original.GetTrailPoint(i), 0);
            Assert.IsTrue(authoring.SetEffectScale(scale));
            var build = FluxParticleBuildPipeline.Build(authoring);
            Assert.AreEqual(baseline.Shaders.Passes.Count, build.Shaders.Passes.Count);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, build.Shaders.Compilation.StateLayout.Pages.Count);
            var scaled = CreateRuntime(build);
            Assert.IsFalse(scaled.SetFloat("system.effect-scale", 0));
            Assert.IsFalse(scaled.SetFloat("system.effect-scale", 3));
            Assert.AreEqual(scale, scaled.GetComponentInParent<FluxParticleSystem>().EffectScale);
            Assert.IsTrue(scaled.Initialize(4));
            scaled.Step(0, 0, 1, 7);
            for (var i = 0; i < 3; i++) scaled.Step(0.11f, 0, 0, 0);
            AssertVector(position, ReadValue(scaled, build, "particle.position", 0));
            AssertVector(velocity, ReadValue(scaled, build, "particle.velocity", 0));
            AssertVector(age, ReadValue(scaled, build, "particle.age", 0));
            for (var i = 0; i < 4; i++)
            {
                var a = history[i];
                var b = ReadTexture(scaled.GetTrailPoint(i), 0);
                AssertVector(new Vector4(a.r * scale, a.g * scale, a.b * scale, a.a), b, 0.0001f);
            }
            var materials = (Material[])scaled.GetProgramVariable("trailMaterials");
            Assert.AreEqual(scale, materials[0].GetFloat("_FluxEffectScale"));
            Assert.IsFalse(scaled.SetFloat("system.effect-scale", 3), "Scale is static during runtime");
        }

        [Test]
        public void EffectScaleBoundsIncludeCenterAndDistanceFadeHasNoExtraState()
        {
            using var settings = new SerializedObject(authoring);
            settings.FindProperty("render.cullingBounds").boundsValue = new Bounds(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
            settings.ApplyModifiedPropertiesWithoutUndo();
            authoring.SetEffectScale(200);
            Assert.AreEqual(new Vector3(200, 400, 600), authoring.DisplayCullingBounds.center);
            Assert.AreEqual(new Vector3(800, 1000, 1200), authoring.DisplayCullingBounds.size);
            var baseline = FluxParticleBuildPipeline.Build(authoring);
            SetModules(new FluxParticleDistanceFadeModule());
            var build = FluxParticleBuildPipeline.Build(authoring);
            Assert.AreEqual(baseline.Shaders.Passes.Count, build.Shaders.Passes.Count);
            Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, build.Shaders.Compilation.StateLayout.Pages.Count);
            var shader = ShaderUtil.CreateShaderAsset(build.RendererSource, true);
            try { Assert.IsFalse(ShaderUtil.ShaderHasError(shader)); }
            finally { UnityEngine.Object.DestroyImmediate(shader); }
        }
    }
}
