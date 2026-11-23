using System;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts
{
    /// <summary>
    /// System全体の追従設定。OFFでは既存の配置と更新順を保持します。
    /// </summary>
    [Serializable]
    public sealed class FluxParticleFollowSettings
    {
        [SerializeField]
        FluxParticleFollowTarget target;

        [SerializeField]
        Transform targetTransform;

        [SerializeField]
        Vector3 positionOffset;

        [SerializeField]
        FluxParticleFollowOffsetSpace offsetSpace;

        [SerializeField]
        FluxParticleFollowRotation rotation;

        [SerializeField]
        Transform previewTarget;

        /// <summary>
        /// 追従先。既定はOFF
        /// </summary>
        public FluxParticleFollowTarget Target => target;

        /// <summary>
        /// 指定Transformモードの追従先。System自身とその子は指定不可
        /// </summary>
        public Transform TargetTransform => targetTransform;

        /// <summary>
        /// スケールを適用しない追従位置のオフセット
        /// </summary>
        public Vector3 PositionOffset => positionOffset;

        /// <summary>
        /// オフセットの座標系。既定はWorld
        /// </summary>
        public FluxParticleFollowOffsetSpace OffsetSpace => offsetSpace;

        /// <summary>
        /// 回転の追従方式。既定は回転を維持
        /// </summary>
        public FluxParticleFollowRotation Rotation => rotation;

        /// <summary>
        /// 旧Previewの保存参照。現在のPreview操作では使用せず、Runtimeにもベイクしません。
        /// </summary>
        public Transform PreviewTarget => previewTarget;
    }
}
