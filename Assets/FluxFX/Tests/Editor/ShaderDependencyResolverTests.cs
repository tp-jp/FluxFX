using System;
using System.IO;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Shaders.Dependencies;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class ShaderDependencyResolverTests
    {
        const string TestDirectory = "Assets/FluxFX/Tests/TempShaderDependencies";

        ShaderDependencyResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TestDirectory);
            _resolver = new ShaderDependencyResolver();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(TestDirectory))
            {
                Directory.Delete(TestDirectory, true);
            }
        }

        [Test]
        public void SingleSource_ShouldResolve()
        {
            Write("Root.hlsl", "float Value = 1;");

            var graph = _resolver.Resolve(new[] { PathOf("Root.hlsl") });

            Assert.AreEqual(1, graph.Sources.Count);
            Assert.AreEqual(1, graph.Roots.Count);
            Assert.IsEmpty(graph.GetSource(PathOf("Root.hlsl")).Includes);
        }

        [Test]
        public void NestedIncludes_ShouldResolve()
        {
            Write("Root.hlsl", "#include \"Child.hlsl\"");
            Write("Child.hlsl", "#include \"Leaf.hlsl\"");
            Write("Leaf.hlsl", "float Value = 1;");

            var graph = _resolver.Resolve(new[] { PathOf("Root.hlsl") });

            Assert.AreEqual(3, graph.Sources.Count);
            Assert.AreEqual(PathOf("Child.hlsl"), graph.GetSource(PathOf("Root.hlsl")).Includes[0].Path);
            Assert.AreEqual(PathOf("Leaf.hlsl"), graph.GetSource(PathOf("Child.hlsl")).Includes[0].Path);
        }

        [Test]
        public void SharedDependency_ShouldResolveOnce()
        {
            Write("A.hlsl", "#include \"Shared.hlsl\"");
            Write("B.hlsl", "#include \"Shared.hlsl\"");
            Write("Shared.hlsl", "float Value = 1;");

            var graph = _resolver.Resolve(new[] { PathOf("A.hlsl"), PathOf("B.hlsl") });

            Assert.AreEqual(3, graph.Sources.Count);
            Assert.AreEqual(2, graph.Roots.Count);
        }

        [Test]
        public void CircularDependency_ShouldThrow()
        {
            Write("A.hlsl", "#include \"B.hlsl\"");
            Write("B.hlsl", "#include \"A.hlsl\"");

            var exception = Assert.Throws<InvalidOperationException>(() => _resolver.Resolve(new[] { PathOf("A.hlsl") }));

            StringAssert.Contains("Circular Shader include dependency", exception.Message);
        }

        [Test]
        public void MissingDependency_ShouldThrow()
        {
            Write("Root.hlsl", "#include \"Missing.hlsl\"");

            Assert.Throws<FileNotFoundException>(() => _resolver.Resolve(new[] { PathOf("Root.hlsl") }));
        }

        [Test]
        public void ExternalInclude_ShouldNotResolveAsProjectFile()
        {
            Write("Root.hlsl", "#include \"UnityCG.cginc\"");

            var graph = _resolver.Resolve(new[] { PathOf("Root.hlsl") });
            var include = graph.GetSource(PathOf("Root.hlsl")).Includes[0];

            Assert.AreEqual(1, graph.Sources.Count);
            Assert.IsTrue(include.IsExternal);
            Assert.AreEqual("UnityCG.cginc", include.Path);
        }

        [Test]
        public void IncludePosition_ShouldIdentifyOriginalPath()
        {
            Write("Root.hlsl", "    #include \"Child.hlsl\"\n");
            Write("Child.hlsl", "float Value = 1;");

            var graph = _resolver.Resolve(new[] { PathOf("Root.hlsl") });
            var source = graph.GetSource(PathOf("Root.hlsl"));
            var include = source.Includes[0];

            Assert.AreEqual("Child.hlsl", source.Source.Substring(include.StartIndex, include.Length));
        }

        [Test]
        public void UnsupportedInclude_ShouldThrow()
        {
            Write("Root.hlsl", "#include SOME_MACRO");

            var exception = Assert.Throws<InvalidOperationException>(() => _resolver.Resolve(new[] { PathOf("Root.hlsl") }));

            StringAssert.Contains("Unsupported Shader include directive", exception.Message);
        }

        static void Write(string name, string content)
        {
            File.WriteAllText(PathOf(name), content);
        }

        static string PathOf(string name)
        {
            return $"{TestDirectory}/{name}";
        }
    }
}