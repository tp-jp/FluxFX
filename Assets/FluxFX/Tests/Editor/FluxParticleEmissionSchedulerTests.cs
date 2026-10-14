using System;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Parameters;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleEmissionSchedulerTests
    {
        static long Emit(double[] state, float deltaTime, float rate, float duration, bool loop,
            float[] times = null, int[] counts = null)
        {
            return FluxParticleEmissionScheduler.Emit(state, deltaTime, rate, duration, loop,
                times ?? Array.Empty<float>(), counts ?? Array.Empty<int>());
        }

        [Test]
        public void DelayBurstBoundariesAndCompletionAreEvaluatedOnce()
        {
            var state = FluxParticleEmissionScheduler.Create(0.5f);
            var times = new[] { 0f, 0.25f, 0.5f };
            var counts = new[] { 3, 4, 5 };
            Assert.AreEqual(0, Emit(state, 0.5f, 4, 1, false, times, counts));
            Assert.AreEqual(8, Emit(state, 0.25f, 4, 1, false, times, counts));
            Assert.AreEqual(6, Emit(state, 0.25f, 4, 1, false, times, counts));
            Assert.AreEqual(2, Emit(state, 0.5f, 4, 1, false, times, counts));
            Assert.AreEqual(0, Emit(state, 10, 4, 1, false, times, counts));
        }

        [Test]
        public void CrossingMultipleCyclesMatchesPartitionedSteps()
        {
            var times = new[] { 0f, 0.25f, 0.5f, 0.75f };
            var counts = new[] { 2, 3, 5, 99 };
            var single = FluxParticleEmissionScheduler.Create(0);
            Assert.AreEqual(25, Emit(single, 1.25f, 0, 0.5f, true, times, counts));
            var partitioned = FluxParticleEmissionScheduler.Create(0);
            long sum = 0;
            for (var i = 0; i < 5; i++)
            {
                sum += Emit(partitioned, 0.25f, 0, 0.5f, true, times, counts);
            }
            Assert.AreEqual(25, sum);
            Assert.AreEqual(5, Emit(single, 0.25f, 0, 0.5f, true, times, counts));
            Assert.AreEqual(5, Emit(partitioned, 0.25f, 0, 0.5f, true, times, counts));
        }

        [Test]
        public void FractionSurvivesLoopAndLiveRateChangeWhileZeroStepDoesNotAdvance()
        {
            var state = FluxParticleEmissionScheduler.Create(0);
            Assert.AreEqual(0, Emit(state, 0.125f, 3, 0.25f, true));
            var paused = (double[])state.Clone();
            Assert.AreEqual(0, Emit(state, 0, 100, 0.25f, true, new[] { 0f }, new[] { 100 }));
            CollectionAssert.AreEqual(paused, state);
            long total = 0;
            for (var i = 0; i < 7; i++)
            {
                total += Emit(state, 0.125f, 3, 0.25f, true);
            }
            Assert.AreEqual(3, total);
            FluxParticleEmissionScheduler.Reset(state, 0);
            Assert.AreEqual(0, Emit(state, 0.5f, 1, 1, true));
            Assert.AreEqual(1, Emit(state, 0.25f, 2, 1, true));
        }

        [Test]
        public void DuplicateBurstTimesAccumulateAndLargeCountsDoNotOverflowInt()
        {
            var state = FluxParticleEmissionScheduler.Create(0);
            var count = Emit(state, 0.25f, 0, 1, false, new[] { 0.25f, 0.25f },
                new[] { int.MaxValue, int.MaxValue });
            Assert.AreEqual(2L * int.MaxValue, count);
            Assert.AreEqual(0, Emit(state, 0.25f, 0, 1, false, new[] { 0.25f, 0.25f },
                new[] { int.MaxValue, int.MaxValue }));
        }

        [Test]
        public void SmallRateRemainsAccurateAcrossManySteps()
        {
            var state = FluxParticleEmissionScheduler.Create(0);
            const int steps = 100000;
            const float rate = 0.1f;
            long count = 0;
            for (var i = 0; i < steps; i++)
            {
                count += Emit(state, 0.125f, rate, 0.5f, true);
            }
            Assert.AreEqual((long)Math.Floor(steps * 0.125 * rate), count);
        }

        [Test]
        public void ResetReplaysDelayAndBursts()
        {
            var state = FluxParticleEmissionScheduler.Create(0.25f);
            var times = new[] { 0f, 0.25f };
            var counts = new[] { 2, 3 };
            var first = Emit(state, 0.5f, 2, 1, false, times, counts);
            Emit(state, 1, 2, 1, false, times, counts);
            FluxParticleEmissionScheduler.Reset(state, 0.25f);
            Assert.AreEqual(first, Emit(state, 0.5f, 2, 1, false, times, counts));
        }

        [Test]
        public void RuntimeCursorWrapPreservesLargeIntegerCounts()
        {
            var method = typeof(FluxParticleEmitter).GetMethod("Wrap", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var period in new[] { 7, 1048576, int.MaxValue })
            {
                foreach (var count in new[] { 0L, 1L, 4294967294L, long.MaxValue })
                {
                    var start = period - 1;
                    var expected = (int)((start + count % period) % period);
                    Assert.AreEqual(expected, method.Invoke(null, new object[] { start, count, period }));
                }
            }
        }

        [Test]
        public void RuntimeEmitterUsesSharedScheduleAndKeepsRingAndSeedAcrossRestart()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var emitter = root.GetComponentInChildren<FluxParticleEmitter>(true);
                var parameters = new FluxParticleEmissionParameters();
                parameters.Add(4d);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var type = typeof(FluxParticleEmitter);
                var times = new[] { 0f, 0.25f, 0.5f };
                var counts = new[] { 2, 3, 5 };
                type.GetMethod("Initialize", flags).Invoke(emitter,
                    new object[] { parameters, times, counts, 4, 0.5f, 0.5f, true });
                var reference = FluxParticleEmissionScheduler.Create(0.5f);
                var cursor = 0;
                var seed = 0;
                foreach (var delta in new[] { 0.5f, 0.25f, 0.25f, 0.25f, 0.25f })
                {
                    var requested = Emit(reference, delta, 4, 0.5f, true, times, counts);
                    type.GetMethod("UpdateEmission", flags).Invoke(emitter, new object[] { delta });
                    var active = (int)Math.Min(4, requested);
                    var skipped = requested - active;
                    Assert.AreEqual(active, emitter.SpawnCount);
                    Assert.AreEqual((cursor + skipped) % 4, emitter.SpawnStart);
                    Assert.AreEqual((seed + skipped) % 1048576, emitter.SpawnSeedStart);
                    cursor = (int)((cursor + requested) % 4);
                    seed = (int)((seed + requested) % 1048576);
                }
                type.GetMethod("ResetPlayback", flags).Invoke(emitter, null);
                type.GetMethod("UpdateEmission", flags).Invoke(emitter, new object[] { 0.5f });
                Assert.AreEqual(0, emitter.SpawnCount);
                Assert.AreEqual(cursor, emitter.SpawnStart);
                Assert.AreEqual(seed, emitter.SpawnSeedStart);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(0.005f)]
        [TestCase(0f)]
        [TestCase(float.NaN)]
        public void AuthoringRejectsDurationBelowRuntimeMinimum(float duration)
        {
            var root = new GameObject("Emission Validation Test");
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var serialized = new SerializedObject(authoring);
                serialized.FindProperty("playback.duration").floatValue = duration;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}