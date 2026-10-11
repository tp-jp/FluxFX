using System;
using NUnit.Framework;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Scripts;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class FluxFXCompilationSessionTests
    {
        GameObject _firstObject;
        GameObject _secondObject;
        FluxParticleAuthoring _firstAuthoring;
        FluxParticleAuthoring _secondAuthoring;

        [SetUp]
        public void SetUp()
        {
            _firstObject = new GameObject("Particle System");
            _secondObject = new GameObject("Particle System");

            _firstAuthoring = _firstObject.AddComponent<FluxParticleAuthoring>();
            _secondAuthoring = _secondObject.AddComponent<FluxParticleAuthoring>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_firstObject);
            Object.DestroyImmediate(_secondObject);
        }

        [Test]
        public void AddAndGetRequired()
        {
            var session = new FluxFXCompilationSession();
            var result = CreateResult(_firstAuthoring);

            session.Add(_firstAuthoring, result);

            Assert.AreEqual(1, session.Count);
            Assert.AreSame(result, session.GetRequired(_firstAuthoring));
        }

        [Test]
        public void DifferentAuthoringsAreIndependent()
        {
            var session = new FluxFXCompilationSession();
            var first = CreateResult(_firstAuthoring);
            var second = CreateResult(_secondAuthoring);

            session.Add(_firstAuthoring, first);
            session.Add(_secondAuthoring, second);

            Assert.AreEqual(2, session.Count);
            Assert.AreSame(first, session.GetRequired(_firstAuthoring));
            Assert.AreSame(second, session.GetRequired(_secondAuthoring));
        }

        [Test]
        public void DuplicateRegistrationIsRejected()
        {
            var session = new FluxFXCompilationSession();
            var result = CreateResult(_firstAuthoring);

            session.Add(_firstAuthoring, result);

            Assert.Throws<InvalidOperationException>(() => session.Add(_firstAuthoring, result));
        }

        [Test]
        public void TryGetReturnsFalseWhenMissing()
        {
            var session = new FluxFXCompilationSession();

            Assert.IsFalse(session.TryGet(_firstAuthoring, out var result));
            Assert.IsNull(result);
        }

        [Test]
        public void GetRequiredThrowsWhenMissing()
        {
            var session = new FluxFXCompilationSession();

            Assert.Throws<InvalidOperationException>(() => session.GetRequired(_firstAuthoring));
        }

        [Test]
        public void SessionsAreIndependent()
        {
            var firstSession = new FluxFXCompilationSession();
            var secondSession = new FluxFXCompilationSession();

            firstSession.Add(_firstAuthoring, CreateResult(_firstAuthoring));

            Assert.AreEqual(1, firstSession.Count);
            Assert.AreEqual(0, secondSession.Count);
            Assert.IsFalse(secondSession.TryGet(_firstAuthoring, out _));
        }

        static FluxParticleBuildResult CreateResult(FluxParticleAuthoring authoring)
        {
            var registry = new FluxParticleGpuModuleRegistry();
            var plan = FluxParticleCompilePlan.Create(authoring);
            var result = new FluxParticleGpuCompiler(registry).Compile(plan);

            Assert.IsTrue(result.Success, result.Validation.GetReport());

            var artifact = new FluxParticleShaderPipeline(registry).BuildVelocity(result.Compilation);

            return new FluxParticleBuildResult(result.Compilation, artifact);
        }
    }
}