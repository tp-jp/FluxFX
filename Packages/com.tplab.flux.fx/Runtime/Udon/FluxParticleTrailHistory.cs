using TpLab.Flux.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// Slotの世代を毎Stepで追跡し、ワールド空間の軌跡を循環Bufferへ記録します。
    /// </summary>
    public static class FluxParticleTrailHistory
    {
        public const int MaximumTrails = 4096;
        public const int PointCount = 16;
        public const int MetadataA = PointCount;
        public const int MetadataB = PointCount + 1;
        public const int TickIndex = 0;
        public const int HeadIndex = 1;
        public const int MetadataIndex = 2;
        public const int ElapsedIndex = 3;
        public const float SampleInterval = 1f / 30f;
        public const int MaximumTick = 16777215;

        /// <summary>
        /// 履歴と世代を消去します。
        /// </summary>
        public static void Clear(FluxKernel kernel, FluxBuffer[] buffers, Material[] materials, float[] state)
        {
            kernel.SetFloat("_TrailMode", 2);
            for (var i = 0; i < buffers.Length; i++)
            {
                kernel.Dispatch(buffers[i]);
            }
            state[TickIndex] = 0;
            state[HeadIndex] = 0;
            state[MetadataIndex] = MetadataA;
            state[ElapsedIndex] = SampleInterval;
            var metadata = buffers[MetadataA];
            var size = new Vector4(metadata.Texture.width, metadata.Texture.height, metadata.Count, 0);
            for (var segment = 0; segment < materials.Length; segment++)
            {
                materials[segment].SetVector("_TrailSize", size);
                materials[segment].SetVector("_TrailFade", new Vector4((float)segment / (PointCount - 1), (float)(segment + 1) / (PointCount - 1), 0, 0));
            }
            Bind(buffers, materials, state);
        }

        /// <summary>
        /// 生存状態と受理済みSpawnを反映し、30Hzを上限に位置を記録します。
        /// </summary>
        public static void Step(FluxKernel kernel, FluxBuffer[] buffers, Material[] materials, float[] state,
            Texture spawnMask, int count, float deltaTime, Matrix4x4 localToWorld, bool worldSpace)
        {
            // Floatで世代を正確に比較できる範囲内で全履歴を更新します。
            if (state[TickIndex] >= MaximumTick) Clear(kernel, buffers, materials, state);
            state[TickIndex] += 1;
            var tracked = Mathf.Min(MaximumTrails, count);
            var texture = buffers[0].Texture;
            kernel.SetVector("_TrailSize", new Vector4(texture.width, texture.height, tracked, count));
            kernel.SetFloat("_TrailTick", state[TickIndex]);
            kernel.SetFloat("_TrailEnabled", 1f);
            kernel.SetTexture("_TrailSpawnMask", spawnMask);
            kernel.SetFloat("_TrailHasSpawnMask", spawnMask == null ? 0f : 1f);
            kernel.SetFloat("_TrailWorldSpace", worldSpace ? 1f : 0f);
            for (var row = 0; row < 3; row++)
            {
                kernel.SetVector("_TrailRow" + row, localToWorld.GetRow(row));
            }
            var previous = (int)state[MetadataIndex];
            var next = previous == MetadataA ? MetadataB : MetadataA;
            kernel.SetBuffer("_TrailMetadata", buffers[previous]);
            kernel.SetFloat("_TrailMode", 0);
            kernel.Dispatch(buffers[next]);
            state[MetadataIndex] = next;
            state[ElapsedIndex] += deltaTime;
            if (state[ElapsedIndex] >= SampleInterval)
            {
                kernel.SetBuffer("_TrailMetadata", buffers[next]);
                kernel.SetFloat("_TrailMode", 1);
                kernel.Dispatch(buffers[(int)state[HeadIndex]]);
                state[HeadIndex] = ((int)state[HeadIndex] + 1) % PointCount;
                state[ElapsedIndex] %= SampleInterval;
                Bind(buffers, materials, state);
            }
            else
            {
                for (var segment = 0; segment < materials.Length; segment++)
                {
                    materials[segment].SetTexture("_TrailMetadata", buffers[next].Texture);
                }
            }
        }

        /// <summary>
        /// 古い点から新しい点へ描画区間を割り当てます。
        /// </summary>
        public static void Bind(FluxBuffer[] buffers, Material[] materials, float[] state)
        {
            var metadata = buffers[(int)state[MetadataIndex]];
            for (var segment = 0; segment < PointCount - 1; segment++)
            {
                var first = ((int)state[HeadIndex] + segment) % PointCount;
                materials[segment].SetTexture("_TrailFirst", buffers[first].Texture);
                materials[segment].SetTexture("_TrailSecond", buffers[(first + 1) % PointCount].Texture);
                materials[segment].SetTexture("_TrailMetadata", metadata.Texture);
            }
        }

        /// <summary>
        /// 描画設定を全区間へ反映します。
        /// </summary>
        public static void SetAppearance(Material[] materials, bool enabled, float width, Vector4 color)
        {
            if (materials == null) return;
            for (var i = 0; i < materials.Length; i++)
            {
                materials[i].SetFloat("_TrailEnabled", enabled ? 1f : 0f);
                materials[i].SetFloat("_TrailWidth", width);
                materials[i].SetVector("_TrailColor", color);
            }
        }
    }
}
