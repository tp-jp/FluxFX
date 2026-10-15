using System;
using System.IO;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleGenerationSessionTests
    {
        [Test]
        public void FailedSavePreservesManifestAndCanBeRetriedAfterUnlocking()
        {
            using (var session = FluxParticleGenerationSession.Begin("Test"))
            {
                var original = File.ReadAllText(session.ManifestPath);
                using (var locked = new FileStream(session.ManifestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Assert.Throws<IOException>(() => session.RecordFailure("Expected persistence test failure"));
                    Assert.AreEqual(original, File.ReadAllText(session.ManifestPath));
                    Assert.AreEqual("Running", FluxParticleGenerationSession.Load(session.Id).State);
                }
                session.RecordFailure("Expected persistence test failure");
                session.Dispose();
                Assert.AreEqual("Failed", FluxParticleGenerationSession.Load(session.Id).State);
                Assert.IsFalse(File.Exists(session.ManifestPath + ".tmp"));
            }
        }

        [Test]
        public void DisposeUsesFailureRecordedByAnotherHandle()
        {
            using (var session = FluxParticleGenerationSession.Begin("Test"))
            {
                FluxParticleGenerationSession.Load(session.Id).RecordFailure("Expected shared handle failure");
                session.Dispose();
                Assert.AreEqual("Failed", FluxParticleGenerationSession.Load(session.Id).State);
            }
        }

        [TestCase("Packages/Generated")]
        [TestCase("Assets/../Other")]
        [TestCase("Assets/FluxFX/../../Other")]
        [TestCase("Assets//Generated")]
        public void RejectsInvalidOutputRoots(string root)
        {
            Assert.Throws<ArgumentException>(() => FluxParticleGenerationSession.Begin("Test", root));
        }

        [Test]
        public void SharedCacheAndSessionMaterialsHaveSeparateLifetimesWithoutDeletingAssets()
        {
            var root = "Assets/FluxFX/Tests/Generation_" + Guid.NewGuid().ToString("N");
            var first = FluxParticleGenerationSession.Begin("Test", root);
            var second = FluxParticleGenerationSession.Begin("Test", root);
            try
            {
                Assert.AreEqual(first.ShaderRoot, second.ShaderRoot);
                Assert.AreEqual(first.LookupRoot, second.LookupRoot);
                Assert.AreNotEqual(first.MaterialRoot, second.MaterialRoot);
                Assert.IsFalse(AssetDatabase.IsValidFolder(root));
                FluxParticleGenerationSession.EnsureFolder(first.LookupRoot);
                var path = first.LookupRoot + "/Lookup.asset";
                var texture = new Texture2D(1, 1);
                AssetDatabase.CreateAsset(texture, path);
                first.Record(path, "Lookup", "SceneA/SystemA");
                first.Record(path, "Lookup", "SceneA/SystemB");
                first.Record(path, "Lookup", "SceneA/SystemA");
                second.Record(path, "Lookup", "SceneB/SystemA");
                var manifest = JsonUtility.FromJson<FluxParticleGenerationSession.Manifest>(File.ReadAllText(first.ManifestPath));
                Assert.AreEqual("Running", manifest.state);
                Assert.AreEqual(1, manifest.assets.Count);
                Assert.AreEqual(AssetDatabase.AssetPathToGUID(path), manifest.assets[0].guid);
                CollectionAssert.AreEquivalent(new[] { "SceneA/SystemA", "SceneA/SystemB" }, manifest.assets[0].owners);
                Assert.IsNotEmpty(manifest.assets[0].lastUsedUtc);
                first.Dispose();
                Assert.AreEqual("Completed", FluxParticleGenerationSession.Load(first.Id).State);
                Assert.AreEqual("Running", FluxParticleGenerationSession.Load(second.Id).State);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                Assert.Throws<InvalidOperationException>(() => first.Record(path, "Lookup", "SceneA/SystemA"));
                second.RecordFailure("Expected test failure");
                Assert.AreEqual("Running", FluxParticleGenerationSession.Load(second.Id).State);
                FluxParticleGenerationSession.Load(second.Id).Dispose();
                Assert.AreEqual("Failed", FluxParticleGenerationSession.Load(second.Id).State);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            }
            finally
            {
                first.Dispose();
                second.Dispose();
                if (AssetDatabase.IsValidFolder(root))
                {
                    AssetDatabase.DeleteAsset(root);
                }
            }
        }

        [Test]
        public void BuildSessionIsSharedUntilWholeBuildEnds()
        {
            if (FluxParticleGenerationLifecycle.Current != null)
            {
                Assert.Ignore("A generation session is already active.");
            }
            try
            {
                FluxParticleGenerationLifecycle.BeginPlayerBuild();
                var first = FluxParticleGenerationLifecycle.Current;
                FluxParticleGenerationLifecycle.BeginPlayerBuild();
                var second = FluxParticleGenerationLifecycle.Current;
                Assert.AreEqual(first.Id, second.Id);
                Assert.AreEqual("Build", second.Purpose);
                FluxParticleGenerationLifecycle.EndPlayerBuild(false);
                Assert.IsNull(FluxParticleGenerationLifecycle.Current);
                Assert.AreEqual("Failed", FluxParticleGenerationSession.Load(first.Id).State);
            }
            finally
            {
                FluxParticleGenerationLifecycle.EndPlayerBuild(false);
            }
        }

        [Test]
        public void RestoredSessionRejectsForeignAssetsAndCanBeInterrupted()
        {
            using (var original = FluxParticleGenerationSession.Begin("Test"))
            {
                var restored = FluxParticleGenerationSession.Load(original.Id);
                Assert.AreEqual(original.MaterialRoot, restored.MaterialRoot);
                Assert.Throws<ArgumentException>(() => restored.Record("Assets/UserMaterial.mat", "Material", "test"));
                Assert.Throws<ArgumentException>(() => restored.Record(restored.LookupRoot + "/../Outside.asset", "Lookup", "test"));
                restored.Interrupt();
                Assert.AreEqual("Interrupted", FluxParticleGenerationSession.Load(original.Id).State);
            }
        }
    }
}
