using System;
using System.Collections.Generic;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Contracts;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleContractTests
    {
        enum BlinkTime
        {
            Simulation = 0,
            ParticleAge = 3,
            AgeAlias = ParticleAge
        }

        [Flags]
        enum Flags
        {
            First = 1,
            Second = 2
        }

        enum Empty
        {
        }

        [Test]
        public void GravityContractKeepsTypedParameterAndSeparateEnabled()
        {
            FluxParticleParameterContract<Vector3> acceleration = FluxParticleParameter.Vector3("gravity")
                .ApiName("Gravity").Policy(FluxParticleParameterPolicy.Live).Build();
            var module = Module("fluxfx.gravity").Enabled("gravity.enabled").Parameter(acceleration).Build();

            Assert.AreEqual("fluxfx.gravity", module.Id);
            Assert.AreEqual(typeof(Vector3), acceleration.ValueType);
            Assert.AreEqual(FluxParticleParameterKind.Vector3, acceleration.Kind);
            Assert.IsTrue(acceleration.IsValid(new Vector3(0, -9.81f, 0)));
            Assert.IsFalse(acceleration.IsValid(new Vector3(0, float.NaN, 0)));
            Assert.IsFalse(acceleration.IsValid(new Vector3(0, 0, float.PositiveInfinity)));
            Assert.AreSame(acceleration, module.Parameters[0]);
            Assert.AreEqual(1, module.Parameters.Count);
            Assert.AreEqual(FluxParticleParameterKind.Bool, module.Enabled.Kind);
            Assert.AreEqual(FluxParticleParameterPolicy.Live, module.Enabled.Policy);
            Assert.IsTrue(module.Enabled.IsValid(false));
            Assert.IsTrue(module.Enabled.IsValid(true));
        }

        [Test]
        public void SizeAndBlinkConstraintsHaveTypedValuesAndExplicitPolicies()
        {
            var size = Float("size.end", "EndSize").Minimum(0).Build();
            var duty = Float("blink.duty", "Duty").Range(0, 1).Build();
            var time = FluxParticleParameter.Enum<BlinkTime>("blink.time-mode").ApiName("TimeMode")
                .Policy(FluxParticleParameterPolicy.Static).Build();
            var spawn = Float("spawn.speed", "Speed").Policy(FluxParticleParameterPolicy.SpawnLocked).Build();

            Assert.IsTrue(size.IsValid(0));
            Assert.IsFalse(size.IsValid(-float.Epsilon));
            Assert.IsTrue(duty.IsValid(0));
            Assert.IsTrue(duty.IsValid(1));
            Assert.IsFalse(duty.IsValid(1.01f));
            Assert.IsTrue(time.IsValid(BlinkTime.ParticleAge));
            Assert.IsFalse(time.IsValid((BlinkTime)1));
            Assert.AreEqual(FluxParticleParameterPolicy.Static, time.Policy);
            Assert.AreEqual(FluxParticleParameterPolicy.SpawnLocked, spawn.Policy);
        }

        [Test]
        public void BuiltContractsDoNotFollowBuilderChanges()
        {
            var parameterBuilder = Float("test.value", "Value").Range(0, 1);
            var parameter = parameterBuilder.Build();
            var moduleBuilder = Module("test.module").Enabled("test.enabled").Parameter(parameter);
            var module = moduleBuilder.Build();

            parameterBuilder.ApiName("Changed").Policy(FluxParticleParameterPolicy.Static).Range(10, 20);
            moduleBuilder.Handle("ChangedHandle").Accessor("Changed").Enabled("changed.enabled")
                .Parameter(Float("test.other", "Other").Build());
            var changedModule = moduleBuilder.Build();

            Assert.AreEqual("Value", parameter.ApiName);
            Assert.AreEqual(FluxParticleParameterPolicy.Live, parameter.Policy);
            Assert.IsTrue(parameter.IsValid(0.5f));
            Assert.IsFalse(parameter.IsValid(15));
            Assert.IsTrue(parameterBuilder.Build().IsValid(15));
            Assert.AreEqual("TestHandle", module.HandleName);
            Assert.AreEqual("Test", module.AccessorName);
            Assert.AreEqual("test.enabled", module.Enabled.Id);
            Assert.AreEqual(1, module.Parameters.Count);
            Assert.AreEqual(2, changedModule.Parameters.Count);
            Assert.AreEqual("ChangedHandle", changedModule.HandleName);
            Assert.AreEqual("changed.enabled", changedModule.Enabled.Id);
            Assert.AreEqual(1, module.Parameters.Count);
        }

        [Test]
        public void ContractCollectionsCannotBeChangedThroughMutableInterfaces()
        {
            var numeric = Float("test.value", "Value").Range(0, 1).Build();
            var module = Module("test.module").Parameter(numeric).Build();
            var enumeration = FluxParticleParameter.Enum<BlinkTime>("test.enum").ApiName("Mode")
                .Policy(FluxParticleParameterPolicy.Live).Build();

            Assert.Throws<NotSupportedException>(() => ((IList<float>)numeric.NumericConstraint.Minimum)[0] = -1);
            Assert.Throws<NotSupportedException>(() => ((IList<float>)numeric.NumericConstraint.Maximum).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<FluxParticleParameterContract>)module.Parameters).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<BlinkTime>)enumeration.AllowedValues)[0] = (BlinkTime)99);
            FluxParticleParameterContract untyped = enumeration;
            Assert.AreEqual(typeof(BlinkTime), untyped.EnumConstraint.EnumType);
            Assert.AreEqual(BlinkTime.ParticleAge, untyped.EnumConstraint.AllowedValues[1]);
            Assert.Throws<NotSupportedException>(() => ((IList<Enum>)untyped.EnumConstraint.AllowedValues).Clear());
            Assert.AreEqual(2, enumeration.AllowedValues.Count);
            Assert.IsNull(module.Enabled);
        }

        [Test]
        public void EnumInputArraysAndLaterBuilderChangesCannotChangeContracts()
        {
            var input = new[] { BlinkTime.Simulation };
            var builder = FluxParticleParameter.Enum<BlinkTime>("test.enum").ApiName("Mode")
                .Policy(FluxParticleParameterPolicy.Live).AllowedValues(input);
            input[0] = BlinkTime.ParticleAge;
            var contract = builder.Build();
            builder.AllowedValues(BlinkTime.ParticleAge);

            Assert.IsTrue(contract.IsValid(BlinkTime.Simulation));
            Assert.IsFalse(contract.IsValid(BlinkTime.ParticleAge));
            Assert.IsTrue(builder.Build().IsValid(BlinkTime.ParticleAge));
        }

        [Test]
        public void EnumConstraintsRejectUnknownEmptyDuplicateAndUndeclaredFlagCombinations()
        {
            var builder = FluxParticleParameter.Enum<BlinkTime>("test.enum").ApiName("Mode")
                .Policy(FluxParticleParameterPolicy.Live);
            Assert.Throws<InvalidOperationException>(() => builder.AllowedValues((BlinkTime)2).Build());
            Assert.Throws<InvalidOperationException>(() => builder.AllowedValues().Build());
            Assert.Throws<InvalidOperationException>(() => builder.AllowedValues(BlinkTime.ParticleAge, BlinkTime.AgeAlias).Build());
            Assert.Throws<InvalidOperationException>(() => FluxParticleParameter.Enum<Empty>("test.empty")
                .ApiName("Empty").Policy(FluxParticleParameterPolicy.Live).Build());
            var flags = FluxParticleParameter.Enum<Flags>("test.flags").ApiName("Flags")
                .Policy(FluxParticleParameterPolicy.Live).Build();
            Assert.IsTrue(flags.IsValid(Flags.First));
            Assert.IsFalse(flags.IsValid(Flags.First | Flags.Second));
        }

        [TestCase(0f, 0f, true, false)]
        [TestCase(1f, 0f, true, true)]
        [TestCase(float.NaN, 1f, true, true)]
        [TestCase(0f, float.PositiveInfinity, true, true)]
        [TestCase(0f, float.Epsilon, false, false)]
        public void InvalidNumericRangesFailWithParameterId(float min, float max, bool includeMin, bool includeMax)
        {
            var error = Assert.Throws<InvalidOperationException>(() => Float("size.end", "EndSize")
                .Range(min, max, includeMin, includeMax).Build());
            StringAssert.Contains("size.end", error.Message);
        }

        [Test]
        public void OpenBoundsAndIntegerRangesRespectRepresentableValues()
        {
            var positive = Float("test.positive", "Positive").Minimum(0, false).Build();
            Assert.IsFalse(positive.IsValid(0));
            Assert.IsTrue(positive.IsValid(float.Epsilon));
            Assert.IsFalse(positive.IsValid(float.PositiveInfinity));
            var count = Float("test.count", "Count").Range(-2, 2, false, false).Integer().Build();
            Assert.IsTrue(count.IsValid(-1));
            Assert.IsFalse(count.IsValid(-2));
            Assert.IsFalse(count.IsValid(0.5f));
            Assert.IsFalse(count.IsValid(2));
            Assert.Throws<InvalidOperationException>(() => Float("test.empty", "Empty").Range(0.1f, 0.9f).Integer().Build());
            Assert.Throws<InvalidOperationException>(() => Float("test.empty", "Empty").Range(1, 2, false, false).Integer().Build());
            Assert.Throws<InvalidOperationException>(() => Float("test.empty", "Empty").Minimum(float.MaxValue, false).Build());
            Assert.Throws<InvalidOperationException>(() => Float("test.empty", "Empty").Maximum(-float.MaxValue, false).Build());
            var exact = Float("test.exact", "Exact").Range(1, 1).Build();
            Assert.IsTrue(exact.IsValid(1));
            Assert.IsFalse(exact.IsValid(0));
        }

        [Test]
        public void VectorBoundsApplyToEachComponentAndColorAllowsHdr()
        {
            var vector = FluxParticleParameter.Vector3("test.vector").ApiName("Vector")
                .Policy(FluxParticleParameterPolicy.Live).Range(new Vector3(-1, 2, 3), new Vector3(1, 4, 5)).Build();
            Assert.IsTrue(vector.IsValid(new Vector3(0, 3, 4)));
            Assert.IsFalse(vector.IsValid(new Vector3(0, 1, 4)));
            var color = FluxParticleParameter.Color("test.color").ApiName("Color")
                .Policy(FluxParticleParameterPolicy.Live).Build();
            Assert.AreEqual(typeof(Color), color.ValueType);
            Assert.AreEqual(FluxParticleParameterKind.Color, color.Kind);
            Assert.IsTrue(color.IsValid(new Color(4, 2, -1, 1)));
            Assert.IsFalse(color.IsValid(new Color(1, 1, 1, float.NaN)));
            var vector2 = FluxParticleParameter.Vector2("test.xy").ApiName("XY").Policy(FluxParticleParameterPolicy.Live).Build();
            var vector4 = FluxParticleParameter.Vector4("test.xyzw").ApiName("XYZW").Policy(FluxParticleParameterPolicy.Live).Build();
            Assert.IsTrue(vector2.IsValid(new Vector2(1, 2)));
            Assert.IsFalse(vector2.IsValid(new Vector2(1, float.NaN)));
            Assert.AreEqual(FluxParticleParameterKind.Vector4, vector4.Kind);
            Assert.IsFalse(vector4.IsValid(new Vector4(1, 2, 3, float.NegativeInfinity)));
        }

        [Test]
        public void MissingMetadataAndInvalidPoliciesFailAtBuild()
        {
            Assert.Throws<InvalidOperationException>(() => FluxParticleParameter.Float("test.value").Build());
            Assert.Throws<InvalidOperationException>(() => FluxParticleParameter.Float("test.value").ApiName("Value").Build());
            Assert.Throws<InvalidOperationException>(() => Float("test.value", "Value").Policy((FluxParticleParameterPolicy)99).Build());
            Assert.Throws<InvalidOperationException>(() => new FluxParticleModuleContractBuilder("test.module").Build());
            Assert.Throws<InvalidOperationException>(() => new FluxParticleModuleContractBuilder("test.module").Handle("Handle").Build());
            Assert.Throws<InvalidOperationException>(() => Module("test.module").Enabled(null).Build());
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(" test")]
        [TestCase("test ")]
        [TestCase(null)]
        public void InvalidIdsAreRejected(string id)
        {
            Assert.Throws<InvalidOperationException>(() => Float(id, "Value").Build());
            Assert.Throws<InvalidOperationException>(() => Module(id).Build());
        }

        [TestCase("class")]
        [TestCase("9Value")]
        [TestCase("A.B")]
        [TestCase("A B")]
        [TestCase("@Value")]
        [TestCase("__arglist")]
        public void InvalidPublicNamesAreRejected(string name)
        {
            Assert.Throws<InvalidOperationException>(() => Float("test.value", name).Build());
            Assert.Throws<InvalidOperationException>(() => Module("test.module").Handle(name).Build());
            Assert.Throws<InvalidOperationException>(() => Module("test.module").Accessor(name).Build());
        }

        [Test]
        public void DuplicateAndReservedParametersAreRejectedWithModuleContext()
        {
            var value = Float("test.value", "Value").Build();
            var duplicateId = Float("test.value", "Other").Build();
            var duplicateName = Float("test.other", "Value").Build();
            var enabledName = Float("test.other", "Enabled").Build();
            Assert.Throws<ArgumentNullException>(() => Module("test.module").Parameter(null));
            var error = Assert.Throws<InvalidOperationException>(() => Module("test.module").Parameter(value).Parameter(duplicateId).Build());
            StringAssert.Contains("test.module", error.Message);
            StringAssert.Contains("test.value", error.Message);
            Assert.Throws<InvalidOperationException>(() => Module("test.module").Parameter(value).Parameter(duplicateName).Build());
            Assert.Throws<InvalidOperationException>(() => Module("test.module").Parameter(enabledName).Build());
            Assert.Throws<InvalidOperationException>(() => Module("test.module").Enabled("test.value").Parameter(value).Build());
        }

        static FluxParticleNumericParameterBuilder<float> Float(string id, string apiName)
        {
            return FluxParticleParameter.Float(id).ApiName(apiName).Policy(FluxParticleParameterPolicy.Live);
        }

        static FluxParticleModuleContractBuilder Module(string id)
        {
            return new FluxParticleModuleContractBuilder(id).Handle("TestHandle").Accessor("Test");
        }
    }
}
