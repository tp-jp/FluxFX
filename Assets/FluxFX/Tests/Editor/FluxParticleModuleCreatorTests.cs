using System;
using System.IO;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.ModuleCreation;
using UnityEditor;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleModuleCreatorTests
    {
        [TestCase("", "Example", "Assets")]
        [TestCase("class", "Example", "Assets")]
        [TestCase("1Lift", "Example", "Assets")]
        [TestCase("Lift", "Example..Fx", "Assets")]
        [TestCase("Lift", "Example.class", "Assets")]
        [TestCase("Lift", "Example", "Packages")]
        [TestCase("Lift", "Example", "Assets/../Packages")]
        [TestCase("Lift", "Example", "Assets/Editor")]
        [TestCase("Lift", "Example", "Assets/StreamingAssets")]
        [TestCase("CON", "Example", "Assets")]
        [TestCase("GetType", "Example", "Assets")]
        public void CreatorRejectsInvalidInputWithoutWriting(string name, string ns, string parent)
        {
            Assert.IsNotEmpty(FluxParticleModuleCreator.Validate(name, ns, parent));
            Assert.Throws<InvalidOperationException>(() => FluxParticleModuleCreator.Create(name, ns, parent));
        }

        [Test]
        public void CreatorPreservesExistingDestinationAndOrphanMeta()
        {
            var name = "CreatorSafety" + Guid.NewGuid().ToString("N");
            var path = "Assets/" + name;
            AssetDatabase.CreateFolder("Assets", name);
            var contents = Guid.NewGuid().ToString();
            File.WriteAllText(path + "/keep.txt", contents);
            try
            {
                Assert.Throws<InvalidOperationException>(() => FluxParticleModuleCreator.Create(name, "CreatorTests", "Assets"));
                Assert.AreEqual(contents, File.ReadAllText(path + "/keep.txt"));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
            File.WriteAllText(path + ".meta", contents);
            try
            {
                Assert.Throws<InvalidOperationException>(() => FluxParticleModuleCreator.Create(name, "CreatorTests", "Assets"));
                Assert.AreEqual(contents, File.ReadAllText(path + ".meta"));
                Assert.IsFalse(Directory.Exists(path));
            }
            finally
            {
                File.Delete(path + ".meta");
            }
        }
    }
}
