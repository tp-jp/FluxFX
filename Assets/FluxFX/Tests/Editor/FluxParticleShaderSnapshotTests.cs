using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleShaderSnapshotTests
    {
        const string Output = "Assets/Generated/Cache/Shaders";
        Dictionary<string, string> _sources;

        [SetUp]
        public void SetUp()
        {
            _sources = new Dictionary<string, string>
            {
                ["Assets/Root.hlsl"] = "#include \"Child.hlsl\"\nfloat Root() { return Child(); }",
                ["Assets/Child.hlsl"] = "float Child() { return 1; }"
            };
        }

        [Test]
        public void NestedIncludesUseImmutablePaths()
        {
            var snapshot = Build();
            Assert.AreEqual(2, snapshot.IncludeSources.Count);
            StringAssert.DoesNotContain("Assets/Root.hlsl", snapshot.Source);
            Assert.IsTrue(snapshot.IncludeSources.Values.Any(source => source.Contains("#include \"" + Output + "/Include-")));
            Assert.IsFalse(snapshot.IncludeSources.Values.Any(source => source.Contains("\"Child.hlsl\"")));
        }

        [Test]
        public void ChangedChildChangesParentAndShaderButPreservesOldSnapshot()
        {
            var first = Build();
            _sources["Assets/Child.hlsl"] = "float Child() { return 2; }";
            var second = Build();
            Assert.AreNotEqual(first.Source, second.Source);
            Assert.AreEqual(0, first.IncludeSources.Keys.Intersect(second.IncludeSources.Keys).Count());
            Assert.Contains("float Child() { return 1; }", first.IncludeSources.Values.ToArray());
            Assert.Contains("float Child() { return 2; }", second.IncludeSources.Values.ToArray());
        }

        [Test]
        public void SameContentReusesPathsAndSharedDependencies()
        {
            var first = Build("#include \"Assets/Root.hlsl\"\n#include \"Assets/Child.hlsl\"");
            var second = Build("#include \"Assets/Root.hlsl\"\n#include \"Assets/Child.hlsl\"");
            Assert.AreEqual(first.Source, second.Source);
            CollectionAssert.AreEquivalent(first.IncludeSources, second.IncludeSources);
            Assert.AreEqual(2, first.IncludeSources.Count);
        }

        [Test]
        public void RelativeAndPackageIncludesAreFrozen()
        {
            _sources["Assets/Root.hlsl"] = "#include \"Packages/test/Folder/../Child.cginc\"";
            _sources["Packages/test/Child.cginc"] = "#include \"Assets/Child.hlsl\"";
            var snapshot = Build();
            Assert.AreEqual(3, snapshot.IncludeSources.Count);
            Assert.IsFalse(snapshot.IncludeSources.Values.Any(source => source.Contains("Packages/test/")));
        }

        [Test]
        public void UnityIncludesRemainExternal()
        {
            var snapshot = Build("#include \"UnityCG.cginc\"");
            Assert.AreEqual("#include \"UnityCG.cginc\"", snapshot.Source);
            Assert.IsEmpty(snapshot.IncludeSources);
        }

        [Test]
        public void CommentsDoNotCreateDependenciesAndPragmaIncludesRemainPragmaIncludes()
        {
            var snapshot = Build("/*\n#include \"Missing.hlsl\"\n*/\n// #include BAD\n#include_with_pragmas \"Assets/Child.hlsl\" /* comment */");
            Assert.AreEqual(1, snapshot.IncludeSources.Count);
            StringAssert.Contains("#include_with_pragmas \"" + Output, snapshot.Source);
            StringAssert.Contains("/* comment */", snapshot.Source);
        }

        [Test]
        public void CircularDependencyIsReported()
        {
            _sources["Assets/Child.hlsl"] = "#include \"Root.hlsl\"";
            StringAssert.Contains("Circular Shader include dependency", Assert.Throws<InvalidOperationException>(() => Build()).Message);
        }

        [Test]
        public void MissingDependencyIsReported()
        {
            _sources.Remove("Assets/Child.hlsl");
            Assert.Throws<FileNotFoundException>(() => Build());
        }

        [TestCase("#include MACRO")]
        [TestCase("#include \"../Outside.hlsl\"")]
        public void UnresolvedOrEscapingIncludeIsRejected(string source)
        {
            Assert.Throws<InvalidOperationException>(() => Build(source));
        }

        [Test]
        public void LineEndingChangesDoNotCreateNewSnapshots()
        {
            var first = Build();
            _sources["Assets/Root.hlsl"] = _sources["Assets/Root.hlsl"].Replace("\n", "\r\n");
            Assert.AreEqual(first.Source, Build().Source);
        }

        FluxParticleShaderSnapshot Build(string source = "#include \"Assets/Root.hlsl\"")
        {
            return FluxParticleShaderSnapshot.Create(source, Output, path =>
                _sources.TryGetValue(path, out var text) ? text : throw new FileNotFoundException(path));
        }
    }
}
