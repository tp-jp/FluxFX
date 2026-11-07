using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Inspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticlePrefabDeletionTests
    {
        string _folder;
        string _basePath;
        Object[] _previousSelection;

        [UnityTest]
        public IEnumerator DeletingDirtyVariantRemovesAssetAndMeta()
        {
            var path = _folder + "/Variant.prefab";
            yield return CreateVariant(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var backing = prefab.GetComponent<UdonBehaviour>();
            Assert.IsNotNull(backing);
            EditorUtility.SetDirty(backing);
            Assert.IsTrue(EditorUtility.IsDirty(backing));
            Assert.IsTrue(AssetDatabase.DeleteAsset(path));
            yield return FluxParticleEditorTestWait.ForDelayCall();
            Assert.IsFalse(File.Exists(path));
            Assert.IsFalse(File.Exists(path + ".meta"));
        }

        [UnityTest]
        public IEnumerator RejectedDeletionRestoresDirtyStateWithoutChangingBasePrefab()
        {
            var path = _folder + "/Variant.prefab";
            yield return CreateVariant(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var backing = prefab.GetComponent<UdonBehaviour>();
            var parent = AssetDatabase.LoadAssetAtPath<GameObject>(_basePath);
            var parentBacking = parent.GetComponent<UdonBehaviour>();
            var bytes = File.ReadAllBytes(path);
            EditorUtility.SetDirty(parentBacking);
            EditorUtility.SetDirty(backing);
            var parentDirtyCount = EditorUtility.GetDirtyCount(parentBacking);
            var handle = PrefabUtility.GetPrefabInstanceHandle(prefab);
            Assert.IsNotNull(handle);
            EditorUtility.SetDirty(handle);

            Assert.AreEqual(AssetDeleteResult.DidNotDelete, NotifyDeletion(path));
            Assert.IsFalse(EditorUtility.IsDirty(backing));
            Assert.IsFalse(EditorUtility.IsDirty(handle));
            Assert.IsTrue(EditorUtility.IsDirty(parentBacking));
            Assert.AreEqual(parentDirtyCount, EditorUtility.GetDirtyCount(parentBacking));
            // 他のAssetModificationProcessor等が削除を拒否した場合を再現する。
            yield return FluxParticleEditorTestWait.ForDelayCall();
            Assert.IsTrue(EditorUtility.IsDirty(backing));
            Assert.IsTrue(EditorUtility.IsDirty(handle));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
        }

        [Test]
        public void UnrelatedPrefabKeepsItsUnsavedChanges()
        {
            var path = _folder + "/Unrelated.prefab";
            var instance = new GameObject("Unrelated");
            GameObject prefab;
            try
            {
                prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
            prefab.name = "Unsaved change";
            EditorUtility.SetDirty(prefab);
            var count = EditorUtility.GetDirtyCount(prefab);
            Assert.AreEqual(AssetDeleteResult.DidNotDelete, NotifyDeletion(path));
            Assert.IsTrue(EditorUtility.IsDirty(prefab));
            Assert.AreEqual(count, EditorUtility.GetDirtyCount(prefab));
            Assert.AreEqual("Unsaved change", prefab.name);
        }

        [SetUp]
        public void SetUp()
        {
            _previousSelection = Selection.objects;
            Selection.objects = Array.Empty<Object>();
            _folder = "Assets/FluxFX/Tests/PrefabDeletion_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/FluxFX/Tests", Path.GetFileName(_folder));
            _basePath = _folder + "/Base.prefab";
            Assert.IsTrue(AssetDatabase.CopyAsset("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab", _basePath));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Selection.objects = _previousSelection;
            yield return FluxParticleEditorTestWait.ForDelayCall();
            AssetDatabase.DeleteAsset(_folder);
        }

        IEnumerator CreateVariant(string path)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(_basePath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                yield return FluxParticleEditorTestWait.ForDelayCall();
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                yield return FluxParticleEditorTestWait.ForDelayCall();
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
            yield return FluxParticleEditorTestWait.ForDelayCall();
        }

        static AssetDeleteResult NotifyDeletion(string path)
        {
            var type = typeof(FluxParticleAuthoringEditor).Assembly.GetType(
                "TpLab.Flux.FX.Editor.Inspector.FluxParticlePrefabDeletionGuard", true);
            return (AssetDeleteResult)type.GetMethod("OnWillDeleteAsset", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { path, RemoveAssetOptions.MoveAssetToTrash });
        }
    }
}
