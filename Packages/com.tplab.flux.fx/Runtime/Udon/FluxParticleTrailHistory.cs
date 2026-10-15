using TpLab.Flux.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// Slotの世代を毎Stepで追跡し、ワールド空間の軌跡を循環Bufferへ記録します。
    /// </summary>
    public static class FluxParticleTrailHistory
    {
        public const int TickIndex = 0;
        public const int HeadIndex = 1;
        public const int MetadataIndex = 2;
        public const int ElapsedIndex = 3;
        public const int SampleAgeOffset = 4;
        public const int MaximumTick = 16777215;
        // GPUで0として扱われる非正規化数を記録間隔に使用しない。
        const float MinimumSampleInterval = 1.17549435E-38f;

        /// <summary>
        /// 記録時間と点数から、GPUで扱える記録間隔かを検証します。
        /// </summary>
        public static bool IsValidDuration(float duration, int points)
        {
            return points >= 2 && !float.IsNaN(duration) && !float.IsInfinity(duration) &&
                duration / (points - 1) >= MinimumSampleInterval;
        }

        /// <summary>
        /// 履歴・世代・記録点の経過時間を消去します。
        /// </summary>
        public static void Clear(FluxKernel kernel, FluxBuffer[] buffers, Material[] materials, float[] state)
        {
            kernel.SetFloat("_TrailMode", 2);
            for (var i = 0; i < buffers.Length; i++)
            {
                kernel.Dispatch(buffers[i]);
            }
            var points = buffers.Length - 2;
            state[TickIndex] = 0;
            state[HeadIndex] = 0;
            state[MetadataIndex] = points;
            state[ElapsedIndex] = 0;
            for (var i = SampleAgeOffset; i < state.Length; i++)
            {
                state[i] = -1;
            }
            var metadata = buffers[points];
            var size = new Vector4(metadata.Texture.width, metadata.Texture.height, metadata.Count, 0);
            for (var segment = 0; segment < materials.Length; segment++)
            {
                materials[segment].SetVector("_TrailSize", size);
            }
            Bind(buffers, materials, state);
        }

        /// <summary>
        /// 生存状態と受理済みSpawnを反映し、記録時間と点数に応じた間隔で位置を記録します。
        /// </summary>
        public static void Step(FluxKernel kernel, FluxBuffer[] buffers, Material[] materials, float[] state,
            Texture spawnMask, int count, float deltaTime, Matrix4x4 localToWorld, bool worldSpace, float duration)
        {
            // Floatで世代を正確に比較できる範囲内で全履歴を更新します。
            if (state[TickIndex] >= MaximumTick)
            {
                Clear(kernel, buffers, materials, state);
            }
            state[TickIndex] += 1;
            var points = buffers.Length - 2;
            var interval = duration / (points - 1);
            var texture = buffers[0].Texture;
            kernel.SetVector("_TrailSize", new Vector4(texture.width, texture.height, buffers[0].Count, count));
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
            var next = previous == points ? points + 1 : points;
            kernel.SetBuffer("_TrailMetadata", buffers[previous]);
            kernel.SetFloat("_TrailMode", 0);
            kernel.Dispatch(buffers[next]);
            state[MetadataIndex] = next;
            for (var i = SampleAgeOffset; i < state.Length; i++)
            {
                if (state[i] < 0) continue;
                state[i] = Mathf.Min(float.MaxValue, state[i] + deltaTime);
            }
            state[ElapsedIndex] += deltaTime;
            if (state[TickIndex] == 1 || state[ElapsedIndex] >= interval)
            {
                kernel.SetBuffer("_TrailMetadata", buffers[next]);
                kernel.SetFloat("_TrailMode", 1);
                var head = (int)state[HeadIndex];
                kernel.Dispatch(buffers[head]);
                state[SampleAgeOffset + head] = 0;
                state[HeadIndex] = (head + 1) % points;
                state[ElapsedIndex] %= interval;
                Bind(buffers, materials, state);
            }
            else
            {
                BindAges(buffers, materials, state);
            }
        }

        /// <summary>
        /// 古い点から新しい点へ描画区間を割り当てます。
        /// </summary>
        public static void Bind(FluxBuffer[] buffers, Material[] materials, float[] state)
        {
            var points = buffers.Length - 2;
            for (var segment = 0; segment < materials.Length; segment++)
            {
                var first = ((int)state[HeadIndex] + segment) % points;
                materials[segment].SetTexture("_TrailFirst", buffers[first].Texture);
                materials[segment].SetTexture("_TrailSecond", buffers[(first + 1) % points].Texture);
            }
            BindAges(buffers, materials, state);
        }

        static void BindAges(FluxBuffer[] buffers, Material[] materials, float[] state)
        {
            var metadata = buffers[(int)state[MetadataIndex]];
            var points = buffers.Length - 2;
            for (var segment = 0; segment < materials.Length; segment++)
            {
                var first = ((int)state[HeadIndex] + segment) % points;
                var second = (first + 1) % points;
                materials[segment].SetTexture("_TrailMetadata", metadata.Texture);
                materials[segment].SetVector("_TrailAges", new Vector4(state[SampleAgeOffset + first], state[SampleAgeOffset + second], 0, 0));
            }
        }

        /// <summary>
        /// 描画設定を全区間へ反映します。
        /// </summary>
        public static void SetAppearance(Material[] materials, bool enabled, float width, Vector4 color, float duration)
        {
            if (materials == null) return;
            for (var i = 0; i < materials.Length; i++)
            {
                materials[i].SetFloat("_TrailEnabled", enabled ? 1f : 0f);
                materials[i].SetFloat("_TrailWidth", width);
                materials[i].SetVector("_TrailColor", color);
                materials[i].SetFloat("_TrailDuration", duration);
            }
        }
    }
}
