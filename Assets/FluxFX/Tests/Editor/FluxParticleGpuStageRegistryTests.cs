using System;
using System.Collections.Generic;
using NUnit.Framework;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class FluxParticleGpuStageRegistryTests
    {
        [Test]
        public void VelocityStageIsRegistered()
        {
            Assert.IsTrue(FluxParticleGpuStageRegistry.TryGet(FluxParticleExecutionStage.VelocityUpdate, out var descriptor));

            Assert.AreEqual(FluxParticleKernelTarget.Velocity, descriptor.KernelTarget);
            Assert.AreEqual("velocity", descriptor.RuntimeKey);
            Assert.AreEqual(
                "Packages/com.tplab.flux.fx/Runtime/Shaders/Templates/ParticleVelocityUpdate.shader.template",
                descriptor.ShaderTemplatePath);
            Assert.AreEqual("FluxFX/ParticleVelocityUpdate", descriptor.SourceShaderName);
            Assert.AreEqual("Hidden/FluxFX/Generated/Velocity_", descriptor.GeneratedShaderPrefix);
        }

        [Test]
        public void PositionStageIsRegistered()
        {
            Assert.IsTrue(FluxParticleGpuStageRegistry.TryGet(FluxParticleExecutionStage.PositionUpdate, out var descriptor));

            Assert.AreEqual(FluxParticleKernelTarget.Position, descriptor.KernelTarget);
            Assert.AreEqual("position", descriptor.RuntimeKey);
            Assert.AreEqual(
                "Packages/com.tplab.flux.fx/Runtime/Shaders/Templates/ParticlePositionUpdate.shader.template",
                descriptor.ShaderTemplatePath);
            Assert.AreEqual("FluxFX/ParticlePositionUpdate", descriptor.SourceShaderName);
            Assert.AreEqual("Hidden/FluxFX/Generated/PositionUpdate_", descriptor.GeneratedShaderPrefix);
        }

        [Test]
        public void UnsupportedStagesAreNotRegistered()
        {
            Assert.IsFalse(FluxParticleGpuStageRegistry.TryGet(FluxParticleExecutionStage.None, out _));
            Assert.IsFalse(FluxParticleGpuStageRegistry.TryGet(FluxParticleExecutionStage.Spawn, out _));
            Assert.IsFalse(FluxParticleGpuStageRegistry.TryGet(FluxParticleExecutionStage.SpawnAttributes, out _));
            Assert.IsFalse(FluxParticleGpuStageRegistry.TryGet(FluxParticleExecutionStage.Rendering, out _));

            Assert.Throws<InvalidOperationException>(
                () => FluxParticleGpuStageRegistry.GetRequired(FluxParticleExecutionStage.Rendering));
        }

        [Test]
        public void StagesAreReturnedInExecutionOrder()
        {
            var descriptors = FluxParticleGpuStageRegistry.GetAll();

            Assert.AreEqual(2, descriptors.Count);
            Assert.AreEqual(FluxParticleExecutionStage.VelocityUpdate, descriptors[0].Stage);
            Assert.AreEqual(FluxParticleExecutionStage.PositionUpdate, descriptors[1].Stage);
        }

        [Test]
        public void DescriptorReferencesAreStable()
        {
            var first = FluxParticleGpuStageRegistry.GetAll();
            var second = FluxParticleGpuStageRegistry.GetAll();

            Assert.AreSame(first, second);
            Assert.AreSame(
                first[0],
                FluxParticleGpuStageRegistry.GetRequired(FluxParticleExecutionStage.VelocityUpdate));
        }

        [Test]
        public void DescriptorListIsReadOnly()
        {
            var descriptors = FluxParticleGpuStageRegistry.GetAll();
            var list = descriptors as IList<FluxParticleGpuStageDescriptor>;

            Assert.IsNotNull(list);
            Assert.IsTrue(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list.Clear());
        }

        [Test]
        public void RuntimeKeysKernelTargetsAndShaderPrefixesAreUnique()
        {
            var runtimeKeys = new HashSet<string>(StringComparer.Ordinal);
            var kernelTargets = new HashSet<FluxParticleKernelTarget>();
            var shaderPrefixes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var descriptor in FluxParticleGpuStageRegistry.GetAll())
            {
                Assert.IsTrue(runtimeKeys.Add(descriptor.RuntimeKey));
                Assert.IsTrue(kernelTargets.Add(descriptor.KernelTarget));
                Assert.IsTrue(shaderPrefixes.Add(descriptor.GeneratedShaderPrefix));
            }
        }

        [Test]
        public void SupportedStagesHaveValidAttributeContracts()
        {
            foreach (var descriptor in FluxParticleGpuStageRegistry.GetAll())
            {
                Assert.IsTrue(FluxParticleStageContract.TryGet(descriptor.Stage, out var contract));
                Assert.IsTrue(contract.HasExecutionPath);
                Assert.AreEqual(descriptor.Stage, contract.Stage);
                Assert.AreNotEqual(FluxParticleAttribute.None, contract.WritableAttributes);
            }
        }

        [Test]
        public void InvalidDescriptorArgumentsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new FluxParticleGpuStageDescriptor(
                FluxParticleExecutionStage.VelocityUpdate | FluxParticleExecutionStage.PositionUpdate,
                FluxParticleKernelTarget.Velocity,
                "velocity",
                "template",
                "shader",
                "prefix"));

            Assert.Throws<ArgumentException>(() => new FluxParticleGpuStageDescriptor(
                FluxParticleExecutionStage.VelocityUpdate,
                FluxParticleKernelTarget.Velocity,
                "",
                "template",
                "shader",
                "prefix"));
        }
    }
}