using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    // このテスト自体がFluxFX Editorとは別のasmdefから公開APIを使用します。
    public sealed class FluxParticleShaderStoreTests
    {
        string _root;
        string _source;
        FluxParticleGenerationSession _session;
        Material _material;
        GameObject _instance;

        [SetUp]
        public void SetUp()
        {
            _root = FluxParticleTestPaths.TemporaryRoot + "/ShaderStore_" + Guid.NewGuid().ToString("N");
            FluxParticleGenerationSession.EnsureFolder(_root);
            _session = FluxParticleGenerationSession.Begin("ShaderStoreTest", _root);
            File.WriteAllText(_root + "/Root.hlsl", "#include \"Child.hlsl\"");
            File.WriteAllText(_root + "/Child.hlsl", "#include \"Lift.hlsl\"");
            File.WriteAllText(_root + "/Lift.hlsl", File.ReadAllText(
                "Assets/FluxFX/Samples/Examples/CustomLift/Editor/CustomLift.hlsl"));
            _source = "Shader \"Hidden/FluxFX/ShaderStoreTest\" { SubShader { Pass { HLSLPROGRAM\n" +
                "#pragma vertex vert_img\n#pragma fragment frag\n#include \"UnityCG.cginc\"\n" +
                "struct FluxCustomLiftContext { float parameterCustomLiftStrength; float3 parameterCustomLiftDirection; " +
                "float valueStepTime; float valueStepDeltaTime; float3 valueParticleVelocity; };\n" +
                "#include \"" + _root + "/Root.hlsl\"\n" +
                "float4 frag(v2f_img i) : SV_Target { FluxCustomLiftContext ctx = (FluxCustomLiftContext)0; " +
                "ctx.parameterCustomLiftStrength = 2; ctx.parameterCustomLiftDirection = float3(1,0,0); " +
                "ctx.valueStepDeltaTime = 1; /* scale */ FluxFX_CustomLift(ctx); return float4(ctx.valueParticleVelocity,1); }\nENDHLSL\n} } }";
        }

        [TearDown]
        public void TearDown()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
            if (_material != null && !EditorUtility.IsPersistent(_material)) Object.DestroyImmediate(_material);
            _session?.Dispose();
            AssetDatabase.DeleteAsset(_root);
            FluxParticleTestPaths.RemoveEmptyGenerationLedger(_root);
        }

        [Test]
        public void PublicApiReusesContentAndRecordsEveryOwnerAndDependency()
        {
            var shader = Save("first");
            var path = AssetDatabase.GetAssetPath(shader);
            var guid = AssetDatabase.AssetPathToGUID(path);
            Assert.AreSame(shader, Save("second"));
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            Assert.AreEqual("Running", FluxParticleGenerationSession.Load(_session.Id).State);
            var assets = Manifest().assets;
            var record = assets.Single(asset => asset.kind == "Shader");
            CollectionAssert.AreEquivalent(new[] { "first", "second" }, record.owners);
            Assert.AreEqual(3, record.dependencies.Count);
            Assert.AreEqual(4, assets.Count);
            Assert.IsTrue(assets.All(asset => !string.IsNullOrEmpty(asset.guid)));
            Assert.AreEqual(2, assets.Where(asset => asset.kind == "ShaderInclude").Sum(asset => asset.dependencies.Count));
        }

        [TestCase("Child.hlsl")]
        [TestCase("Lift.hlsl")]
        public void ChildAndGrandchildChangesProduceNewShaders(string dependency)
        {
            var oldPath = AssetDatabase.GetAssetPath(Save());
            File.AppendAllText(_root + "/" + dependency, "\n// changed dependency\n");
            Assert.AreNotEqual(oldPath, AssetDatabase.GetAssetPath(Save()));
        }

        [Test]
        public void ContextAndHlslCanEvolveAndOriginalSourcesCanBeMovedAndDeleted()
        {
            var oldShader = Save();
            var oldPath = AssetDatabase.GetAssetPath(oldShader);
            var oldPixels = Render(oldShader);
            Assert.AreEqual(1, oldPixels.r, 0.0001f);
            var lift = File.ReadAllText(_root + "/Lift.hlsl");
            File.WriteAllText(_root + "/Lift.hlsl", lift.Replace("float strength =", "float strength = ctx.valueSystemEffectScale *"));
            _source = _source.Replace("float valueStepTime;", "float valueSystemEffectScale; float valueStepTime;")
                .Replace("/* scale */", "ctx.valueSystemEffectScale = 0.25;");
            var newShader = Save();
            var newPath = AssetDatabase.GetAssetPath(newShader);
            Assert.AreNotEqual(oldPath, newPath);
            Assert.AreEqual(0.25f, Render(newShader).r, 0.0001f);
            AssetDatabase.ImportAsset(_root + "/Lift.hlsl");
            Assert.IsEmpty(AssetDatabase.MoveAsset(_root + "/Lift.hlsl", _root + "/Moved.hlsl"));
            AssetDatabase.DeleteAsset(_root + "/Moved.hlsl");
            File.Delete(_root + "/Child.hlsl");
            File.Delete(_root + "/Root.hlsl");
            foreach (var path in new[] { oldPath, newPath })
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                Assert.AreEqual(path == oldPath ? 1 : 0.25f, Render(shader).r, 0.0001f);
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
            }
            Directory.CreateDirectory("Temp/ShaderStore");
            File.WriteAllText("Temp/ShaderStore/ContextComparison.txt",
                "Custom Lift: old red=1; new red=0.25; tolerance=0.0001. Both unchanged after original include move/deletion and forced reimport.");
        }

        [TestCase("#include MISSING_MACRO", "Unsupported")]
        [TestCase("#include \"Missing.hlsl\"", "Missing.hlsl")]
        [TestCase("#include \"Root.hlsl\"", "Circular")]
        public void DependencyFailureReportsOwnerAndSourceWithoutChangingReferences(string dependency, string expected)
        {
            var shader = Save();
            _material = new Material(shader);
            File.WriteAllText(_root + "/Child.hlsl", dependency);
            var error = Assert.Throws<InvalidOperationException>(() => Save("external-owner"));
            StringAssert.Contains(expected, error.Message);
            StringAssert.Contains("external-owner", error.Message);
            StringAssert.Contains(_root + "/Child.hlsl", error.Message);
            StringAssert.Contains(_session.ShaderRoot, error.Message);
            Assert.AreSame(shader, _material.shader);
            Assert.AreEqual("Running", Manifest().state);
            Assert.IsNotEmpty(Manifest().error);
        }

        [Test]
        public void CompileFailureIsRecordedAndUnreferencedPartialAssetsCanBeCollected()
        {
            var shader = Save();
            _material = new Material(shader);
            File.WriteAllText(_root + "/Lift.hlsl", "void FluxFX_CustomLift(inout FluxCustomLiftContext ctx) { ctx.missingValue = 1; }");
            var ignore = LogAssert.ignoreFailingMessages;
            try
            {
                LogAssert.ignoreFailingMessages = true;
                var error = Assert.Throws<InvalidOperationException>(() => Save("compile-owner"));
                StringAssert.Contains("compile-owner", error.Message);
                StringAssert.Contains("missingValue", error.Message);
                StringAssert.Contains(_root + "/Lift.hlsl", error.Message);
                StringAssert.Contains(".shader", error.Message);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = ignore;
            }
            Assert.AreSame(shader, _material.shader);
            Assert.AreEqual(2, Manifest().assets.Count(asset => asset.kind == "Shader"));
            _session.Dispose();
            Assert.AreEqual("Failed", Manifest().state);
            Assert.Greater(FluxParticleGenerationCleanup.Collect(_root), 0);
            Assert.IsNotNull(_material.shader);
            Assert.IsTrue(File.Exists(AssetDatabase.GetAssetPath(shader)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ChangedContentAtGeneratedPathIsNotOverwritten(bool include)
        {
            var shader = Save();
            var path = include ? Manifest().assets.First(asset => asset.kind == "ShaderInclude").path : AssetDatabase.GetAssetPath(shader);
            var original = File.ReadAllText(path);
            File.WriteAllText(path, original + "\n// user modification");
            try
            {
                StringAssert.Contains(path, Assert.Throws<InvalidOperationException>(() => Save()).Message);
                Assert.AreEqual(original + "\n// user modification", File.ReadAllText(path));
            }
            finally
            {
                File.WriteAllText(path, original);
            }
        }

        [Test]
        public void SharedSessionAndUnsavedSceneProtectShaderAndIncludes()
        {
            var shader = Save();
            using (var second = FluxParticleGenerationSession.Begin("SecondOwner", _root))
            {
                Assert.AreSame(shader, FluxParticleShaderStore.Save(_source, second, "second"));
                CreateMaterial();
                _session.Dispose();
                Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(_root));
                _instance = new GameObject("Unsaved shader reference", typeof(MeshRenderer));
                _instance.GetComponent<MeshRenderer>().sharedMaterial = _material;
            }
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(_root));
            Object.DestroyImmediate(_instance);
            _instance = null;
            Assert.AreEqual(5, FluxParticleGenerationCleanup.Collect(_root));
        }

        [Test]
        public void UnsavedPrefabStageProtectsShaderAndIncludes()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) Assert.Ignore("Do not replace the user's Prefab Stage.");
            var shader = Save();
            _instance = new GameObject("Shader Store Prefab", typeof(MeshRenderer));
            var prefabPath = _root + "/Stage.prefab";
            PrefabUtility.SaveAsPrefabAsset(_instance, prefabPath);
            Object.DestroyImmediate(_instance);
            _instance = null;
            CreateMaterial();
            _session.Dispose();
            var stage = PrefabStageUtility.OpenPrefab(prefabPath);
            try
            {
                stage.prefabContentsRoot.GetComponent<MeshRenderer>().sharedMaterial = _material;
                EditorSceneManager.MarkSceneDirty(stage.scene);
                Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(_root));
            }
            finally
            {
                stage.prefabContentsRoot.GetComponent<MeshRenderer>().sharedMaterial = null;
                stage.ClearDirtiness();
                StageUtility.GoToMainStage();
            }
            Assert.AreEqual(5, FluxParticleGenerationCleanup.Collect(_root));
        }

        [Test]
        public void IncludeWriteFailureStillRecordsAlreadyWrittenFiles()
        {
            var snapshot = FluxParticleShaderSnapshot.Create(_source, _session.ShaderRoot, File.ReadAllText);
            var paths = snapshot.IncludeSources.Keys.ToArray();
            FluxParticleGenerationSession.EnsureFolder(_session.ShaderRoot);
            // 後続includeの保存先をディレクトリで塞ぎ、先行includeの書き込み後に失敗させます。
            Directory.CreateDirectory(paths.Last());
            Assert.Throws<InvalidOperationException>(() => Save());
            var records = Manifest().assets;
            Assert.IsNotEmpty(records);
            Assert.IsTrue(records.All(asset => asset.kind == "ShaderInclude" && !string.IsNullOrEmpty(asset.guid)));
            Assert.IsTrue(records.All(asset => File.Exists(asset.path)));
            _session.Dispose();
            Assert.AreEqual(records.Count, FluxParticleGenerationCleanup.Collect(_root));
        }

        [Test]
        public void EndedSessionCannotGenerateOrChangeItsState()
        {
            _session.Dispose();
            Assert.Throws<InvalidOperationException>(() => Save());
            Assert.AreEqual("Completed", Manifest().state);
            Assert.IsFalse(Directory.Exists(_session.ShaderRoot));
        }

        Shader Save(string owner = "custom-lift-test") => FluxParticleShaderStore.Save(_source, _session, owner);

        FluxParticleGenerationSession.Manifest Manifest() =>
            JsonUtility.FromJson<FluxParticleGenerationSession.Manifest>(File.ReadAllText(_session.ManifestPath));

        void CreateMaterial()
        {
            _material = new Material(Save());
            FluxParticleGenerationSession.EnsureFolder(_session.MaterialRoot);
            var path = _session.MaterialRoot + "/Reference.mat";
            AssetDatabase.CreateAsset(_material, path);
            _session.Record(path, "Material", "test");
        }

        static Color Render(Shader shader)
        {
            var material = new Material(shader);
            var target = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var texture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                ShaderUtil.CompilePass(material, 0, true);
                Graphics.Blit(Texture2D.blackTexture, target, material);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                return texture.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(material);
            }
        }
    }
}
