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

        static bool Contains(
            System.Collections.Generic.IReadOnlyList<FluxParticleInspectorModuleMetadata> modules,
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