using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// Mainの操作ハンドル
    /// </summary>
    /// <remarks>
    /// System.Mainから取得し、型付き拡張メソッドで操作します。
    /// ParameterIndexとParameterCountの定数は、別アセンブリのビルド処理と共有するBinding定義です。
    /// 通常の利用では数値キーを指定する必要はありません。
    /// </remarks>
    public class FluxParticleMainHandle : FluxParticleParameterHandle
    {
        public const int StartLifetimeRangeParameterIndex = 0;
        public const int StartSpeedParameterIndex = 1;
        public const int StartSpeedRangeParameterIndex = 2;
        public const int StartColorParameterIndex = 3;
        public const int StartColorMinParameterIndex = 4;
        public const int StartColorMaxParameterIndex = 5;
        public const int StartSizeParameterIndex = 6;
        public const int StartSizeRangeParameterIndex = 7;
        public const int StartRotationParameterIndex = 8;
        public const int StartRotationMinParameterIndex = 9;
        public const int StartRotationMaxParameterIndex = 10;
        public const int ParameterCount = 11;

        internal static FluxParticleMainHandle Create(FluxParticleSystem system, int[] slots)
        {
#if COMPILER_UDONSHARP
            var handle = (FluxParticleMainHandle)new DataList();
#else
            var handle = new FluxParticleMainHandle();
#endif
            InitializeSlots(handle, system.Executor, system, slots);
            return handle;
        }
    }

    public static class FluxParticleMainHandleExtensions
    {
        /// <summary>
        /// 初期Lifetimeの範囲を同じ値に設定します。
        /// </summary>
        public static bool SetStartLifetime(this FluxParticleMainHandle handle, float value) => handle.SetStartLifetimeRange(new Vector2(value, value));

        /// <summary>
        /// 初期Lifetimeの範囲の操作に対応しているかを返します。
        /// </summary>
        public static bool HasStartLifetimeRange(this FluxParticleMainHandle handle) => handle.HasParameter(FluxParticleMainHandle.StartLifetimeRangeParameterIndex);

        /// <summary>
        /// 初期Lifetimeの範囲を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static Vector2 GetStartLifetimeRange(this FluxParticleMainHandle handle) => handle.GetValue(FluxParticleMainHandle.StartLifetimeRangeParameterIndex);

        /// <summary>
        /// 初期Lifetimeの範囲を変更します。非対応またはStatic制約により変更できない場合はfalseを返します。
        /// </summary>
        public static bool SetStartLifetimeRange(this FluxParticleMainHandle handle, Vector2 value) => handle.SetValue(FluxParticleMainHandle.StartLifetimeRangeParameterIndex, FluxParticleValueType.Float2, new Vector4(value.x, value.y, 0, 0));

        /// <summary>
        /// 初期速度の操作に対応しているかを返します。
        /// </summary>
        public static bool HasStartSpeed(this FluxParticleMainHandle handle) => handle.HasParameter(FluxParticleMainHandle.StartSpeedParameterIndex);

        /// <summary>
        /// 初期速度を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static float GetStartSpeed(this FluxParticleMainHandle handle) => handle.GetValue(FluxParticleMainHandle.StartSpeedParameterIndex).x;

        /// <summary>
        /// 初期速度を変更します。非対応またはStatic制約により変更できない場合はfalseを返します。
        /// </summary>
        public static bool SetStartSpeed(this FluxParticleMainHandle handle, float value) => handle.SetValue(FluxParticleMainHandle.StartSpeedParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 初期速度の範囲の操作に対応しているかを返します。
        /// </summary>
        public static bool HasStartSpeedRange(this FluxParticleMainHandle handle) => handle.HasParameter(FluxParticleMainHandle.StartSpeedRangeParameterIndex);

        /// <summary>
        /// 初期速度の範囲を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static Vector2 GetStartSpeedRange(this FluxParticleMainHandle handle) => handle.GetValue(FluxParticleMainHandle.StartSpeedRangeParameterIndex);

        /// <summary>
        /// 初期速度の範囲を変更します。非対応またはStatic制約により変更できない場合はfalseを返します。
        /// </summary>
        public static bool SetStartSpeedRange(this FluxParticleMainHandle handle, Vector2 value) => handle.SetValue(FluxParticleMainHandle.StartSpeedRangeParameterIndex, FluxParticleValueType.Float2, new Vector4(value.x, value.y, 0, 0));

        /// <summary>
        /// 初期色の操作に対応しているかを返します。
        /// </summary>
        public static bool HasStartColor(this FluxParticleMainHandle handle) => handle.HasParameter(FluxParticleMainHandle.StartColorParameterIndex);

        /// <summary>
        /// 初期色を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static Color GetStartColor(this FluxParticleMainHandle handle) => handle.GetValue(FluxParticleMainHandle.StartColorParameterIndex);

        /// <summary>
        /// 初期色を変更します。非対応またはStatic制約により変更できない場合はfalseを返します。
        /// </summary>
        public static bool SetStartColor(this FluxParticleMainHandle handle, Color value) => handle.SetValue(FluxParticleMainHandle.StartColorParameterIndex, FluxParticleValueType.Float4, value);

        /// <summary>
        /// 初期色の下限の操作に対応しているかを返します。
        /// </summary>
        public static bool HasStartColorMin(this FluxParticleMainHandle handle) => handle.HasParameter(FluxParticleMainHandle.StartColorMinParameterIndex);

        /// <summary>
        /// 初期色の下限を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static Color GetStartColorMin(this FluxParticleMainHandle handle) => handle.GetValue(FluxParticleMainHandle.StartColorMinParameterIndex);

        /// <summary>
        /// 初期色の上限の操作に対応しているかを返します。
        /// </summary>
        public static bool HasStartColorMax(this FluxParticleMainHandle handle) => handle.HasParameter(FluxParticleMainHandle.StartColorMaxParameterIndex);

        /// <summary>
        /// 初期色の上限を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static Color GetStartColorMax(this FluxParticleMainHandle handle) => handle.GetValue(FluxParticleMainHandle.StartColorMaxParameterIndex);

        /// <summary>
        /// 初期サイズの操作に対応しているかを返します。
        /// </summary>
        public static bool HasStartSize(this FluxParticleMainHandle handle) => handle.HasParameter(FluxParticleMainHandle.StartSizeParameterIndex);

        /// <summary>
        /// 初期サイズを取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static float GetStartSize(this FluxParticleMainHandle handle) => handle.GetValue(FluxParticleMainHandle.StartSizeParameterIndex).x;

        /// <summary>
        /// 初期サイズを変更します。非対応またはStatic制約により変更できない場合はfalseを返します。
        /// </summary>
        public static bool SetStartSize(this FluxParticleMainHandle handle, float value) => handle.SetValue(FluxParticleMainHandle.StartSizeParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 初期サイズの範囲の操作に対応しているかを返します。
        /// </summary>
        public static bool HasStartSizeRange(this FluxParticleMainHandle handle) => handle.HasParameter(FluxParticleMainHandle.StartSizeRangeParameterIndex);

        /// <summary>
        /// 初期サイズの範囲を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static Vector2 GetStartSizeRange(this FluxParticleMainHandle handle) => handle.GetValue(FluxParticleMainHandle.StartSizeRangeParameterIndex);

        /// <summary>
        /// 初期サイズの範囲を変更します。非対応またはStatic制約により変更できない場合はfalseを返します。
        /// </summary>
        public static bool SetStartSizeRange(this FluxParticleMainHandle handle, Vector2 value) => handle.SetValue(FluxParticleMainHandle.StartSizeRangeParameterIndex, FluxParticleValueType.Float2, new Vector4(value.x, value.y, 0, 0));

        /// <summary>
        /// 初期回転の操作に対応しているかを返します。
        /// </summary>
        public static bool HasStartRotation(this FluxParticleMainHandle handle) => handle.HasParameter(FluxParticleMainHandle.StartRotationParameterIndex);

        /// <summary>
        /// 初期回転を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static Vector3 GetStartRotation(this FluxParticleMainHandle handle) => handle.GetValue(FluxParticleMainHandle.StartRotationParameterIndex);

        /// <summary>
        /// 初期回転を変更します。非対応またはStatic制約により変更できない場合はfalseを返します。
        /// </summary>
        public static bool SetStartRotation(this FluxParticleMainHandle handle, Vector3 value) => handle.SetValue(FluxParticleMainHandle.StartRotationParameterIndex, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 初期回転の下限の操作に対応しているかを返します。
        /// </summary>
        public static bool HasStartRotationMin(this FluxParticleMainHandle handle) => handle.HasParameter(FluxParticleMainHandle.StartRotationMinParameterIndex);

        /// <summary>
        /// 初期回転の下限を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static Vector3 GetStartRotationMin(this FluxParticleMainHandle handle) => handle.GetValue(FluxParticleMainHandle.StartRotationMinParameterIndex);

        /// <summary>
        /// 初期回転の上限の操作に対応しているかを返します。
        /// </summary>
        public static bool HasStartRotationMax(this FluxParticleMainHandle handle) => handle.HasParameter(FluxParticleMainHandle.StartRotationMaxParameterIndex);

        /// <summary>
        /// 初期回転の上限を取得します。非対応の場合は既定値を返します。
        /// </summary>
        public static Vector3 GetStartRotationMax(this FluxParticleMainHandle handle) => handle.GetValue(FluxParticleMainHandle.StartRotationMaxParameterIndex);

        /// <summary>
        /// 初期色の範囲を変更します。両方とも変更できる場合に設定します。
        /// </summary>
        public static bool SetStartColorRange(this FluxParticleMainHandle handle, Color min, Color max) => handle.SetPair(FluxParticleMainHandle.StartColorMinParameterIndex, FluxParticleMainHandle.StartColorMaxParameterIndex, FluxParticleValueType.Float4, min, max);

        /// <summary>
        /// 初期回転の範囲を変更します。両方とも変更できる場合に設定します。
        /// </summary>
        public static bool SetStartRotationRange(this FluxParticleMainHandle handle, Vector3 min, Vector3 max) => handle.SetPair(FluxParticleMainHandle.StartRotationMinParameterIndex, FluxParticleMainHandle.StartRotationMaxParameterIndex, FluxParticleValueType.Float3, min, max);

        /// <summary>
        /// ビルド済みのParticle Slot数を取得します。
        /// </summary>
        public static int GetParticleCount(this FluxParticleMainHandle handle) => GetSystem(handle).ParticleCount;

        /// <summary>
        /// ビルド済みのSimulation Spaceを取得します。
        /// </summary>
        public static FluxParticleSimulationSpace GetSimulationSpace(this FluxParticleMainHandle handle) => GetSystem(handle).SimulationSpace;

        /// <summary>
        /// 次回Restartの待機秒数を取得します。
        /// </summary>
        public static float GetStartDelay(this FluxParticleMainHandle handle) => GetSystem(handle).Emitter.StartDelay;

        /// <summary>
        /// 次回Restartの周期を取得します。
        /// </summary>
        public static float GetDuration(this FluxParticleMainHandle handle) => GetSystem(handle).Emitter.Duration;

        /// <summary>
        /// 次回RestartのLoop設定を取得します。
        /// </summary>
        public static bool GetLoop(this FluxParticleMainHandle handle) => GetSystem(handle).Emitter.Loop;

        /// <summary>
        /// StartDelayを変更し、次回Restartから使用します。
        /// </summary>
        public static bool SetStartDelay(this FluxParticleMainHandle handle, float value) => GetSystem(handle).Emitter.SetStartDelay(value);

        /// <summary>
        /// Durationを変更し、次回Restartから使用します。
        /// </summary>
        public static bool SetDuration(this FluxParticleMainHandle handle, float value) => GetSystem(handle).Emitter.SetDuration(value);

        /// <summary>
        /// Loopを変更し、次回Restartから使用します。
        /// </summary>
        public static void SetLoop(this FluxParticleMainHandle handle, bool value) => GetSystem(handle).Emitter.SetLoop(value);

        static FluxParticleSystem GetSystem(FluxParticleMainHandle handle)
        {
            return (FluxParticleSystem)handle[FluxParticleParameterHandle.ContextIndex].Reference;
        }
    }
}

