using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;
using Mathf = UnityEngine.Mathf;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleShapeParameter
    {
        ShapeType,
        Radius,
        Angle,
        Size,

        Count
    }

    /// <summary>
    /// ParticleのShapeに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleShapeParameters : DataList
    {
        internal static FluxParticleShapeParameters New(FluxParticleShapeType shapeType, float radius, float angle, Vector3 size)
        {
            var data = new DataToken[(int)FluxParticleShapeParameter.Count];

            data[(int)FluxParticleShapeParameter.ShapeType] = shapeType.ToInt();
            data[(int)FluxParticleShapeParameter.Radius] = Mathf.Max(0, radius);
            data[(int)FluxParticleShapeParameter.Angle] = Mathf.Clamp(angle, 0, 90);
            data[(int)FluxParticleShapeParameter.Size] = new DataToken(size);

            return (FluxParticleShapeParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleShapeParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleShapeParametersExtensions
    {
        /// <summary>
        /// Particleの生成形状を取得します。
        /// </summary>
        /// <param name="parameters">Shapeパラメーター</param>
        /// <returns>Particleの生成形状</returns>
        [PublicAPI]
        public static FluxParticleShapeType GetShapeType(this FluxParticleShapeParameters parameters)
        {
            return (FluxParticleShapeType)parameters[(int)FluxParticleShapeParameter.ShapeType].Int;
        }

        /// <summary>
        /// Particleの生成形状を設定します。
        /// </summary>
        /// <param name="parameters">Shapeパラメーター</param>
        /// <param name="type">Particleの生成形状</param>
        [PublicAPI]
        public static void SetShapeType(this FluxParticleShapeParameters parameters, FluxParticleShapeType type)
        {
            parameters[(int)FluxParticleShapeParameter.ShapeType] = type.ToInt();
        }

        /// <summary>
        /// Shapeの半径を取得します。
        /// </summary>
        /// <param name="parameters">Shapeパラメーター</param>
        /// <returns>Shapeの半径</returns>
        [PublicAPI]
        public static float GetRadius(this FluxParticleShapeParameters parameters)
        {
            return (float)parameters[(int)FluxParticleShapeParameter.Radius].Double;
        }

        /// <summary>
        /// Shapeの半径を設定します。
        /// </summary>
        /// <param name="parameters">Shapeパラメーター</param>
        /// <param name="radius">Shapeの半径</param>
        [PublicAPI]
        public static void SetRadius(this FluxParticleShapeParameters parameters, float radius)
        {
            parameters[(int)FluxParticleShapeParameter.Radius] = Mathf.Max(0, radius);
        }

        /// <summary>
        /// Cone Shapeの角度を取得します。
        /// </summary>
        /// <param name="parameters">Shapeパラメーター</param>
        /// <returns>Cone Shapeの角度</returns>
        [PublicAPI]
        public static float GetAngle(this FluxParticleShapeParameters parameters)
        {
            return (float)parameters[(int)FluxParticleShapeParameter.Angle].Double;
        }

        /// <summary>
        /// Cone Shapeの角度を設定します。
        /// </summary>
        /// <param name="parameters">Shapeパラメーター</param>
        /// <param name="angle">Cone Shapeの角度</param>
        [PublicAPI]
        public static void SetAngle(this FluxParticleShapeParameters parameters, float angle)
        {
            parameters[(int)FluxParticleShapeParameter.Angle] = Mathf.Clamp(angle, 0, 90);
        }

        /// <summary>
        /// Box Shapeのサイズを取得します。
        /// </summary>
        /// <param name="parameters">Shapeパラメーター</param>
        /// <returns>Box Shapeのサイズ</returns>
        [PublicAPI]
        public static Vector3 GetSize(this FluxParticleShapeParameters parameters)
        {
            return (Vector3)parameters[(int)FluxParticleShapeParameter.Size].Reference;
        }

        /// <summary>
        /// Box Shapeのサイズを設定します。
        /// </summary>
        /// <param name="parameters">Shapeパラメーター</param>
        /// <param name="size">Box Shapeのサイズ</param>
        [PublicAPI]
        public static void SetSize(this FluxParticleShapeParameters parameters, Vector3 size)
        {
            parameters[(int)FluxParticleShapeParameter.Size] = new DataToken(size);
        }
    }
}