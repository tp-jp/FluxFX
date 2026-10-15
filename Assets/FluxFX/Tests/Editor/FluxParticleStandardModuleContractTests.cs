using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Editor.Contracts.Modules;
using TpLab.Flux.FX.Editor.Modules;
using TpLab.Flux.FX.Editor.Modules.Blink;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;
using CompiledPolicy = TpLab.Flux.FX.Editor.Compilation.Definitions.FluxParticleParameterPolicy;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleStandardModuleContractTests
    {
        [TestCase("Drag")]
        [TestCase("Force")]
        [TestCase("LimitVelocity")]
        [TestCase("Noise")]
        [TestCase("Vortex")]
        [TestCase("VelocityOverLifetime")]
        [TestCase("TextureSheetAnimation")]
        [TestCase("Trails")]
        [TestCase("Blink")]
        public void ContractPreservesIdentityEnabledAndHandleSlots(string name)
        {
            var contract = Contract(name);
            var expected = ParameterIds(name);
            var moduleType = typeof(FluxParticleModule).Assembly.GetType("TpLab.Flux.FX.Scripts.Modules.FluxParticle" + name + "Module", true);
            var definitionType = typeof(IFluxParticleModuleDefinition).Assembly.GetType("TpLab.Flux.FX.Editor.Modules." + name + ".FluxParticle" + name + "Definition", true);
            var definition = (IFluxParticleModuleDefinition)Activator.CreateInstance(definitionType);
            Assert.AreEqual("FluxParticle" + name + "Handle", contract.HandleName);
            Assert.AreEqual(name, contract.AccessorName);
            CollectionAssert.AreEqual(expected, contract.Parameters.Select(parameter => parameter.Id));
            foreach (var enabled in new[] { false, true })
            {
                var settings = (FluxParticleModule)Activator.CreateInstance(moduleType);
                typeof(FluxParticleModule).GetField("enabled", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, enabled);
                var context = new FluxParticleProgramContribution(new FluxParticleDefinitionBuilder());
                definition.Contribute(settings, context);
                var binding = context.ModuleBindings[definition.ModuleId];
                Assert.AreEqual(contract.Id, binding.ModuleId);
                Assert.AreEqual(contract.Enabled.Id, binding.EnabledParameterId);
                Assert.AreSame(contract.Enabled, context.Parameters[binding.EnabledParameterId].Contract);
                Assert.AreEqual(enabled ? 1 : 0, context.InitialValues[binding.EnabledParameterId].x);
                CollectionAssert.AreEqual(expected, binding.Parameters.Select(parameter => parameter.Id));
                Assert.AreEqual(expected.Length + 1, context.Parameters.Count);
                for (var i = 0; i < expected.Length; i++)
                {
                    Assert.AreSame(contract.Parameters[i], binding.Parameters[i].Contract);
                    Assert.IsTrue(context.InitialValues.ContainsKey(expected[i]));
                    Assert.AreEqual(expected[i] == "texture-sheet.tiles" ? CompiledPolicy.Static : CompiledPolicy.Live, binding.Parameters[i].Policy);
                }
            }
        }

        [Test]
        public void NumericContractsPreserveBoundsAndSignedValues()
        {
            foreach (var parameter in new[] { FluxParticleLimitVelocityContract.MaxSpeed, FluxParticleNoiseContract.Strength,
                FluxParticleNoiseContract.Scale, FluxParticleNoiseContract.Speed, FluxParticleVortexContract.Strength,
                FluxParticleTextureSheetAnimationContract.Cycles, FluxParticleTrailsContract.Width, FluxParticleBlinkContract.Frequency })
            {
                Assert.IsTrue(parameter.IsValid(0), parameter.Id);
                Assert.IsTrue(parameter.IsValid(float.MaxValue), parameter.Id);
                Assert.IsFalse(parameter.IsValid(-1), parameter.Id);
                Assert.IsFalse(parameter.IsValid(float.NaN), parameter.Id);
                Assert.IsFalse(parameter.IsValid(float.PositiveInfinity), parameter.Id);
            }
            foreach (var parameter in new[] { FluxParticleBlinkContract.Duty, FluxParticleBlinkContract.Minimum,
                FluxParticleBlinkContract.Smoothness, FluxParticleBlinkContract.RandomPhase })
            {
                Assert.IsTrue(parameter.IsValid(0), parameter.Id);
                Assert.IsTrue(parameter.IsValid(1), parameter.Id);
                Assert.IsFalse(parameter.IsValid(-0.01f), parameter.Id);
                Assert.IsFalse(parameter.IsValid(1.01f), parameter.Id);
            }
            Assert.IsTrue(FluxParticleDragContract.Drag.IsValid(-1));
            Assert.IsTrue(FluxParticleVelocityOverLifetimeContract.Radial.IsValid(-1));
            Assert.IsTrue(FluxParticleVortexContract.Axis.IsValid(Vector3.zero));
            Assert.IsTrue(FluxParticleTrailsContract.Color.IsValid(new Color(4, -1, 2, 1.5f)));
            Assert.IsFalse(FluxParticleTrailsContract.Duration.IsValid(0));
            Assert.IsTrue(FluxParticleTrailsContract.Duration.IsValid(0.001f));
            Assert.IsFalse(FluxParticleTrailsContract.Duration.IsValid(float.PositiveInfinity));
            Assert.IsTrue(FluxParticleTextureSheetAnimationContract.Tiles.IsValid(new Vector2(1, 16777216)));
            Assert.IsFalse(FluxParticleTextureSheetAnimationContract.Tiles.IsValid(new Vector2(0, 1)));
            Assert.IsFalse(FluxParticleTextureSheetAnimationContract.Tiles.IsValid(new Vector2(1, 1.5f)));
        }

        [Test]
        public void BlinkContractsUsePublicEnumsAndPreserveShaderEncoding()
        {
            var settings = new FluxParticleBlinkModule();
            typeof(FluxParticleBlinkModule).GetField("timeMode", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, FluxParticleBlinkTimeMode.ParticleAge);
            typeof(FluxParticleBlinkModule).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, FluxParticleBlinkTarget.Both);
            var context = new FluxParticleProgramContribution(new FluxParticleDefinitionBuilder());
            new FluxParticleBlinkDefinition().Contribute(settings, context);
            Assert.AreEqual(typeof(FluxParticleBlinkTimeMode), FluxParticleBlinkContract.TimeMode.ValueType);
            Assert.AreEqual(typeof(FluxParticleBlinkTarget), FluxParticleBlinkContract.Target.ValueType);
            Assert.AreEqual(1, context.InitialValues["blink.time-mode"].x);
            Assert.AreEqual(2, context.InitialValues["blink.target"].x);
            Assert.AreEqual(FluxParticleValueType.Float, context.Parameters["blink.time-mode"].Type);
            Assert.AreEqual(FluxParticleValueType.Float, context.Parameters["blink.target"].Type);
            Assert.IsFalse(FluxParticleBlinkContract.TimeMode.IsValid((FluxParticleBlinkTimeMode)99));
            Assert.IsFalse(FluxParticleBlinkContract.Target.IsValid((FluxParticleBlinkTarget)(-1)));
        }

        static FluxParticleModuleContract Contract(string name)
        {
            switch (name)
            {
                case "Drag": return FluxParticleDragContract.Module;
                case "Force": return FluxParticleForceContract.Module;
                case "LimitVelocity": return FluxParticleLimitVelocityContract.Module;
                case "Noise": return FluxParticleNoiseContract.Module;
                case "Vortex": return FluxParticleVortexContract.Module;
                case "VelocityOverLifetime": return FluxParticleVelocityOverLifetimeContract.Module;
                case "TextureSheetAnimation": return FluxParticleTextureSheetAnimationContract.Module;
                case "Trails": return FluxParticleTrailsContract.Module;
                case "Blink": return FluxParticleBlinkContract.Module;
                default: throw new ArgumentOutOfRangeException(nameof(name));
            }
        }

        static string[] ParameterIds(string name)
        {
            switch (name)
            {
                case "Drag": return new[] { "drag" };
                case "Force": return new[] { "force" };
                case "LimitVelocity": return new[] { "limit-velocity.max-speed" };
                case "Noise": return new[] { "noise.strength", "noise.scale", "noise.speed" };
                case "Vortex": return new[] { "vortex.center", "vortex.axis", "vortex.strength" };
                case "VelocityOverLifetime": return new[] { "velocity-over-lifetime.start", "velocity-over-lifetime.end", "velocity-over-lifetime.orbital", "velocity-over-lifetime.offset", "velocity-over-lifetime.radial" };
                case "TextureSheetAnimation": return new[] { "texture-sheet.tiles", "texture-sheet.cycles" };
                case "Trails": return new[] { "trails.width", "trails.color", "trails.duration" };
                case "Blink": return new[] { "blink.frequency", "blink.duty", "blink.minimum", "blink.smoothness", "blink.random-phase", "blink.time-mode", "blink.target" };
                default: throw new ArgumentOutOfRangeException(nameof(name));
            }
        }
    }
}
