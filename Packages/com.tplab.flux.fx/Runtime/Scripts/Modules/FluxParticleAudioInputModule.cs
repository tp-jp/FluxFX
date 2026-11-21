using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    /// <summary>
    /// AudioLink入力を平滑化して供給します。単独では粒子の見た目を変更しません。
    /// </summary>
    [Serializable]
    public sealed class FluxParticleAudioInputModule : FluxParticleModule
    {
        [SerializeField]
        [Tooltip("AudioLink is optional. Outputs are zero without input; this module alone does not change appearance.")]
        FluxParticleAudioBand band = FluxParticleAudioBand.Bass;

        [SerializeField]
        [Range(0, 4)]
        float gain = 1;

        [SerializeField]
        [Range(0, 0.5f)]
        float noiseFloor = 0.02f;

        [SerializeField]
        [Range(0, 1)]
        float strength = 1;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Response time in seconds.")]
        float attack = 0.2f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Response time in seconds.")]
        float release = 0.8f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Response time in seconds.")]
        float slowResponse = 0.4f;

        [SerializeField]
        [Range(0, 4)]
        float averageGain = 1.5f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Seconds without a new AudioLink frame before outputs decay to zero.")]
        float timeout = 1;

        [SerializeField]
        FluxParticleAudioChannel[] channels = Array.Empty<FluxParticleAudioChannel>();

        /// <summary>
        /// 追加の独立した音声応答。配列の構成変更には再構築が必要
        /// </summary>
        public IReadOnlyList<FluxParticleAudioChannel> Channels => channels ?? Array.Empty<FluxParticleAudioChannel>();

        /// <summary>
        /// 音域
        /// </summary>
        public FluxParticleAudioBand Band => band;

        /// <summary>
        /// 入力感度
        /// </summary>
        public float Gain => gain;

        /// <summary>
        /// 無音閾値
        /// </summary>
        public float NoiseFloor => noiseFloor;

        /// <summary>
        /// 反応量
        /// </summary>
        public float Strength => strength;

        /// <summary>
        /// 立ち上がり時間（秒）
        /// </summary>
        public float Attack => attack;

        /// <summary>
        /// 減衰時間（秒）
        /// </summary>
        public float Release => release;

        /// <summary>
        /// 遅い応答の追従時間（秒）
        /// </summary>
        public float SlowResponse => slowResponse;

        /// <summary>
        /// 全4音域平均の入力感度
        /// </summary>
        public float AverageGain => averageGain;

        /// <summary>
        /// 入力更新の停止判定時間（秒）
        /// </summary>
        public float Timeout => timeout;

        /// <inheritdoc />
        public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.PositionUpdate;

        /// <inheritdoc />
        public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.None;

        /// <inheritdoc />
        public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.None;
    }
}
