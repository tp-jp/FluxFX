namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// 距離をEffect Scaleと既存の一様Transform Scaleに追従させるか、ワールド単位で固定するか。
    /// </summary>
    public enum FluxParticleDistanceMode
    {
        FollowEffect,
        FixedWorld
    }
}
