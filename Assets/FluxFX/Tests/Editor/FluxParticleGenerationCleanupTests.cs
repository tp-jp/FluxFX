using System;
using System.IO;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleGenerationCleanupTests
    {
        string root;
        string materialPath;
        string lookupPath;
        FluxParticleGenerationSession session;
        GameObject instance;

        [SetUp]
        public void SetUp()
        {
            root = "Assets/FluxFX/Tests/Cleanup_" + Guid.NewGuid().ToString("N");
            session = FluxParticleGenerationSession.Begin("Test", root);
            FluxParticleGenerationSession.EnsureFolder(session.MaterialRoot);
            FluxParticleGenerationSession.EnsureFolder(session.LookupRoot);
            lookupPath = session.LookupRoot + "/Lookup.asset";
            materialPath = session.MaterialRoot + "/Render.mat";
            var texture = new Texture2D(1, 1);
            AssetDatabase.CreateAsset(texture, lookupPath);
            var material = new Material(Shader.Find("Unlit/Texture")) { mainTexture = texture };
            AssetDatabase.CreateAsset(material, materialPath);
            session.Record(lookupPath, "Lookup", "test");
            session.Record(materialPath, "Material", "test");
            AssetDatabase.SaveAssets();
        }

        [TearDown]
        public void TearDown()
        {
            if (instance != null)
            {
                Object.DestroyImmediate(instance);
            }
            session?.Dispose();
            AssetDatabase.DeleteAsset(root);
        }

        void CreateRenderer()
        {
            instance = new GameObject("Cleanup reference", typeof(MeshRenderer));
            instance.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }

        void AssertAssetsRemain()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(materialPath));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(lookupPath));
        }

        [Test]
        public void TrailMeshCacheRemainsReferencedAndIsCollectedAfterRelease()
        {
            FluxParticleGenerationSession.EnsureFolder(session.MeshRoot);
            var path = session.MeshRoot + "/Trail.asset";
            var mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, path);
            session.Record(path, "Mesh", "test");
            CreateRenderer();
            instance.AddComponent<MeshFilter>().sharedMesh = mesh;
            session.Dispose();
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Mesh>(path));
            Object.DestroyImmediate(instance);
            instance = null;
            Assert.AreEqual(3, FluxParticleGenerationCleanup.Collect(root));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Mesh>(path));
        }

        [Test]
        public void RunningSessionProtectsAssetsUntilItEnds()
        {
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
            AssertAssetsRemain();
            session.Dispose();
            Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(root));
            Assert.IsFalse(AssetDatabase.IsValidFolder(session.MaterialRoot));
            Assert.IsFalse(File.Exists(materialPath));
            Assert.IsFalse(File.Exists(lookupPath));
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
        }

        [Test]
        public void UnsavedSceneReferenceProtectsMaterialAndItsLookup()
        {
            CreateRenderer();
            session.Dispose();
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
            AssertAssetsRemain();
            Object.DestroyImmediate(instance);
            instance = null;
            Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(root));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedAndInterruptedSessionsReleaseUnusedAssets(bool interrupted)
        {
            if (interrupted)
            {
                session.Interrupt();
            }
            else
            {
                session.RecordFailure("Expected cleanup test failure");
                session.Dispose();
            }
            Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(root));
        }

        [Test]
        public void SavedPrefabProtectsMaterialAndItsLookup()
        {
            CreateRenderer();
            var prefabPath = root + "/Referenced.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            instance = null;
            session.Dispose();
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
            AssertAssetsRemain();
            AssetDatabase.DeleteAsset(prefabPath);
            Assert.AreEqual(2, FluxParticleGenerationCleanup.Collect(root));
        }

        [Test]
        public void UnrecordedAndReplacedAssetsAreNotDeleted()
        {
            session.Dispose();
            var originalGuid = AssetDatabase.AssetPathToGUID(materialPath);
            AssetDatabase.DeleteAsset(materialPath);
            var replacement = new Material(Shader.Find("Unlit/Texture"))
            {
                mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(lookupPath)
            };
            var replacementPath = root + "/Replacement.mat";
            AssetDatabase.CreateAsset(replacement, replacementPath);
            Assert.AreNotEqual(originalGuid, AssetDatabase.AssetPathToGUID(replacementPath));
            Assert.IsEmpty(AssetDatabase.MoveAsset(replacementPath, materialPath));
            Assert.AreNotEqual(originalGuid, AssetDatabase.AssetPathToGUID(materialPath));
            var unrecorded = session.MaterialRoot + "/User.mat";
            AssetDatabase.CreateAsset(new Material(replacement), unrecorded);
            AssetDatabase.SaveAssets();
            Assert.AreEqual(0, FluxParticleGenerationCleanup.Collect(root));
            AssertAssetsRemain();
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(unrecorded));
        }

        [Test]
        public void InvalidManifestAbortsBeforeDeletingAssets()
        {
            session.Dispose();
            var saved = File.ReadAllText(session.ManifestPath);
            try
            {
                File.WriteAllText(session.ManifestPath, "{}");
                Assert.Throws<InvalidOperationException>(() => FluxParticleGenerationCleanup.Collect(root));
                AssertAssetsRemain();
            }
            finally
            {
                File.WriteAllText(session.ManifestPath, saved);
            }
        }
    }
}
