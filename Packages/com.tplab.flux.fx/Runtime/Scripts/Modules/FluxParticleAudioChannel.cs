using System;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    /// <summary>
    /// 固定IDで参照する音声チャンネル。名前や配列順を変更しても参照を保持します。
    /// </summary>
    [Serializable]
    public sealed class FluxParticleAudioChannel
    {
        [SerializeField]
        [HideInInspector]
        string id = Guid.NewGuid().ToString("N");

        [SerializeField]
        string name = "Channel";

        [SerializeField]
        FluxParticleAudioSource source = FluxParticleAudioSource.Bass;

        [SerializeField]
        [Range(0, 4)]
        float gain = 1;

        [SerializeField]
        [Min(0.01f)]
        float attack = 0.2f;

        [SerializeField]
        [Min(0.01f)]
        float release = 0.8f;

        /// <summary>
        /// 参照用の固定ID
        /// </summary>
        public string Id => id;

        /// <summary>
        /// 表示名
        /// </summary>
        public string Name => name;

        /// <summary>
        /// 入力元
        /// </summary>
        public FluxParticleAudioSource Source => source;

        /// <summary>
        /// 共通入力処理後の追加感度。適用後は0〜1
        /// </summary>
        public float Gain => gain;

        /// <summary>
        /// 立ち上がり時間（秒）
        /// </summary>
        public float Attack => attack;

        /// <summary>
        /// 減衰時間（秒）
        /// </summary>
        public float Release => release;
    }
}
