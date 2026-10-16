using System;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// ビルド済みParameterの入力制約
    /// </summary>
    [Flags]
    public enum FluxParticleParameterConstraintFlags
    {
        None = 0,
        Minimum = 1,
        Maximum = 2,
        ExclusiveMinimum = 4,
        ExclusiveMaximum = 8,
        Integer = 16,
        MaximumProduct = 32,
        AllowedValues = 64,
    }
}
