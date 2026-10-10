using System.IO;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Editor.Shaders.Artifacts;
using UnityEditor;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class ShaderArtifactPipelineTests
    {
        const string TestDirectory = "Assets/FluxFX/Tests/TempShaderPipeline";
        const string ModulePath = TestDirectory + "/TestModule.hlsl";

        ShaderArtifactBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TestDirectory);
            _builder = new ShaderArtifactBuilder();
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
        public void SameInput_ShouldProduceSameArtifactId()
        {
            File.WriteAllText(ModulePath, "float TestValue = 1;");

            var first = Build();
            var second = Build();

            Assert.AreEqual(first.ArtifactId, second.ArtifactId);
            Assert.AreEqual(first.Source, second.Source);
        }

        [Test]
        public void ChangedModule_ShouldCreateDifferentArtifact()
        {
            File.WriteAllText(ModulePath, "float TestValue = 1;");
            var first = Build();

            File.WriteAllText(ModulePath, "float TestValue = 2;");
            var second = Build();

            Assert.AreNotEqual(first.ArtifactId, second.ArtifactId);
            Assert.AreNotEqual(first.Includes.Get(ModulePath).AssetPath, second.Includes.Get(ModulePath).AssetPath);
        }

        [Test]
        public void OldArtifact_ShouldRetainOriginalInclude()
        {
            File.WriteAllText(ModulePath, "float TestValue = 1;");
            var first = Build();
            var oldInclude = first.Includes.Get(ModulePath);

            File.WriteAllText(ModulePath, "float TestValue = 2;");
            var second = Build();

            Assert.AreEqual("float TestValue = 1;", oldInclude.Source);
            StringAssert.Contains(oldInclude.AssetPath, first.Source);
            Assert.IsFalse(first.Source.Contains(second.Includes.Get(ModulePath).AssetPath));
        }

        [Test]
        public void ShaderSource_ShouldNotReferenceMutableModule()
        {
            File.WriteAllText(ModulePath, "float TestValue = 1;");

            var artifact = Build();

            Assert.IsFalse(artifact.Source.Contains($"#include \"{ModulePath}\""));
            StringAssert.Contains(artifact.Includes.Get(ModulePath).AssetPath, artifact.Source);
        }

        ShaderArtifact Build()
        {
            var source = "Shader \"{{SHADER_NAME}}\"\n" +
                         "{\n" +
                         "    SubShader\n" +
                         "    {\n" +
                         "        Pass\n" +
                         "        {\n" +
                         "            HLSLPROGRAM\n" +
                         $"            #include \"{ModulePath}\"\n" +
                         "            ENDHLSL\n" +
                         "        }\n" +
                         "    }\n" +
                         "}\n";

            var draft = new FluxParticleShaderDraft(
                TpLab.Flux.FX.Scripts.Modules.FluxParticleExecutionStage.VelocityUpdate,
                source);

            return _builder.Build(draft);
        }
    }
}