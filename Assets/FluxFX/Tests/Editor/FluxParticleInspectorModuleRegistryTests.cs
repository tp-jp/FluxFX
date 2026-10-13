using System.Collections.Generic;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class FluxParticleInspectorModuleRegistryTests
    {
        FluxParticleInspectorModuleRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new FluxParticleInspectorModuleRegistry();
        }

        [Test]
        public void StandardModulesAreRegistered()
        {
            Assert.IsTrue(_registry.TryGet(typeof(FluxParticleGravityModule), out _));
            Assert.IsTrue(_registry.TryGet(typeof(FluxParticleVelocityOverLifetimeModule), out _));
            Assert.IsTrue(_registry.TryGet(typeof(FluxParticleColorOverLifetimeModule), out _));
            Assert.IsTrue(_registry.TryGet(typeof(FluxParticleTextureSheetAnimationModule), out _));
        }

        [Test]
        public void GravityBelongsToVelocityUpdate()
        {
            var modules = _registry.GetModules(FluxParticleExecutionStage.VelocityUpdate);

            Assert.IsTrue(Contains(modules, typeof(FluxParticleGravityModule)));
            Assert.IsTrue(Contains(modules, typeof(FluxParticleForceModule)));
            Assert.IsTrue(Contains(modules, typeof(FluxParticleNoiseModule)));
            Assert.IsFalse(Contains(modules, typeof(FluxParticleVelocityOverLifetimeModule)));
        }

        [Test]
        public void VelocityOverLifetimeBelongsToPositionUpdate()
        {
            var modules = _registry.GetModules(FluxParticleExecutionStage.PositionUpdate);

            Assert.IsTrue(Contains(modules, typeof(FluxParticleVelocityOverLifetimeModule)));
            Assert.IsFalse(Contains(modules, typeof(FluxParticleGravityModule)));
        }

        [Test]
        public void RenderingModulesBelongToRendering()
        {
            var modules = _registry.GetModules(FluxParticleExecutionStage.Rendering);

            Assert.IsTrue(Contains(modules, typeof(FluxParticleColorOverLifetimeModule)));
            Assert.IsTrue(Contains(modules, typeof(FluxParticleSizeOverLifetimeModule)));
            Assert.IsTrue(Contains(modules, typeof(FluxParticleRotationOverLifetimeModule)));
            Assert.IsTrue(Contains(modules, typeof(FluxParticleTextureSheetAnimationModule)));
        }

        [Test]
        public void CustomGpuModuleIsAutomaticallyRegistered()
        {
            Assert.IsTrue(_registry.TryGet(typeof(TestGpuModule), out var metadata));

            Assert.IsTrue(metadata.IsCustom);
            Assert.AreEqual(FluxParticleExecutionStage.VelocityUpdate, metadata.Stage);

            var modules = _registry.GetModules(FluxParticleExecutionStage.VelocityUpdate);

            Assert.IsTrue(Contains(modules, typeof(TestGpuModule)));
        }

        [Test]
        public void UnsupportedCustomStageIsNotRegistered()
        {
            Assert.IsFalse(_registry.TryGet(typeof(UnsupportedStageGpuModule), out _));
        }

        [Test]
        public void MetadataUsesModuleStage()
        {
            foreach (var metadata in _registry.GetAll())
            {
                var module = (FluxParticleModule)System.Activator.CreateInstance(metadata.ModuleType);

                Assert.AreEqual(module.Stage, metadata.Stage, metadata.ModuleType.FullName);
            }
        }

        [Test]
        public void DisplayOrderIsStable()
        {
            var modules = _registry.GetModules(FluxParticleExecutionStage.VelocityUpdate);

            for (var i = 1; i < modules.Count; i++)
            {
                Assert.GreaterOrEqual(modules[i].DisplayOrder, modules[i - 1].DisplayOrder);
            }
        }

        [Test]
        public void UnknownStageReturnsEmptyList()
        {
            var modules = _registry.GetModules(FluxParticleExecutionStage.None);

            Assert.IsNotNull(modules);
            Assert.IsEmpty(modules);
        }

        [Test]
        public void StageModulesAreCached()
        {
            var first = _registry.GetModules(FluxParticleExecutionStage.VelocityUpdate);
            var second = _registry.GetModules(FluxParticleExecutionStage.VelocityUpdate);

            Assert.AreSame(first, second);
        }

        [Test]
        public void StageModulesCannotBeModified()
        {
            var modules = _registry.GetModules(FluxParticleExecutionStage.VelocityUpdate);
            var list = modules as System.Collections.Generic.IList<FluxParticleInspectorModuleMetadata>;

            Assert.IsNotNull(list);
            Assert.IsTrue(list.IsReadOnly);
            Assert.Throws<System.NotSupportedException>(() => list.Clear());
        }

        [Test]
        public void CustomModuleMetadataHasStableIdentity()
        {
            Assert.IsTrue(_registry.TryGet(typeof(TestGpuModule), out var first));
            Assert.IsTrue(_registry.TryGet(typeof(TestGpuModule), out var second));

            Assert.AreSame(first, second);
            Assert.IsTrue(first.IsCustom);
            Assert.AreEqual(FluxParticleExecutionStage.VelocityUpdate, first.Stage);
            Assert.IsFalse(string.IsNullOrWhiteSpace(first.DisplayName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(first.ExpandedKey));
        }

        [Test]
        public void NoModuleAppearsInMultipleStages()
        {
            var types = new System.Collections.Generic.HashSet<System.Type>();

            foreach (var metadata in _registry.GetAll())
            {
                Assert.IsTrue(types.Add(metadata.ModuleType), $"Duplicate Module: {metadata.ModuleType.FullName}");

                var stageModules = _registry.GetModules(metadata.Stage);
                Assert.IsTrue(Contains(stageModules, metadata.ModuleType));
            }

            Assert.AreEqual(_registry.Count, types.Count);
        }

        [Test]
        public void StandardModulesHaveUniqueExpandedKeys()
        {
            var keys = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            var standardCount = 0;

            foreach (var metadata in _registry.GetAll())
            {
                if (metadata.IsCustom) continue;

                Assert.IsTrue(keys.Add(metadata.ExpandedKey), $"Duplicate ExpandedKey: {metadata.ExpandedKey}");
                standardCount++;
            }

            Assert.AreEqual(11, standardCount);
        }

        [Test]
        public void StandardModuleStageClassificationIsComplete()
        {
            var velocity = _registry.GetModules(FluxParticleExecutionStage.VelocityUpdate);
            var position = _registry.GetModules(FluxParticleExecutionStage.PositionUpdate);
            var rendering = _registry.GetModules(FluxParticleExecutionStage.Rendering);

            Assert.IsTrue(Contains(velocity, typeof(FluxParticleGravityModule)));
            Assert.IsTrue(Contains(velocity, typeof(FluxParticleForceModule)));
            Assert.IsTrue(Contains(velocity, typeof(FluxParticleDragModule)));
            Assert.IsTrue(Contains(velocity, typeof(FluxParticleNoiseModule)));
            Assert.IsTrue(Contains(velocity, typeof(FluxParticleVortexModule)));
            Assert.IsTrue(Contains(velocity, typeof(FluxParticleLimitVelocityModule)));

            Assert.IsTrue(Contains(position, typeof(FluxParticleVelocityOverLifetimeModule)));

            Assert.IsTrue(Contains(rendering, typeof(FluxParticleColorOverLifetimeModule)));
            Assert.IsTrue(Contains(rendering, typeof(FluxParticleSizeOverLifetimeModule)));
            Assert.IsTrue(Contains(rendering, typeof(FluxParticleRotationOverLifetimeModule)));
            Assert.IsTrue(Contains(rendering, typeof(FluxParticleTextureSheetAnimationModule)));
        }

        static bool Contains(
            IReadOnlyList<FluxParticleInspectorModuleMetadata> modules,
            System.Type type)
        {
            foreach (var module in modules)
            {
                if (module.ModuleType == type) return true;
            }

            return false;
        }
    }
}