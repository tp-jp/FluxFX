using TpLab.Flux.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// 構成済みのBufferとKernelを使い、空きSlotの4分木集計からSpawn先を割り当てます。
    /// </summary>
    public static class FluxParticleSpawnAllocator
    {
        /// <summary>
        /// 空きSlotの集計に必要なBuffer段数を求めます。
        /// </summary>
        public static int GetLevelCount(int count)
        {
            var levels = 1;
            while (count > 1)
            {
                count = (count + 3) / 4;
                levels++;
            }
            return levels;
        }

        /// <summary>
        /// 集計と生成入力の固定Bindingを設定します。
        /// </summary>
        public static void Configure(FluxKernel[] kernels, FluxBuffer[] levels, FluxBuffer mask, int count)
        {
            var ranks = kernels[levels.Length];
            var stateSize = new Vector4(mask.Texture.width, mask.Texture.height, count, 0);
            ranks.SetVector("_FluxStateSize", stateSize);
            kernels[0].SetVector("_FluxStateSize", stateSize);
            kernels[0].SetVector("_FluxAllocationSize", new Vector4(levels[0].Texture.width, levels[0].Texture.height, count, 0));
            for (var level = 1; level < levels.Length; level++)
            {
                var source = levels[level - 1];
                var destination = levels[level];
                kernels[level].SetBuffer("_FluxSource", source);
                kernels[level].SetVector("_FluxSourceSize", new Vector4(source.Texture.width, source.Texture.height, source.Count, 0));
                kernels[level].SetVector("_FluxDestinationSize", new Vector4(destination.Texture.width, destination.Texture.height, destination.Count, 0));
            }
            for (var level = 0; level < 12; level++)
            {
                var buffer = levels[Mathf.Min(level, levels.Length - 1)];
                ranks.SetBuffer("_FluxLevel" + level, buffer);
                ranks.SetVector("_FluxLevelSize" + level, new Vector4(buffer.Texture.width, buffer.Texture.height, buffer.Count, 0));
            }
            ranks.SetFloat("_FluxLevelCount", levels.Length);
            ranks.SetBuffer("_FluxTotal", levels[levels.Length - 1]);
            kernels[levels.Length + 1].SetBuffer("_FluxAllocation", mask);
        }

        /// <summary>
        /// 現在のStateから空きSlot数の集計ツリーを更新します。
        /// </summary>
        public static void BuildCounts(FluxKernel[] kernels, FluxBuffer[] levels)
        {
            for (var level = 0; level < levels.Length; level++)
            {
                kernels[level].Dispatch(levels[level]);
            }
        }

        /// <summary>
        /// 空きSlotを集計し、生成要求を循環順位へ割り当てます。
        /// </summary>
        public static void Execute(FluxKernel[] kernels, FluxBuffer[] levels, FluxBuffer mask,
            FluxBuffer keys, int start, int requests, int seed)
        {
            var ranks = kernels[levels.Length];
            var key = kernels[levels.Length + 1];
            ranks.SetFloat("_FluxSpawnCount", requests);
            if (requests == 0)
            {
                ranks.Dispatch(mask);
                return;
            }
            BuildCounts(kernels, levels);
            ranks.SetFloat("_FluxSpawnFirst", start);
            ranks.Dispatch(mask);
            key.SetFloat("_FluxSpawnSeedBase", seed);
            key.Dispatch(keys);
        }
    }
}
