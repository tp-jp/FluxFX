using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class FluxParticleCompileValidatorTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("CompilePlanTest");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void EmptyModules_ShouldPass()
        {
            var result = Validate();

            Assert.IsTrue(result.IsValid, result.GetReport());
            Assert.IsEmpty(result.Issues);
        }

        [Test]
        public void ExistingModules_ShouldPass()
        {
            SetModules(
                new FluxParticleGravityModule(),
                new FluxParticleForceModule(),
                new FluxParticleDragModule(),
                new FluxParticleNoiseModule(),
                new FluxParticleVortexModule(),
                new FluxParticleLimitVelocityModule(),
                new FluxParticleVelocityOverLifetimeModule(),
                new FluxParticleColorOverLifetimeModule(),
                new FluxParticleSizeOverLifetimeModule(),
                new FluxParticleRotationOverLifetimeModule(),
                new FluxParticleTextureSheetAnimationModule());

            var result = Validate();

            Assert.IsTrue(result.IsValid, result.GetReport());
            Assert.IsEmpty(result.Issues);
        }

        [Test]
        public void UndefinedStage_ShouldReportV001()
        {
            SetModules(new TestModule(
                FluxParticleExecutionStage.None,
                FluxParticleAttribute.None,
                FluxParticleAttribute.None));

            AssertIssue("V001");
        }

        [Test]
        public void MultipleStages_ShouldReportV001()
        {
            SetModules(new TestModule(
                FluxParticleExecutionStage.VelocityUpdate |
                FluxParticleExecutionStage.Rendering,
                FluxParticleAttribute.None,
                FluxParticleAttribute.None));

            AssertIssue("V001");
        }

        [Test]
        public void InvalidRead_ShouldReportV002()
        {
            SetModules(new TestModule(
                FluxParticleExecutionStage.VelocityUpdate,
                FluxParticleAttribute.StartColor,
                FluxParticleAttribute.None));

            AssertIssue("V002");
        }

        [Test]
        public void InvalidWrite_ShouldReportV003()
        {
            SetModules(new TestModule(
                FluxParticleExecutionStage.Rendering,
                FluxParticleAttribute.Age,
                FluxParticleAttribute.Velocity));

            AssertIssue("V003");
        }

        [Test]
        public void DuplicateType_ShouldReportV004()
        {
            SetModules(
                new FluxParticleGravityModule(),
                new FluxParticleGravityModule());

            AssertIssue("V004");
        }

        [Test]
        public void NoiseAndVortex_ShouldReadPositionInVelocityUpdate()
        {
            SetModules(
                new FluxParticleNoiseModule(),
                new FluxParticleVortexModule());

            var result = Validate();

            Assert.IsTrue(result.IsValid, result.GetReport());
        }

        [Test]
        public void RenderingModule_ShouldAllowNoWrites()
        {
            SetModules(new FluxParticleColorOverLifetimeModule());

            var result = Validate();

            Assert.IsTrue(result.IsValid, result.GetReport());
        }

        void AssertIssue(string expectedCode)
        {
            var result = Validate();

            Assert.IsTrue(result.HasErrors);

            var found = false;

            foreach (var issue in result.Issues)
            {
                if (issue.Code != expectedCode) continue;

                found = true;
                break;
            }

            Assert.IsTrue(found, result.GetReport());
        }

        FluxParticleValidationResult Validate()
        {
            var plan = FluxParticleCompilePlan.Create(_authoring);
            return new FluxParticleCompileValidator().Validate(plan);
        }

        void SetModules(params FluxParticleModule[] modules)
        {
            var field = typeof(FluxParticleAuthoring).GetField(
                "modules",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(field);

            field.SetValue(
                _authoring,
                new List<FluxParticleModule>(modules));
        }

        [Serializable]
        sealed class TestModule : FluxParticleModule
        {
            readonly FluxParticleExecutionStage _stage;
            readonly FluxParticleAttribute _reads;
            readonly FluxParticleAttribute _writes;

            public TestModule(
                FluxParticleExecutionStage stage,
                FluxParticleAttribute reads,
                FluxParticleAttribute writes)
            {
                _stage = stage;
                _reads = reads;
                _writes = writes;
            }

            public override FluxParticleExecutionStage Stage => _stage;
            public override FluxParticleAttribute ReadAttributes => _reads;
            public override FluxParticleAttribute WriteAttributes => _writes;
        }
    }
}
