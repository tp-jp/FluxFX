using UnityEngine;
using VRC.SDKBase;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// 空きSlotの4分木集計からGPU上でSpawn先を割り当てます。
    /// </summary>
    public static class FluxParticleSpawnAllocator
    {
        /// <summary>
        /// 空きSlotの集計に必要なTexture段数を求めます。
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
        /// 空きSlot判定と4個ずつの集計に使うTextureを作成します。
        /// </summary>
        /// <returns>集計Texture配列。確保に失敗した場合はnull</returns>
        public static RenderTexture[] CreateTextures(int count)
        {
            var textures = new RenderTexture[GetLevelCount(count)];
            for (var level = 0; level < textures.Length; level++)
            {
                var width = FluxParticleCapacity.GetTextureWidth(count);
                var texture = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                textures[level] = texture;
                if (!texture.Create())
                {
                    ReleaseTextures(textures);
                    return null;
                }
                count = (count + 3) / 4;
            }
            return textures;
        }

        /// <summary>
        /// 集計Textureを解放します。
        /// </summary>
        public static void ReleaseTextures(RenderTexture[] textures)
        {
            if (textures == null) return;
            for (var i = 0; i < textures.Length; i++)
            {
                var texture = textures[i];
                if (texture == null) continue;
                texture.Release();
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                Object.DestroyImmediate(texture);
#else
                Object.Destroy(texture);
#endif
            }
        }

        /// <summary>
        /// Slot数と集計Textureの固定Bindingを設定します。
        /// </summary>
        public static void Configure(Material[] materials, RenderTexture[] levels, RenderTexture mask, int count)
        {
            var ranks = materials[2];
            var stateSize = new Vector4(mask.width, mask.height, count, 0);
            ranks.SetVector("_FluxStateSize", stateSize);
            materials[0].SetVector("_FluxStateSize", stateSize);
            materials[0].SetVector("_FluxAllocationSize", new Vector4(levels[0].width, levels[0].height, count, 0));
            var sourceCount = count;
            for (var level = 0; level < 12; level++)
            {
                var texture = levels[Mathf.Min(level, levels.Length - 1)];
                ranks.SetTexture("_FluxLevel" + level, texture);
                ranks.SetVector("_FluxLevelSize" + level, new Vector4(texture.width, texture.height, sourceCount, 0));
                sourceCount = (sourceCount + 3) / 4;
            }
            ranks.SetFloat("_FluxLevelCount", levels.Length);
            ranks.SetTexture("_FluxTotal", levels[levels.Length - 1]);
        }

        /// <summary>
        /// 現在のStateから空きSlot数の集計ツリーを更新します。
        /// </summary>
        public static void BuildCounts(Material[] materials, RenderTexture[] levels, int count)
        {
            var availability = materials[0];
            var reduction = materials[1];
            VRCGraphics.Blit(null, levels[0], availability);
            var sourceCount = count;
            for (var level = 1; level < levels.Length; level++)
            {
                var source = levels[level - 1];
                var destination = levels[level];
                var destinationCount = (sourceCount + 3) / 4;
                reduction.SetVector("_FluxSourceSize", new Vector4(source.width, source.height, sourceCount, 0));
                reduction.SetVector("_FluxDestinationSize", new Vector4(destination.width, destination.height, destinationCount, 0));
                reduction.SetTexture("_FluxSource", source);
                VRCGraphics.Blit(null, destination, reduction);
                sourceCount = destinationCount;
            }
        }

        /// <summary>
        /// 空きSlotを集計し、生成要求を循環順位へ割り当てます。
        /// </summary>
        public static void Execute(Material[] materials, RenderTexture[] levels, RenderTexture mask,
            RenderTexture keys, int count, int start, int requests, int seed)
        {
            var ranks = materials[2];
            var key = materials[3];
            ranks.SetFloat("_FluxSpawnCount", requests);
            if (requests == 0)
            {
                VRCGraphics.Blit(null, mask, ranks);
                return;
            }
            BuildCounts(materials, levels, count);
            ranks.SetFloat("_FluxSpawnFirst", start);
            VRCGraphics.Blit(null, mask, ranks);
            key.SetFloat("_FluxSpawnSeedBase", seed);
            key.SetTexture("_FluxAllocation", mask);
            VRCGraphics.Blit(null, keys, key);
        }
    }
}
