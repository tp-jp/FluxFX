namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// 未平滑化音声の入力元。0は既存Brightnessの全4帯域平均を保持します。
    /// </summary>
    public enum FluxParticleAudioSource
    {
        Average = 0,
        Bass = 1,
        LowMid = 2,
        HighMid = 3,
        Treble = 4
    }
}
