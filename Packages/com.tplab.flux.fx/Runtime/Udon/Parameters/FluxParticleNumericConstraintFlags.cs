using System;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// ビルド済み数値Parameterの入力制約
    /// </summary>
    [Flags]
    public enum FluxParticleNumericConstraintFlags
    {
        None = 0,
        Minimum = 1,
        Maximum = 2,
        ExclusiveMinimum = 4,
        ExclusiveMaximum = 8,
        Integer = 16,
    }
}
