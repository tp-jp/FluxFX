using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleShaderIncludeStoreTests
    {
        string _root;
        string _source;
        FluxParticleGenerationSession _session;
        GameObject _instance;

        [SetUp]
        public void SetUp()
        {
            _root = FluxParticleTestPaths.TemporaryRoot + "/Includes_" + Guid.NewGuid().ToString("N");
            FluxParticleGenerationSession.EnsureFolder(_root);
            _session = FluxParticleGenerationSession.Begin("Test", _root);
            File.WriteAllText(_root + "/Child.hlsl", "float4 TestColor() { return float4(1, 0, 0, 1); }");
            File.WriteAllText(_root + "/Root.hlsl", "#include \"Child.hlsl\"");
            _source = "Shader \"Hidden/FluxFX/IncludeSnapshotTest\" { SubShader { Pass { HLSLPROGRAM\n" +
                "#pragma vertex vert_img\n#pragma fragment frag\n#include \"UnityCG.cginc\"\n" +
                "#include \"" + _root + "/Root.hlsl\"\nfloat4 frag(v2f_img i) : SV_Target { return TestColor(); }\nENDHLSL\n} } }";
        }

        [TearDown]
        public void TearDown()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
            _session?.Dispose();
            AssetDatabase.DeleteAsset(_root);
        }

        [Test]
        public void SourceChangesCannotBreakPreviouslyGeneratedShader()
        {
            var shader = SaveShader();
            var oldPath = AssetDatabase.GetAssetPath(shader);
            var oldSource = File.ReadAllText(oldPath);
            File.WriteAllText(_root + "/Child.hlsl", "invalid HLSL deliberately replacing the previous function");
            AssetDatabase.ImportAsset(_root + "/Child.hlsl", ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(oldPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Assert.IsFalse(ShaderUtil.ShaderHasError(AssetDatabase.LoadAssetAtPath<Shader>(oldPath)));
            Assert.AreEqual(oldSource, File.ReadAllText(oldPath));
            File.WriteAllText(_root + "/Child.hlsl", "float4 TestColor() { return float4(0, 1, 0, 1); }");
            Assert.AreNotEqual(oldPath, AssetDatabase.GetAssetPath(SaveShader()));
        }

        [Test]
        public void SharedIncludesAreRecordedForEverySession()
        {
            var first = FluxParticleShaderIncludeStore.Save(_source, _session, "first");
            using (var second = FluxParticleGenerationSession.Begin("Test", _root))
            {
                Assert.AreEqual(first.Source, FluxParticleShaderIncludeStore.Save(_source, second, "second").Source);
                Assert.AreEqual(2, Directory.GetFiles(_session.ShaderRoot, "*.hlsl").Length);
                StringAssert.Contains("ShaderInclude", File.ReadAllText(_session.ManifestPath));
                StringAssert.Contains("ShaderInclude", File.ReadAllText(second.ManifestPath));
                _session.Dispose();
                Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(_root));
            }
            Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(_root));
        }

        [Test]
        public void ModifiedSnapshotIsRejectedWithoutOverwriting()
        {
            FluxParticleShaderIncludeStore.Save(_source, _session, "test");
            var path = Directory.GetFiles(_session.ShaderRoot, "*.hlsl").First();
            File.WriteAllText(path, "modified");
            Assert.Throws<InvalidOperationException>(() => FluxParticleShaderIncludeStore.Save(_source, _session, "test"));
            Assert.AreEqual("modified", File.ReadAllText(path));
        }

        [Test]
        public void LiveMaterialProtectsShaderAndTransitiveIncludes()
        {
            var shader = SaveShader();
            FluxParticleGenerationSession.EnsureFolder(_session.MaterialRoot);
            var materialPath = _session.MaterialRoot + "/Test.mat";
            var material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
            _session.Record(materialPath, "Material", "test");
            _instance = new GameObject("Snapshot reference", typeof(MeshRenderer));
            _instance.GetComponent<MeshRenderer>().sharedMaterial = material;
            _session.Dispose();
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(_root));
            Object.DestroyImmediate(_instance);
            _instance = null;
            Assert.AreEqual(4, FluxParticleGenerationCleanup.Collect(_root));
        }

        [Test]
        public void PackageDependenciesUseResolvedPackageLocation()
        {
            var result = FluxParticleShaderIncludeStore.Save(
                "#include \"Packages/com.tplab.flux.fx/Editor/Core/Hlsl/VelocityIdentity.hlsl\"", _session, "test");
            StringAssert.DoesNotContain("Packages/", result.Source);
            Assert.IsNotEmpty(Directory.GetFiles(_session.ShaderRoot, "*.hlsl"));
        }

        Shader SaveShader()
        {
            var method = typeof(FluxParticleProgramBuild).GetMethod("SaveShader", BindingFlags.Static | BindingFlags.NonPublic);
            return (Shader)method.Invoke(null, new object[] { _source, _session, "test" });
        }
    }
}
