using System;
using System.IO;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Shaders.Artifacts;
using TpLab.Flux.FX.Editor.Shaders.Dependencies;
using UnityEditor;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class ShaderIncludeArtifactTests
    {
        const string TestDirectory = "Assets/FluxFX/Tests/TempShaderArtifacts";
        const string SourceDirectory = TestDirectory + "/Sources";
        const string OutputDirectory = TestDirectory + "/Generated";

        ShaderDependencyResolver _resolver;
        ShaderIncludeArtifactBuilder _builder;
        ShaderIncludeArtifactStore _store;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(SourceDirectory);

            _resolver = new ShaderDependencyResolver();
            _builder = new ShaderIncludeArtifactBuilder(OutputDirectory);
            _store = new ShaderIncludeArtifactStore();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestDirectory);

            if (Directory.Exists(TestDirectory))
            {
                Directory.Delete(TestDirectory, true);
            }
        }

        [Test]
        public void SingleSource_ShouldGenerateArtifact()
        {
            Write("Gravity.hlsl", "float Value = 1;");

            var result = Build("Gravity.hlsl");
            var artifact = result.Get(SourcePath("Gravity.hlsl"));

            Assert.AreEqual("float Value = 1;", artifact.Source);
            StringAssert.StartsWith(OutputDirectory + "/Gravity_", artifact.AssetPath);
            Assert.AreEqual(64, artifact.Hash.Length);
        }

        [Test]
        public void NestedIncludes_ShouldUseImmutablePaths()
        {
            Write("Root.hlsl", "#include \"Child.hlsl\"");
            Write("Child.hlsl", "#include \"Leaf.hlsl\"");
            Write("Leaf.hlsl", "float Value = 1;");

            var result = Build("Root.hlsl");
            var root = result.Get(SourcePath("Root.hlsl"));
            var child = result.Get(SourcePath("Child.hlsl"));
            var leaf = result.Get(SourcePath("Leaf.hlsl"));

            Assert.AreEqual(3, result.Artifacts.Count);
            StringAssert.Contains(child.AssetPath, root.Source);
            StringAssert.Contains(leaf.AssetPath, child.Source);
            Assert.IsFalse(root.Source.Contains("\"Child.hlsl\""));
            Assert.IsFalse(child.Source.Contains("\"Leaf.hlsl\""));
        }

        [Test]
        public void ChangedChild_ShouldChangeParentHash()
        {
            Write("Root.hlsl", "#include \"Child.hlsl\"");
            Write("Child.hlsl", "float Value = 1;");

            var first = Build("Root.hlsl");

            Write("Child.hlsl", "float Value = 2;");

            var second = Build("Root.hlsl");

            Assert.AreNotEqual(
                first.Get(SourcePath("Child.hlsl")).Hash,
                second.Get(SourcePath("Child.hlsl")).Hash);

            Assert.AreNotEqual(
                first.Get(SourcePath("Root.hlsl")).Hash,
                second.Get(SourcePath("Root.hlsl")).Hash);
        }

        [Test]
        public void SameContent_ShouldProduceSameArtifact()
        {
            Write("Root.hlsl", "float Value = 1;");

            var first = Build("Root.hlsl");
            var second = Build("Root.hlsl");

            Assert.AreEqual(first.Get(SourcePath("Root.hlsl")).Hash, second.Get(SourcePath("Root.hlsl")).Hash);
            Assert.AreEqual(first.Get(SourcePath("Root.hlsl")).AssetPath, second.Get(SourcePath("Root.hlsl")).AssetPath);
        }

        [Test]
        public void Store_ShouldReuseExistingArtifact()
        {
            Write("Root.hlsl", "float Value = 1;");

            var result = Build("Root.hlsl");
            var path = result.Get(SourcePath("Root.hlsl")).AssetPath;

            _store.Save(result);
            _store.Save(result);

            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual("float Value = 1;", File.ReadAllText(path));
        }

        [Test]
        public void Store_ShouldDetectModifiedArtifact()
        {
            Write("Root.hlsl", "float Value = 1;");

            var result = Build("Root.hlsl");
            var path = result.Get(SourcePath("Root.hlsl")).AssetPath;

            _store.Save(result);

            File.WriteAllText(path, "float Value = 999;");

            Assert.Throws<InvalidOperationException>(() => _store.Save(result));
        }

        [Test]
        public void ChangedSource_ShouldPreserveOldArtifact()
        {
            Write("Root.hlsl", "float Value = 1;");

            var first = Build("Root.hlsl");
            var oldPath = first.Get(SourcePath("Root.hlsl")).AssetPath;

            _store.Save(first);

            Write("Root.hlsl", "float Value = 2;");

            var second = Build("Root.hlsl");
            var newPath = second.Get(SourcePath("Root.hlsl")).AssetPath;

            _store.Save(second);

            Assert.AreNotEqual(oldPath, newPath);
            Assert.AreEqual("float Value = 1;", File.ReadAllText(oldPath));
            Assert.AreEqual("float Value = 2;", File.ReadAllText(newPath));
        }

        [Test]
        public void SharedDependency_ShouldReuseSameArtifact()
        {
            Write("A.hlsl", "#include \"Shared.hlsl\"");
            Write("B.hlsl", "#include \"Shared.hlsl\"");
            Write("Shared.hlsl", "float SharedValue = 1;");

            var graph = _resolver.Resolve(new[] { SourcePath("A.hlsl"), SourcePath("B.hlsl") });
            var result = _builder.Build(graph);
            var shared = result.Get(SourcePath("Shared.hlsl"));

            Assert.AreEqual(3, result.Artifacts.Count);
            StringAssert.Contains(shared.AssetPath, result.Get(SourcePath("A.hlsl")).Source);
            StringAssert.Contains(shared.AssetPath, result.Get(SourcePath("B.hlsl")).Source);
        }

        ShaderIncludeArtifactSet Build(string name)
        {
            var graph = _resolver.Resolve(new[] { SourcePath(name) });
            return _builder.Build(graph);
        }

        static void Write(string name, string content)
        {
            File.WriteAllText(SourcePath(name), content);
        }

        static string SourcePath(string name)
        {
            return $"{SourceDirectory}/{name}";
        }
    }
}