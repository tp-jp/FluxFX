using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleModuleContributionTests
    {
        [TestCase(FluxParticleSizeOverLifetimeMode.Linear)]
        [TestCase(FluxParticleSizeOverLifetimeMode.Curve)]
        public void BindingSnapshotKeepsLocalSlotsAndIsIndependentOfLaterAuthoringChanges(FluxParticleSizeOverLifetimeMode mode)
        {
            var root = new GameObject("Module Contribution Test");
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var serialized = new SerializedObject(authoring);
                var modules = serialized.FindProperty("modules");
                modules.arraySize = 5;
                modules.GetArrayElementAtIndex(0).managedReferenceValue = new FluxParticleGravityModule();
                modules.GetArrayElementAtIndex(1).managedReferenceValue = new FluxParticleSizeOverLifetimeModule();
                modules.GetArrayElementAtIndex(2).managedReferenceValue = new FluxParticleDragModule();
                modules.GetArrayElementAtIndex(3).managedReferenceValue = new FluxParticleColorOverLifetimeModule();
                modules.GetArrayElementAtIndex(4).managedReferenceValue = new FluxParticleRotationOverLifetimeModule();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();
                var size = serialized.FindProperty("modules").GetArrayElementAtIndex(1);
                size.FindPropertyRelative("mode").intValue = (int)mode;
                size.FindPropertyRelative("enabled").boolValue = false;
                var color = serialized.FindProperty("modules").GetArrayElementAtIndex(3);
                color.FindPropertyRelative("mode").intValue = (int)(mode == FluxParticleSizeOverLifetimeMode.Linear
                    ? FluxParticleColorOverLifetimeMode.Linear : FluxParticleColorOverLifetimeMode.Gradient);
                color.FindPropertyRelative("enabled").boolValue = false;
                var rotation = serialized.FindProperty("modules").GetArrayElementAtIndex(4);
                rotation.FindPropertyRelative("mode").intValue = (int)(mode == FluxParticleSizeOverLifetimeMode.Linear
                    ? FluxParticleRotationOverLifetimeMode.Linear : FluxParticleRotationOverLifetimeMode.Curve);
                rotation.FindPropertyRelative("enabled").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var build = FluxParticleBuildPipeline.Build(authoring);
                var gravity = build.ModuleBindings.Single(binding => binding.ModuleId == "fluxfx.gravity");
                var binding = build.ModuleBindings.Single(value => value.ModuleId == "tplab.fluxfx.size-over-lifetime");
                var drag = build.ModuleBindings.Single(value => value.ModuleId == "fluxfx.drag");
                var colorBinding = build.ModuleBindings.Single(value => value.ModuleId == "fluxfx.color-over-lifetime");
                var rotationBinding = build.ModuleBindings.Single(value => value.ModuleId == "tplab.fluxfx.rotation-over-lifetime");
                Assert.AreEqual(1, colorBinding.Parameters.Count);
                Assert.AreEqual(mode == FluxParticleSizeOverLifetimeMode.Linear ? "color.end" : null, colorBinding.Parameters[0]?.Id);
                Assert.AreEqual(mode == FluxParticleSizeOverLifetimeMode.Linear, build.InitialValues.ContainsKey("color.end"));
                Assert.AreEqual(mode == FluxParticleSizeOverLifetimeMode.Linear, build.Shaders.Compilation.Parameters.ContainsKey("color.end"));
                Assert.AreEqual("rotation.end", rotationBinding.Parameters.Single().Id);
                Assert.AreEqual(0, build.InitialValues[colorBinding.EnabledParameterId].x);
                Assert.AreEqual(0, build.InitialValues[rotationBinding.EnabledParameterId].x);
                Assert.AreEqual("gravity", gravity.Parameters.Single().Id);
                Assert.AreEqual("drag", drag.Parameters.Single().Id);
                Assert.AreEqual("size.enabled", binding.EnabledParameterId);
                Assert.AreEqual(0, build.InitialValues[binding.EnabledParameterId].x);
                Assert.AreEqual(1, binding.Parameters.Count);
                Assert.AreEqual(mode == FluxParticleSizeOverLifetimeMode.Linear ? "size.end" : null, binding.Parameters[0]?.Id);
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<FluxParticleParameterDefinition>)binding.Parameters)[0] = gravity.Parameters[0]);

                serialized.Update();
                serialized.FindProperty("modules").arraySize = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.AreEqual(5, build.ModuleBindings.Count);
                Assert.AreEqual(mode == FluxParticleSizeOverLifetimeMode.Linear ? "size.end" : null, binding.Parameters[0]?.Id);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HandleSlotsRequireExplicitAssignmentAndCannotBeOverwritten()
        {
            var context = new FluxParticleProgramContribution(new FluxParticleDefinitionBuilder());
            var contribution = new FluxParticleModuleContribution(context, "test", "test.enabled", false, 2);
            contribution.Parameter(0, "test.value", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live, Vector4.one);
            Assert.Throws<InvalidOperationException>(() => contribution.Complete());
            Assert.Throws<InvalidOperationException>(() => contribution.UnavailableParameter(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => contribution.UnavailableParameter(2));
            Assert.IsEmpty(context.ModuleBindings);
            contribution.UnavailableParameter(1);
            contribution.Complete();
            Assert.IsNull(context.ModuleBindings["test"].Parameters[1]);
            Assert.Throws<InvalidOperationException>(() => contribution.Complete());
            Assert.Throws<InvalidOperationException>(() => contribution.UnavailableParameter(1));
        }
    }
}
