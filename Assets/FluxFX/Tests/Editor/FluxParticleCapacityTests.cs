using System;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using UnityEngine;
using UdonSharp;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleCapacityTests
    {
        [TestCase(typeof(FluxParticleRenderer), "get_MaximumParticleCount")]
        [TestCase(typeof(FluxParticleEmitter), "get_HasCompleted")]
        [TestCase(typeof(FluxBuffer), "Release")]
        [TestCase(typeof(FluxKernel), "__0_SetTexture")]
        public void CrossBehaviourApisAreExportedByCompiledUdonProgram(Type behaviourType, string entryPoint)
        {
            var asset = UdonSharpProgramAsset.GetAllUdonSharpPrograms()
                .SingleOrDefault(candidate => candidate.sourceCsScript != null && candidate.sourceCsScript.GetClass() == behaviourType);
            Assert.IsNotNull(asset, "UdonSharp Program Asset is required for " + behaviourType.Name);
            var program = asset.SerializedProgramAsset.RetrieveProgram();
            Assert.IsNotNull(program, "Compile UdonSharp programs before running this test.");
            CollectionAssert.Contains(program.EntryPoints.GetExportedSymbols().ToArray(), entryPoint);
        }

        [Test]
        public void RuntimeCapacityUsesNumericPrecisionAndMeshArrayDimensions()
        {
            Assert.AreEqual(FluxParticleCapacity.MaximumExactCount, FluxParticleCapacity.GetMaximumCount(null));
            var mesh = new Mesh();
            try
            {
                mesh.vertices = new Vector3[1000];
                mesh.triangles = new[] { 0, 1, 2 };
                Assert.AreEqual(int.MaxValue / 1000, FluxParticleCapacity.GetMaximumCount(mesh));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [TestCase(1, 1)]
        [TestCase(4, 2)]
        [TestCase(5, 3)]
        [TestCase(65, 5)]
        [TestCase(1048576, 11)]
        [TestCase(16777216, 13)]
        public void FreeSlotHierarchyFitsSamplerBudget(int count, int expectedLevels)
        {
            Assert.AreEqual(expectedLevels, FluxParticleSpawnAllocator.GetLevelCount(count));
        }

        [TestCase(0, 0)]
        [TestCase(1024, 1048576)]
        [TestCase(2048, 4194304)]
        [TestCase(4096, 16777216)]
        [TestCase(16384, 16777216)]
        [TestCase(int.MaxValue, 16777216)]
        public void StateCapacityUsesTextureDimensionsAndExactIntegerRange(int textureSize, int expected)
        {
            Assert.AreEqual(expected, FluxParticleCapacity.GetMaximumStateCount(textureSize));
        }

        [TestCase(4096, true, 4, 6, 16777216)]
        [TestCase(1024, true, 4, 6, 1048576)]
        [TestCase(4096, false, 4, 6, 16383)]
        [TestCase(4096, false, 100, 300, 655)]
        [TestCase(4096, true, 1000, 3, 2147483)]
        [TestCase(4096, true, 4, 1000, 2147483)]
        [TestCase(4096, true, 0, 6, 0)]
        [TestCase(4096, true, 4, 0, 0)]
        public void RenderCapacityIncludesMeshDimensionsAndIndexSupport(int textureSize, bool supports32,
            int vertices, int indices, int expected)
        {
            Assert.AreEqual(expected, FluxParticleCapacity.GetMaximumCount(textureSize, supports32, vertices, indices));
        }

        [TestCase(4, 6, 1024L, 28, 9)]
        [TestCase(1, 100, 1000L, 4, 5)]
        [TestCase(4, 6, 134217728L, 28, 1198372)]
        [TestCase(4, 6, 0L, 28, 0)]
        public void GraphicsBufferByteLimitsIncludeVertexStrideAndIndexWidth(int vertices, int indices,
            long maxBytes, int stride, int expected)
        {
            Assert.AreEqual(expected, FluxParticleCapacity.GetMaximumCount(4096, true, vertices, indices, maxBytes, stride));
        }

        [TestCase(1, 1)]
        [TestCase(4097, 65)]
        [TestCase(16769025, 4095)]
        [TestCase(16769026, 4096)]
        [TestCase(16777216, 4096)]
        public void TextureWidthIncludesSlotImmediatelyAfterLargePerfectSquare(int count, int expected)
        {
            var width = FluxParticleCapacity.GetTextureWidth(count);
            Assert.AreEqual(expected, width);
            Assert.GreaterOrEqual((long)width * width, count);
            Assert.Less((long)(width - 1) * (width - 1), count);
        }
    }
}
