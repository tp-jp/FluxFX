using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.Udon;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [TestCase(0, TestName = "BuildActiveHierarchyConnectsBurstAndModules")]
        [TestCase(1, TestName = "BuildInactiveSystemConnectsBurstAndModules")]
        [TestCase(2, TestName = "BuildInactiveParentConnectsBurstAndModules")]
        public void BuildHierarchyConnectsBurstAndModules(int inactiveLevel)
        {
            RequireGraphics();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(root.transform, false);
            authoring = instance.GetComponent<FluxParticleAuthoring>();
            var system = instance.GetComponent<FluxParticleSystem>();
            var runtime = instance.GetComponentInChildren<FluxParticleExecutor>(true);
            var emitter = instance.GetComponentInChildren<FluxParticleEmitter>(true);
            runtimes.Add(runtime);

            var settings = new SerializedObject(authoring);
            settings.FindProperty("particleCount").intValue = 16;
            settings.FindProperty("playOnAwake").boolValue = true;
            settings.FindProperty("emission.rate").floatValue = 0;
            var bursts = settings.FindProperty("emission.bursts");
            bursts.arraySize = 1;
            bursts.GetArrayElementAtIndex(0).FindPropertyRelative("time").floatValue = 0;
            bursts.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue = 8;
            settings.ApplyModifiedPropertiesWithoutUndo();
            SetModules(new FluxParticleGravityModule());
            system.Modules.SetProgramVariable("executor", null);
            system.Modules.SetProgramVariable("bindingIds", new string[0]);
            emitter.SetBursts(new float[0], new int[0]);
            emitter.SetRate(99);

            if (inactiveLevel == 1) instance.SetActive(false);
            if (inactiveLevel == 2) root.SetActive(false);
            Assert.AreEqual(inactiveLevel == 0, instance.activeInHierarchy);
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            FluxParticleProgramBuild.Apply(authoring, system, generationSession);

            Assert.AreEqual(0, emitter.Rate);
            Assert.AreEqual(1, emitter.BurstCount);
            Assert.AreEqual(0, emitter.GetBurstTime(0));
            Assert.AreEqual(8, emitter.GetBurstCount(0));
            Assert.AreSame(runtime, system.Modules.Executor);
            var gravity = system.Modules.Gravity;
            Assert.IsNotNull(gravity);
            Assert.IsTrue(gravity.HasEnabled());
            Assert.IsTrue(gravity.SetGravity(new Vector3(0, -2, 0)));
            Assert.AreEqual(new Vector3(0, -2, 0), gravity.GetGravity());
            Assert.IsTrue(gravity.SetEnabled(false));
            Assert.IsFalse(gravity.GetEnabled());
            Assert.IsTrue(gravity.SetEnabled(true));
            Assert.IsTrue(gravity.GetEnabled());

            instance.SetActive(true);
            root.SetActive(true);
            foreach (var kernel in runtime.GetComponentsInChildren<FluxKernel>(true))
            {
                var material = new Material(kernel.Shader);
                kernel.SetProgramVariable("material", material);
                kernelMaterials.Add(material);
            }

            // EditMode drives the C# runtime directly; Play mode and the Udon VM are separate checks.
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Assert.IsTrue(runtime.Initialize(16));
            typeof(FluxParticleEmitter).GetMethod("Initialize", flags).Invoke(emitter, new object[] { 16 });
            typeof(FluxParticleSystem).GetField("_programReady", flags).SetValue(system, true);
            system.Play();
            Assert.IsTrue(system.IsEmitting);
            typeof(FluxParticleSystem).GetMethod("Simulate", flags).Invoke(system, new object[] { 0.125f });
            Assert.AreEqual(8, emitter.SpawnCount);
            var alive = 0;
            for (var slot = 0; slot < 16; slot++)
            {
                if (ReadValue(runtime, compiled, "particle.age", slot).x >= 0) alive++;
            }
            Assert.AreEqual(8, alive, "Rate=0 must still emit the configured initial Burst after activation.");
        }
    }
}
