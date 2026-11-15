using System;
using UnityEngine;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Scripts.Modules
{
    /// <summary>
    /// 粒ごとの基準倍率・時間変化・独立した音声応答を合成します。
    /// </summary>
    [Serializable]
    public sealed class FluxParticleBrightnessModule : FluxParticleModule
    {
        [SerializeField]
        [Min(0)]
        float baseMinimum = 1;

        [SerializeField]
        [Min(0)]
        float baseMaximum = 1;

        [SerializeField]
        FluxParticleBrightnessWaveform waveform = FluxParticleBrightnessWaveform.Off;

        [SerializeField]
        [Min(0)]
        float period = 1;

        [SerializeField]
        [Min(0.0001f)]
        float periodMinimum = 1;

        [SerializeField]
        [Min(0.0001f)]
        float periodMaximum = 1;

        [SerializeField]
        [Range(0, 1)]
        float randomPhase = 0;

        [SerializeField]
        FluxParticleBrightnessTimeMode timeMode = FluxParticleBrightnessTimeMode.SystemTime;

        [SerializeField]
        [Min(0)]
        float minimum = 0;

        [SerializeField]
        [Min(0)]
        float maximum = 1;

        [SerializeField]
        [Range(0, 1)]
        float duty = 0.5f;

        [SerializeField]
        [Range(0, 1)]
        float smoothness = 0;

        [SerializeField]
        FluxParticleBrightnessTarget target = FluxParticleBrightnessTarget.Rgb;

        [SerializeField]
        bool audioEnabled = false;

        [SerializeField]
        [Min(0)]
        float audioBoost = 2;

        [SerializeField]
        [Min(0.01f)]
        float audioAttack = 0.025f;

        [SerializeField]
        [Min(0.01f)]
        float audioRelease = 0.12f;

        [SerializeField]
        [HideInInspector]
        bool preserveBlinkPhase;

        /// <summary>
        /// Blinkから移行したPulseに限り、従来の位相を保持する構築時情報
        /// </summary>
        public bool PreserveBlinkPhase => preserveBlinkPhase;

        /// <summary>
        /// 基準倍率の下限
        /// </summary>
        public float BaseMinimum => baseMinimum;

        /// <summary>
        /// 基準倍率の上限
        /// </summary>
        public float BaseMaximum => baseMaximum;

        /// <summary>
        /// 時間変化の波形
        /// </summary>
        public FluxParticleBrightnessWaveform Waveform => waveform;

        /// <summary>
        /// 基準周期（秒）。0は時間変化なし
        /// </summary>
        public float Period => period;

        /// <summary>
        /// 粒ごとの周期倍率の下限
        /// </summary>
        public float PeriodMinimum => periodMinimum;

        /// <summary>
        /// 粒ごとの周期倍率の上限
        /// </summary>
        public float PeriodMaximum => periodMaximum;

        /// <summary>
        /// 位相のランダム化量
        /// </summary>
        public float RandomPhase => randomPhase;

        /// <summary>
        /// 時間基準
        /// </summary>
        public FluxParticleBrightnessTimeMode TimeMode => timeMode;

        /// <summary>
        /// 時間変化倍率の下限
        /// </summary>
        public float Minimum => minimum;

        /// <summary>
        /// 時間変化倍率の上限
        /// </summary>
        public float Maximum => maximum;

        /// <summary>
        /// Pulseの点灯割合
        /// </summary>
        public float Duty => duty;

        /// <summary>
        /// Pulseの切替の滑らかさ
        /// </summary>
        public float Smoothness => smoothness;

        /// <summary>
        /// 倍率の適用先
        /// </summary>
        public FluxParticleBrightnessTarget Target => target;

        /// <summary>
        /// 音声応答の組み込み。変更には再構築が必要
        /// </summary>
        public bool AudioEnabled => audioEnabled;

        /// <summary>
        /// 音声増光量
        /// </summary>
        public float AudioBoost => audioBoost;

        /// <summary>
        /// 音声の立ち上がり時間（秒）
        /// </summary>
        public float AudioAttack => audioAttack;

        /// <summary>
        /// 音声の減衰時間（秒）
        /// </summary>
        public float AudioRelease => audioRelease;

        /// <inheritdoc />
        public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.Rendering;

        /// <inheritdoc />
        public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.Age;

        /// <inheritdoc />
        public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.None;
    }
}
