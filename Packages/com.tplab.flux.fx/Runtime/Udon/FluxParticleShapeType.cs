using JetBrains.Annotations;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// Particleの生成形状を表します。
    /// </summary>
    [PublicAPI]
    public enum FluxParticleShapeType
    {
        Point,
        Sphere,
        Hemisphere,
        Circle,
        Box,
        Cone
    }

    public static class FluxParticleShapeTypeExtensions
    {
        /// <summary>
        /// Particleの生成形状を数値として取得します。
        /// </summary>
        /// <param name="shapeType">生成形状</param>
        /// <returns>生成形状を表す数値</returns>
        internal static int ToInt(this FluxParticleShapeType shapeType)
        {
            return (int)shapeType;
        }        
    }
}