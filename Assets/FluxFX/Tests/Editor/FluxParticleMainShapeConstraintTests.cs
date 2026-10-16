using System;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void MainShapeHandlesAndDirectUpdatesShareConstraints(bool random, bool mesh)
        {
            RequireGraphics();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            if (random)
            {
                typeof(InitialVelocitySettings).GetField("mode", flags).SetValue(authoring.InitialVelocity, FluxParticleStartSpeedMode.RandomBetweenTwoConstants);
                typeof(TpLab.Flux.FX.Scripts.RenderSettings).GetField("startSizeMode", flags).SetValue(authoring.Render, FluxParticleStartSizeMode.RandomBetweenTwoConstants);
                typeof(TpLab.Flux.FX.Scripts.RenderSettings).GetField("startColorMode", flags).SetValue(authoring.Render, FluxParticleStartColorMode.RandomBetweenTwoConstants);
                typeof(TpLab.Flux.FX.Scripts.RenderSettings).GetField("startRotationMode", flags).SetValue(authoring.Render, FluxParticleStartRotationMode.RandomBetweenTwoConstants);
            }
            if (mesh)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                typeof(TpLab.Flux.FX.Scripts.RenderSettings).GetField("mode", flags).SetValue(authoring.Render, FluxParticleRenderMode.Mesh);
                typeof(TpLab.Flux.FX.Scripts.RenderSettings).GetField("mesh", flags).SetValue(authoring.Render, cube.GetComponent<MeshFilter>().sharedMesh);
                UnityEngine.Object.DestroyImmediate(cube);
            }
            typeof(ShapeSettings).GetField("type", flags).SetValue(authoring.Shape, random ? FluxParticleShapeType.Box : FluxParticleShapeType.Cone);
            SetModules(new FluxParticleRotationOverLifetimeModule());
            var runtime = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            var system = runtime.GetComponentInParent<FluxParticleSystem>();
            var main = system.Main;
            var shape = system.Shape;
            for (var phase = 0; phase < 2; phase++)
            {
                if (phase == 1)
                {
                    Assert.IsTrue(runtime.Initialize(4));
                    runtime.Step(0, 0, 0, 1);
                }
                var beforeStart = phase == 0;
                CheckMainShapeValue(runtime, "lifetime.range", 2, v => main.SetStartLifetimeRange(v), new Vector4(2, 3), beforeStart);
                CheckMainShapeValue(runtime, "lifetime.range", 2, v => main.SetStartLifetimeRange(v), new Vector4(3, 2), false);
                CheckMainShapeValue(runtime, "lifetime.range", 2, v => main.SetStartLifetimeRange(v), new Vector4(-1, 2), false);
                if (random)
                {
                    CheckMainShapeValue(runtime, "spawn.speed-range", 2, v => main.SetStartSpeedRange(v), new Vector4(0, 2), true);
                    CheckMainShapeValue(runtime, "spawn.speed-range", 2, v => main.SetStartSpeedRange(v), new Vector4(2, 1), false);
                    CheckMainShapeValue(runtime, "render.size-range", 2, v => main.SetStartSizeRange(v), new Vector4(0, 2), beforeStart);
                    CheckMainShapeValue(runtime, "render.size-range", 2, v => main.SetStartSizeRange(v), new Vector4(2, 1), false);
                    CheckMainShapeValue(runtime, "shape.size", 3, v => shape.SetSize(v), Vector4.one, true);
                    CheckMainShapeValue(runtime, "shape.size", 3, v => shape.SetSize(v), new Vector4(1, -1, 1), false);
                    CheckMainShapeValue(runtime, "rotation.start-max", 3, v => main.SetStartRotationRange(Vector3.zero, v), new Vector4(0, 0, -45), beforeStart);
                    CheckMainShapeValue(runtime, "rotation.start-max", 3, v => main.SetStartRotationRange(Vector3.zero, v), Vector4.one, beforeStart && mesh);
                    var previousMin = main.GetStartRotationMin();
                    var previousMax = main.GetStartRotationMax();
                    Assert.IsFalse(main.SetStartRotationRange(new Vector3(0, 0, 15), new Vector3(float.NaN, 0, 0)));
                    Assert.AreEqual(previousMin, main.GetStartRotationMin());
                    Assert.AreEqual(previousMax, main.GetStartRotationMax());
                }
                else
                {
                    CheckMainShapeValue(runtime, "spawn.speed", 1, v => main.SetStartSpeed(v.x), Vector4.zero, true);
                    CheckMainShapeValue(runtime, "spawn.speed", 1, v => main.SetStartSpeed(v.x), -Vector4.one, false);
                    CheckMainShapeValue(runtime, "render.size", 1, v => main.SetStartSize(v.x), Vector4.one, beforeStart);
                    CheckMainShapeValue(runtime, "render.size", 1, v => main.SetStartSize(v.x), -Vector4.one, false);
                    CheckMainShapeValue(runtime, "shape.radius", 1, v => shape.SetRadius(v.x), Vector4.zero, true);
                    CheckMainShapeValue(runtime, "shape.radius", 1, v => shape.SetRadius(v.x), -Vector4.one, false);
                    CheckMainShapeValue(runtime, "shape.angle", 1, v => shape.SetAngle(v.x), new Vector4(89, 0), true);
                    CheckMainShapeValue(runtime, "shape.angle", 1, v => shape.SetAngle(v.x), new Vector4(90, 0), false);
                    CheckMainShapeValue(runtime, "shape.angle", 1, v => shape.SetAngle(v.x), new Vector4(-1, 0), false);
                    CheckMainShapeValue(runtime, "rotation.start", 3, v => main.SetStartRotation(v), new Vector4(0, 0, -45), beforeStart);
                    CheckMainShapeValue(runtime, "rotation.start", 3, v => main.SetStartRotation(v), Vector4.one, beforeStart && mesh);
                }
                var rotationId = random ? "rotation.start-max" : "rotation.start";
                Assert.IsFalse(runtime.SetVector(rotationId, new Vector4(float.NaN, 0, 0)));
                if (!mesh) Assert.IsFalse(runtime.SetVector(rotationId, Vector4.one));
                CheckMainShapeValue(runtime, "spawn.position", 3, v => shape.SetPosition(v), new Vector4(-1, 2, -3), true);
                CheckMainShapeValue(runtime, "spawn.position", 3, v => shape.SetPosition(v), new Vector4(float.PositiveInfinity, 0), false);
            }
        }

        static void CheckMainShapeValue(FluxParticleExecutor runtime, string id, int type, Func<Vector4, bool> setter, Vector4 value, bool expected)
        {
            for (var component = type; component < 4; component++)
            {
                value[component] = 0;
            }
            var slot = runtime.ResolveParameter(id, type);
            Assert.GreaterOrEqual(slot, 0, id);
            var previous = runtime.GetParameterValue(slot);
            Assert.AreEqual(expected, setter(value), id + " handle");
            var stored = runtime.GetParameterValue(slot);
            Assert.AreEqual(expected, type == 1 ? runtime.SetFloat(id, value.x) : runtime.SetVector(id, value), id + " ID");
            Assert.AreEqual(expected, runtime.SetParameterValue(slot, type, value), id + " slot");
            Assert.AreEqual(stored, runtime.GetParameterValue(slot), id);
            if (!expected) Assert.AreEqual(previous, stored, id + " unchanged");
        }

        [Test]
        public void OrderedComponentContractsValidateRangesAndBuilderSnapshots()
        {
            var builder = FluxParticleParameter.Vector2("test.range").ApiName("Range").Policy(FluxParticleParameterPolicy.Live);
            var unconstrained = builder.Build();
            var ordered = builder.Minimum(Vector2.zero).OrderedComponents().Build();
            Assert.IsTrue(unconstrained.IsValid(new Vector2(3, 1)));
            Assert.IsFalse(ordered.IsValid(new Vector2(3, 1)));
            Assert.IsFalse(ordered.IsValid(new Vector2(-1, 1)));
            Assert.IsTrue(ordered.IsValid(Vector2.zero));
            Assert.IsTrue(ordered.IsValid(new Vector2(1, 2)));
            Assert.Throws<InvalidOperationException>(() => FluxParticleParameter.Float("test.scalar").ApiName("Scalar")
                .Policy(FluxParticleParameterPolicy.Live).OrderedComponents().Build());
            Assert.Throws<InvalidOperationException>(() => FluxParticleParameter.Vector2("test.impossible").ApiName("Impossible")
                .Policy(FluxParticleParameterPolicy.Live).Range(new Vector2(2, 0), new Vector2(3, 1)).OrderedComponents().Build());
        }
    }
}
