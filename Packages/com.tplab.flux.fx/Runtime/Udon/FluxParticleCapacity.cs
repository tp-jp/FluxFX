using System;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// State Textureと描画Meshの制約からParticle容量を求めます。
    /// </summary>
    public static class FluxParticleCapacity
    {
        // Particle Indexと有効Slot数をfloatで正確に扱える上限。
        public const int MaximumExactCount = 16777216;

        /// <summary>
        /// State Textureに格納できる最大Slot数を求めます。
        /// </summary>
        /// <param name="maxTextureSize">GPUの最大Texture寸法</param>
        /// <returns>Texture寸法と整数精度から求めた上限</returns>
        public static int GetMaximumStateCount(int maxTextureSize)
        {
            if (maxTextureSize < 1) return 0;
            return maxTextureSize >= 4096 ? MaximumExactCount : maxTextureSize * maxTextureSize;
        }

        /// <summary>
        /// GPUと描画Meshの制約から最大Particle数を求めます。
        /// </summary>
        /// <param name="maxTextureSize">GPUの最大Texture寸法</param>
        /// <param name="supports32BitIndices">32bit Indexを使用できるか</param>
        /// <param name="verticesPerParticle">1粒子あたりの頂点数</param>
        /// <param name="indicesPerParticle">1粒子あたりのIndex数</param>
        /// <returns>技術的な容量上限。必要メモリの確保は別途必要</returns>
        public static int GetMaximumCount(int maxTextureSize, bool supports32BitIndices,
            int verticesPerParticle, int indicesPerParticle)
        {
            if (verticesPerParticle < 1 || indicesPerParticle < 1) return 0;
            var count = GetMaximumStateCount(maxTextureSize);
            count = Math.Min(count, int.MaxValue / verticesPerParticle);
            count = Math.Min(count, int.MaxValue / indicesPerParticle);
            if (!supports32BitIndices)
            {
                count = Math.Min(count, 65535 / verticesPerParticle);
            }
            return count;
        }

        /// <summary>
        /// 頂点とIndexのGPU Bufferサイズを含めた最大Particle数を求めます。
        /// </summary>
        /// <param name="maxTextureSize">GPUの最大Texture寸法</param>
        /// <param name="supports32BitIndices">32bit Indexを使用できるか</param>
        /// <param name="verticesPerParticle">1粒子あたりの頂点数</param>
        /// <param name="indicesPerParticle">1粒子あたりのIndex数</param>
        /// <param name="maxBufferBytes">GPU Bufferの最大バイト数</param>
        /// <param name="vertexStrideBytes">1頂点あたりのバイト数</param>
        /// <returns>技術的な容量上限</returns>
        public static int GetMaximumCount(int maxTextureSize, bool supports32BitIndices,
            int verticesPerParticle, int indicesPerParticle, long maxBufferBytes, int vertexStrideBytes)
        {
            if (maxBufferBytes < 1 || vertexStrideBytes < 1) return 0;
            var count = GetMaximumCount(maxTextureSize, supports32BitIndices, verticesPerParticle, indicesPerParticle);
            if (count == 0) return 0;
            count = (int)Math.Min((long)count, maxBufferBytes / ((long)verticesPerParticle * vertexStrideBytes));
            var count16 = Math.Min(count, 65535 / verticesPerParticle);
            count16 = (int)Math.Min((long)count16, maxBufferBytes / ((long)indicesPerParticle * 2));
            if (!supports32BitIndices) return count16;
            var count32 = (int)Math.Min((long)count, maxBufferBytes / ((long)indicesPerParticle * 4));
            return Math.Max(count16, count32);
        }

        /// <summary>
        /// 数値精度とMeshの配列寸法から最大Particle数を求めます。
        /// </summary>
        /// <param name="sourceMesh">描画Mesh。nullの場合はBillboard</param>
        /// <returns>構造上の容量上限。読み取り不能なMeshの場合は0</returns>
        public static int GetMaximumCount(Mesh sourceMesh)
        {
            if (sourceMesh != null && !sourceMesh.isReadable) return 0;
            var vertices = sourceMesh == null ? 4 : sourceMesh.vertexCount;
            var indices = sourceMesh == null ? 6 : sourceMesh.triangles.Length;
            if (vertices < 1 || indices < 1) return 0;
            return Math.Min(MaximumExactCount, Math.Min(int.MaxValue / vertices, int.MaxValue / indices));
        }

        /// <summary>
        /// 全Slotを格納する正方形Textureの一辺を求めます。
        /// </summary>
        /// <param name="count">有効Slot数</param>
        /// <returns>切り上げたTexture寸法</returns>
        public static int GetTextureWidth(int count)
        {
            return (int)Math.Ceiling(Math.Sqrt(count));
        }
    }
}
