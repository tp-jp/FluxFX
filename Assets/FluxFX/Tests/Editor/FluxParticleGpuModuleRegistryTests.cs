using System;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class FluxParticleGpuModuleRegistryTests
    {
        [Test]
        public void RegistryContainsStandardModules()
        {
            var registry = new FluxParticleGpuModuleRegistry();

            Assert.IsTrue(registry.TryGet(typeof(FluxParticleGravityModule), out var gravity));
            Assert.IsTrue(registry.TryGet(typeof(FluxParticleDragModule), out var drag));

            Assert.AreEqual("tplab.fluxfx.gravity", gravity.ModuleId);
            Assert.AreEqual("tplab.fluxfx.drag", drag.ModuleId);
        }

        [Test]
        public void RegistryResolvesGpuDefinitions()
        {
            var registry = new FluxParticleGpuModuleRegistry();

            var gravity = registry.Get(typeof(FluxParticleGravityModule));
            var drag = registry.Get(typeof(FluxParticleDragModule));

            Assert.AreEqual("FluxFX_Gravity", gravity.EntryPoint);
            Assert.AreEqual("FluxFX_Drag", drag.EntryPoint);

            Assert.Less(gravity.Order, drag.Order);
        }

        [Test]
        public void RegistryRejectsUnknownModule()
        {
            var registry = new FluxParticleGpuModuleRegistry();

            Assert.Throws<InvalidOperationException>(() =>
                registry.Get(typeof(FluxParticleNoiseModule)));
        }
    }
}