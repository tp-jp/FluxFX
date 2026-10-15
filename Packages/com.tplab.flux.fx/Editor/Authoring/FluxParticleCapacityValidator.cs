using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Authoring
{
    /// <summary>
    /// EditorのGPU情報からParticle容量を検証します。
    /// </summary>
    public static class FluxParticleCapacityValidator
    {
        /// <summary>
        /// 現在のGPUと描画Meshから最大Particle数を求めます。
        /// </summary>
        /// <param name="sourceMesh">描画Mesh。nullの場合はBillboard</param>
        /// <returns>技術的な容量上限。読み取り不能なMeshの場合は0</returns>
        public static int GetMaximumCount(Mesh sourceMesh)
        {
            if (sourceMesh != null && !sourceMesh.isReadable) return 0;
            var stride = 28;
            if (sourceMesh != null)
            {
                if (sourceMesh.normals.Length == sourceMesh.vertexCount)
                {
                    stride += 12;
                }
                if (sourceMesh.tangents.Length == sourceMesh.vertexCount)
                {
                    stride += 16;
                }
            }
            return FluxParticleCapacity.GetMaximumCount(SystemInfo.maxTextureSize, SystemInfo.supports32bitsIndexBuffer,
                sourceMesh == null ? 4 : sourceMesh.vertexCount,
                sourceMesh == null ? 6 : sourceMesh.triangles.Length, SystemInfo.maxGraphicsBufferSize, stride);
        }

    }
}
