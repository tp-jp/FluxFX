using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Udon.Parameters;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        public sealed class CountingModuleHandle : FluxParticleModuleHandle
        {
            public static int Created;

            public CountingModuleHandle()
            {
                Created++;
            }
        }

        [Test]
        public void ModuleFactoryInitializesOnlyOnceAndDoesNotAllocateMissingHandles()
        {
            RequireGraphics();
            SetModules(new FluxParticleGravityModule());
            var runtime = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            var modules = runtime.GetComponentInParent<FluxParticleSystem>().Modules;
            CountingModuleHandle.Created = 0;
            Assert.IsNull(FluxParticleModuleHandleFactory.Get<CountingModuleHandle>(null, FluxParticleModuleIds.Gravity));
            for (var i = 0; i < 2; i++)
            {
                Assert.IsNull(FluxParticleModuleHandleFactory.Get<CountingModuleHandle>(modules, FluxParticleModuleIds.Noise));
            }
            Assert.AreEqual(0, CountingModuleHandle.Created);
            var handle = FluxParticleModuleHandleFactory.Get<CountingModuleHandle>(modules, FluxParticleModuleIds.Gravity);
            Assert.AreEqual(1, CountingModuleHandle.Created);
            Assert.AreEqual(3, handle.Count);
            Assert.AreSame(runtime, handle[0].Reference);
            Assert.AreSame(handle, FluxParticleModuleHandleFactory.Get<CountingModuleHandle>(modules, FluxParticleModuleIds.Gravity));
            Assert.AreEqual(1, CountingModuleHandle.Created);
            Assert.AreEqual(3, handle.Count);
        }

        [Test]
        public void HandleInfrastructureIsInternalAndExtensionApiRemainsPublic()
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            foreach (var name in new[] { "GetBinding", "GetCachedHandle", "CacheHandle" })
            {
                Assert.IsNull(typeof(FluxParticleModules).GetMethod(name, flags), name);
            }
            foreach (var name in new[] { "Initialize", "InitializeSlots" })
            {
                Assert.IsNull(typeof(FluxParticleParameterHandle).GetMethod(name, flags), name);
            }
            var handles = typeof(FluxParticleModules).GetProperties()
                .Where(property => typeof(FluxParticleModuleHandle).IsAssignableFrom(property.PropertyType));
            Assert.AreEqual(13, handles.Count());
            foreach (var property in handles)
            {
                Assert.IsNull(property.PropertyType.GetMethod("Get", flags), property.Name);
            }
            Assert.IsNotNull(typeof(FluxParticleModuleHandleFactory).GetMethod("Get", flags));
            foreach (var name in new[] { "HasParameter", "GetValue", "SetValue", "SetPair" })
            {
                Assert.IsNotNull(typeof(FluxParticleParameterHandleExtensions).GetMethod(name, flags), name);
            }
        }

        [Test]
        public void StandardModuleIdsAreUniqueAndMatchContractsAndAccessors()
        {
            var ids = typeof(FluxParticleModuleIds).GetFields(BindingFlags.Public | BindingFlags.Static);
            var contracts = typeof(FluxParticleBuildPipeline).Assembly.GetTypes()
                .Where(type => type.IsAbstract && type.IsSealed && type.Name.EndsWith("Contract"))
                .Select(type => type.GetProperty("Module")?.GetValue(null))
                .OfType<FluxParticleModuleContract>().ToArray();
            Assert.AreEqual(contracts.Length, ids.Length);
            Assert.AreEqual(ids.Length, ids.Select(field => field.GetRawConstantValue()).Distinct().Count());
            foreach (var id in ids)
            {
                var contract = contracts.Single(item => item.Id == (string)id.GetRawConstantValue());
                Assert.AreEqual(id.Name, contract.AccessorName);
                Assert.IsNotNull(typeof(FluxParticleModules).GetProperty(id.Name));
            }
        }
    }
}
