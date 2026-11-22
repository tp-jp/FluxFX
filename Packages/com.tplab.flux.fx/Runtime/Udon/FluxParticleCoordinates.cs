using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// 基準座標と表示座標の共通変換。粒子Stateは基準座標のまま保持します。
    /// </summary>
    /// <remarks>scaleにはAuthoring・ビルド・Previewで確定した正の有限値を渡してください。</remarks>
    public static class FluxParticleCoordinates
    {
        /// <summary>
        /// 正の有限な表示倍率かを判定します。
        /// </summary>
        public static bool IsValidScale(float scale) => scale > 0 && !float.IsNaN(scale) && !float.IsInfinity(scale);

        /// <summary>
        /// 基準位置から表示ローカル位置への変換
        /// </summary>
        /// <param name="position">変換元の位置</param>
        /// <param name="scale">確定済みの表示倍率</param>
        public static Vector3 ReferenceToDisplay(Vector3 position, float scale) => position * scale;

        /// <summary>
        /// 表示ローカル位置から基準位置への逆変換
        /// </summary>
        /// <param name="position">変換元の位置</param>
        /// <param name="scale">確定済みの表示倍率</param>
        public static Vector3 DisplayToReference(Vector3 position, float scale) => position / scale;

        /// <summary>
        /// 基準位置をワールドへ変換します。Worldシミュレーションは倍率1のみです。
        /// </summary>
        /// <param name="position">変換元の位置</param>
        /// <param name="origin">システムの配置Transform</param>
        /// <param name="scale">確定済みの表示倍率。Worldでは1</param>
        /// <param name="space">シミュレーション空間</param>
        public static Vector3 ReferenceToWorld(Vector3 position, Transform origin, float scale, FluxParticleSimulationSpace space)
        {
            return space == FluxParticleSimulationSpace.World ? position : origin.TransformPoint(position * scale);
        }

        /// <summary>
        /// 外部ワールド位置を基準空間へ変換します。
        /// </summary>
        /// <param name="position">変換元の位置</param>
        /// <param name="origin">システムの配置Transform</param>
        /// <param name="scale">確定済みの表示倍率。Worldでは1</param>
        /// <param name="space">シミュレーション空間</param>
        public static Vector3 WorldToReference(Vector3 position, Transform origin, float scale, FluxParticleSimulationSpace space)
        {
            return space == FluxParticleSimulationSpace.World ? position : origin.InverseTransformPoint(position) / scale;
        }

        /// <summary>
        /// 基準長さから表示ローカル長さへの変換。Transform Scaleは含みません。
        /// </summary>
        public static float ReferenceLengthToDisplay(float length, float scale) => length * scale;

        /// <summary>
        /// 距離方針に従う実効ワールド長さ。FollowEffectは従来の一様Transform Scaleも含みます。
        /// </summary>
        public static float DistanceToWorld(float distance, float scale, float transformScale, FluxParticleDistanceMode mode)
        {
            return mode == FluxParticleDistanceMode.FixedWorld ? distance : distance * scale * Mathf.Abs(transformScale);
        }

        /// <summary>
        /// 基準カリング範囲の中心とサイズを表示ローカル範囲へ変換します。
        /// </summary>
        public static Bounds BoundsToDisplay(Bounds bounds, float scale) => new Bounds(bounds.center * scale, bounds.size * scale);
    }
}
