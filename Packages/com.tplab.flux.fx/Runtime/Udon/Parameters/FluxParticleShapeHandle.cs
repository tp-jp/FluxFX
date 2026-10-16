using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// Shapeの操作ハンドル
    /// </summary>
    /// <remarks>
    /// System.Shapeから取得し、型付き拡張メソッドで操作します。
    /// ParameterIndexとParameterCountの定数は、別アセンブリのビルド処理と共有するBinding定義です。
    /// 通常の利用では数値キーを指定する必要はありません。
    /// </remarks>
    public class FluxParticleShapeHandle : FluxParticleParameterHandle
    {
        public const int PositionParameterIndex = 0;
        public const int RadiusParameterIndex = 1;
        public const int AngleParameterIndex = 2;
        public const int SizeParameterIndex = 3;
        public const int ParameterCount = 4;

        internal static FluxParticleShapeHandle Create(FluxParticleSystem system, int[] slots)
        {
#if COMPILER_UDONSHARP
            var handle = (FluxParticleShapeHandle)new DataList();
#else
            var handle = new FluxParticleShapeHandle();
#endif
            InitializeSlots(handle, system.Executor, system, slots);
            return handle;
        }
    }

    public static class FluxParticleShapeHandleExtensions
    {
        /// <summary>
        /// 生成位置の操作に対応しているかを返します。
        /// </summary>
        public static bool HasPosition(this FluxParticleShapeHandle handle) => handle.HasParameter(FluxParticleShapeHandle.PositionParameterIndex);

        /// <summary>
        /// 生成位置を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static Vector3 GetPosition(this FluxParticleShapeHandle handle) => handle.GetValue(FluxParticleShapeHandle.PositionParameterIndex);

        /// <summary>
        /// 生成位置を変更します。非対応またはStatic制約により変更できない場合はfalseを返します。
        /// </summary>
        public static bool SetPosition(this FluxParticleShapeHandle handle, Vector3 value) => handle.SetValue(FluxParticleShapeHandle.PositionParameterIndex, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 半径の操作に対応しているかを返します。
        /// </summary>
        public static bool HasRadius(this FluxParticleShapeHandle handle) => handle.HasParameter(FluxParticleShapeHandle.RadiusParameterIndex);

        /// <summary>
        /// 半径を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static float GetRadius(this FluxParticleShapeHandle handle) => handle.GetValue(FluxParticleShapeHandle.RadiusParameterIndex).x;

        /// <summary>
        /// 半径を変更します。非対応またはStatic制約により変更できない場合はfalseを返します。
        /// </summary>
        public static bool SetRadius(this FluxParticleShapeHandle handle, float value) => handle.SetValue(FluxParticleShapeHandle.RadiusParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// Cone角度の操作に対応しているかを返します。
        /// </summary>
        public static bool HasAngle(this FluxParticleShapeHandle handle) => handle.HasParameter(FluxParticleShapeHandle.AngleParameterIndex);

        /// <summary>
        /// Cone角度を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static float GetAngle(this FluxParticleShapeHandle handle) => handle.GetValue(FluxParticleShapeHandle.AngleParameterIndex).x;

        /// <summary>
        /// Cone角度を変更します。非対応またはStatic制約により変更できない場合はfalseを返します。
        /// </summary>
        public static bool SetAngle(this FluxParticleShapeHandle handle, float value) => handle.SetValue(FluxParticleShapeHandle.AngleParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// Boxサイズの操作に対応しているかを返します。
        /// </summary>
        public static bool HasSize(this FluxParticleShapeHandle handle) => handle.HasParameter(FluxParticleShapeHandle.SizeParameterIndex);

        /// <summary>
        /// Boxサイズを取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static Vector3 GetSize(this FluxParticleShapeHandle handle) => handle.GetValue(FluxParticleShapeHandle.SizeParameterIndex);

        /// <summary>
        /// Boxサイズを変更します。非対応またはStatic制約により変更できない場合はfalseを返します。
        /// </summary>
        public static bool SetSize(this FluxParticleShapeHandle handle, Vector3 value) => handle.SetValue(FluxParticleShapeHandle.SizeParameterIndex, FluxParticleValueType.Float3, value);

        /// <summary>
        /// ビルド済みの生成形状を取得します。
        /// </summary>
        public static FluxParticleShapeType GetShapeType(this FluxParticleShapeHandle handle) => ((FluxParticleSystem)handle[FluxParticleParameterHandle.ContextIndex].Reference).ShapeType;
    }
}

