using System;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    /// <summary>
    /// システム原点とカメラの距離に応じて描画を薄くします。距離は親を含む一様Scaleに追従するローカル単位です。
    /// </summary>
    [Serializable]
    public sealed class FluxParticleDistanceFadeModule : FluxParticleModule
    {
        [SerializeField]
        [Min(0)]
        [Tooltip("Distance from the system origin in local units, scaled by its uniform world scale. Orthographic cameras do not fade.")]
        float startDistance = 5;

        [SerializeField]
        [Min(0)]
        [Tooltip("No fade when End Distance is less than or equal to Start Distance. Non-uniform scale is not supported.")]
        float endDistance = 10;

        [SerializeField]
        [Tooltip("RGB attenuates emitted light; Alpha attenuates opacity. Choose for the renderer blend mode.")]
        FluxParticleDistanceFadeTarget target = FluxParticleDistanceFadeTarget.Rgb;

        /// <summary>
        /// フェード開始距離
        /// </summary>
        public float StartDistance => startDistance;

        /// <summary>
        /// フェード終了距離
        /// </summary>
        public float EndDistance => endDistance;

        /// <summary>
        /// フェードの適用先
        /// </summary>
        public FluxParticleDistanceFadeTarget Target => target;

        /// <inheritdoc />
        public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.Rendering;

        /// <inheritdoc />
        public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.None;

        /// <inheritdoc />
        public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.None;
    }
}
