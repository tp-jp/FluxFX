using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// Systemの位置・回転を取得する追従先
    /// </summary>
    public enum FluxParticleFollowTarget
    {
        Off,
        LocalPlayerPosition,
        LocalPlayerHead,
        SpecifiedTransform
    }

    /// <summary>
    /// Systemの回転追従方式。スケールは追従しません。
    /// </summary>
    public enum FluxParticleFollowRotation
    {
        None,
        HorizontalOnly,
        FullRotation
    }

    /// <summary>
    /// 位置オフセットの基準。追従先のスケールは含みません。
    /// </summary>
    public enum FluxParticleFollowOffsetSpace
    {
        World,
        TargetRotation
    }

    /// <summary>
    /// RuntimeとPreviewで共通の、System追従の位置・回転計算を提供します。
    /// </summary>
    public static class FluxParticleFollow
    {
        /// <summary>
        /// 追従先の位置に、指定した座標系のオフセットを加えます。
        /// </summary>
        /// <param name="targetPosition">追従先のワールド位置</param>
        /// <param name="targetRotation">追従先のワールド回転</param>
        /// <param name="offset">スケールを適用しない位置オフセット</param>
        /// <param name="space">オフセットの座標系</param>
        /// <returns>Systemのワールド位置</returns>
        public static Vector3 Position(Vector3 targetPosition, Quaternion targetRotation, Vector3 offset,
            FluxParticleFollowOffsetSpace space) => targetPosition +
            (space == FluxParticleFollowOffsetSpace.TargetRotation ? targetRotation * offset : offset);

        /// <summary>
        /// 追従方式に応じた回転を返します。水平の向きが定まらない場合は現在の方位を保ちます。
        /// </summary>
        /// <param name="targetRotation">追従先のワールド回転</param>
        /// <param name="currentRotation">Systemの現在のワールド回転</param>
        /// <param name="mode">回転の追従方式</param>
        /// <returns>Systemのワールド回転</returns>
        public static Quaternion Rotation(Quaternion targetRotation, Quaternion currentRotation, FluxParticleFollowRotation mode)
        {
            if (mode == FluxParticleFollowRotation.None) return currentRotation;
            if (mode == FluxParticleFollowRotation.FullRotation) return targetRotation;
            var forward = targetRotation * Vector3.forward;
            forward.y = 0;
            return forward.sqrMagnitude > 0.000001f ? Quaternion.LookRotation(forward, Vector3.up) :
                Quaternion.Euler(0, currentRotation.eulerAngles.y, 0);
        }
    }
}
