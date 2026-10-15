using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    /// <summary>
    /// 指定したSlot数・点数・時間で軌跡を記録します。
    /// </summary>
    [Serializable]
    public sealed class FluxParticleTrailsModule : FluxParticleModule, ISerializationCallbackReceiver
    {
        [SerializeField]
        [HideInInspector]
        int settingsVersion = 1;

        [SerializeField]
        [Min(1)]
        int maxTrails = 4096;

        [SerializeField]
        [Min(2)]
        int pointsPerTrail = 16;

        [SerializeField]
        [Min(0)]
        float duration = 0.5f;

        [SerializeField]
        [Min(0)]
        float width = 0.05f;

        [SerializeField]
        [ColorUsage(true, true)]
        Color color = Color.white;

        /// <summary>
        /// 軌跡を記録するSlot数の上限
        /// </summary>
        public int MaxTrails => maxTrails;

        /// <summary>
        /// 1本あたりの履歴点数
        /// </summary>
        public int PointsPerTrail => pointsPerTrail;

        /// <summary>
        /// 軌跡の記録時間（秒）
        /// </summary>
        public float Duration => duration;

        public float Width => width;

        public Color Color => color;

        public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.Rendering;

        public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.Position | FluxParticleAttribute.Age;

        public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.None;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (settingsVersion != 0) return;
            maxTrails = 4096;
            pointsPerTrail = 16;
            duration = 0.5f;
            settingsVersion = 1;
        }
    }
}
