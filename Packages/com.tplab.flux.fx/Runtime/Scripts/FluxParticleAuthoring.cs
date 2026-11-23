using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Flux FX/Flux FX Settings")]
    public class FluxParticleAuthoring : MonoBehaviour
    {
        [SerializeField]
        [Min(1)]
        int particleCount = 64;

        [SerializeField]
        bool playOnAwake = true;

        [SerializeField]
        [HideInInspector]
        int settingsVersion;

        [SerializeField]
        FluxParticleSimulationSpace simulationSpace;

        [SerializeField]
        float effectScale = 1;

        [SerializeField]
        FluxParticleFollowSettings systemFollow = new FluxParticleFollowSettings();

        [SerializeField]
        PlaybackSettings playback = new PlaybackSettings();

        [SerializeField]
        EmissionSettings emission = new EmissionSettings();

        [SerializeField]
        LifetimeSettings lifetime = new LifetimeSettings();

        [SerializeField]
        ShapeSettings shape = new ShapeSettings();

        [SerializeField]
        InitialVelocitySettings initialVelocity = new InitialVelocitySettings();

        [SerializeField]
        RenderSettings render = new RenderSettings();

        [SerializeReference]
        List<FluxParticleModule> modules = new List<FluxParticleModule>();

        /// <summary>
        /// Particle Slot数
        /// </summary>
        public int ParticleCount => particleCount;

        /// <summary>
        /// 起動時の自動再生
        /// </summary>
        public bool PlayOnAwake => playOnAwake;

        /// <summary>
        /// 表示倍率の正式な保存値。停止中のみ変更でき、Transformには書き込みません。
        /// </summary>
        public float EffectScale => effectScale;

        /// <summary>
        /// 表示倍率を設定します。非有限値・非正値、Play中、World空間の非1は拒否します。
        /// </summary>
        public bool SetEffectScale(float value)
        {
            if (Application.isPlaying || !FluxParticleCoordinates.IsValidScale(value) ||
                (simulationSpace == FluxParticleSimulationSpace.World && value != 1)) return false;
            effectScale = value;
            return true;
        }

        /// <summary>
        /// 基準位置からワールド位置への変換
        /// </summary>
        public Vector3 ReferenceToWorld(Vector3 position) => FluxParticleCoordinates.ReferenceToWorld(position, transform, effectScale, simulationSpace);

        /// <summary>
        /// 外部ワールド位置から基準位置への変換
        /// </summary>
        public Vector3 WorldToReference(Vector3 position) => FluxParticleCoordinates.WorldToReference(position, transform, effectScale, simulationSpace);

        /// <summary>
        /// 基準長さから表示ローカル長さへの変換
        /// </summary>
        public float ReferenceLengthToDisplay(float length) => FluxParticleCoordinates.ReferenceLengthToDisplay(length, effectScale);

        /// <summary>
        /// 表示倍率を適用したローカルカリング範囲
        /// </summary>
        public Bounds DisplayCullingBounds => FluxParticleCoordinates.BoundsToDisplay(render.CullingBounds, effectScale);

        public FluxParticleSimulationSpace SimulationSpace => simulationSpace;

        /// <summary>
        /// System全体の追従設定。Play開始・ビルド時にRuntimeへベイク
        /// </summary>
        public FluxParticleFollowSettings SystemFollow => systemFollow;
        public PlaybackSettings Playback => playback;
        public EmissionSettings Emission => emission;
        public LifetimeSettings Lifetime => lifetime;
        public ShapeSettings Shape => shape;
        public InitialVelocitySettings InitialVelocity => initialVelocity;
        public RenderSettings Render => render;
        public IReadOnlyList<FluxParticleModule> Modules => modules;

        void Reset()
        {
            settingsVersion = 1;
        }
    }

    public enum FluxParticleStartSizeMode
    {
        Constant,
        RandomBetweenTwoConstants
    }

    public enum FluxParticleStartRotationMode
    {
        Constant,
        RandomBetweenTwoConstants
    }

    public enum FluxParticleStartColorMode
    {
        Constant,
        RandomBetweenTwoConstants
    }

    public enum FluxParticleStartLifetimeMode
    {
        Constant,
        RandomBetweenTwoConstants
    }

    public enum FluxParticleStartSpeedMode
    {
        Constant,
        RandomBetweenTwoConstants
    }

    public enum FluxParticleColorOverLifetimeMode
    {
        Linear,
        Gradient
    }

    public enum FluxParticleSizeOverLifetimeMode
    {
        Linear,
        Curve
    }

    public enum FluxParticleRotationOverLifetimeMode
    {
        Linear,
        Curve
    }

    [Serializable]
    public class PlaybackSettings
    {
        [SerializeField]
        [Min(0)]
        float startDelay;

        [SerializeField]
        [Min(0.01f)]
        float duration = 5.0f;

        [SerializeField]
        bool loop = true;

        public float StartDelay => startDelay;
        public float Duration => duration;
        public bool Loop => loop;
    }

    [Serializable]
    public class EmissionSettings
    {
        [SerializeField]
        [Min(0)]
        float rate = 8.0f;

        [SerializeField]
        BurstSettings[] bursts = Array.Empty<BurstSettings>();

        public float Rate => rate;
        public BurstSettings[] Bursts => bursts;
    }

    [Serializable]
    public class BurstSettings
    {
        [SerializeField]
        [Min(0)]
        float time;

        [SerializeField]
        [Min(1)]
        int count = 1;

        public float Time => time;
        public int Count => count;
    }

    [Serializable]
    public class LifetimeSettings
    {
        [SerializeField]
        [Min(0)]
        float lifetime = 3.0f;

        [SerializeField]
        FluxParticleStartLifetimeMode mode;

        [SerializeField]
        [Min(0)]
        float min = 1.0f;

        [SerializeField]
        [Min(0)]
        float max = 3.0f;

        public float Lifetime => lifetime;
        public FluxParticleStartLifetimeMode Mode => mode;
        public float Min => min;
        public float Max => max;
    }

    [Serializable]
    public class ShapeSettings
    {
        [SerializeField]
        FluxParticleShapeType type;

        [SerializeField]
        [Min(0)]
        float radius = 1.0f;

        [SerializeField]
        [Range(0, 90)]
        float angle = 25.0f;

        [SerializeField]
        Vector3 size = Vector3.one;

        public FluxParticleShapeType Type => type;
        public float Radius => radius;
        public float Angle => angle;
        public Vector3 Size => size;
    }

    [Serializable]
    public class InitialVelocitySettings
    {
        [SerializeField]
        [Min(0)]
        float speed = 1.0f;

        [SerializeField]
        FluxParticleStartSpeedMode mode;

        [SerializeField]
        [Min(0)]
        float min = 0.5f;

        [SerializeField]
        [Min(0)]
        float max = 1.0f;

        public float Speed => speed;
        public FluxParticleStartSpeedMode Mode => mode;
        public float Min => min;
        public float Max => max;
    }

    [Serializable]
    public class RenderSettings
    {
        [SerializeField]
        FluxParticleRenderMode mode;

        [SerializeField]
        Mesh mesh;

        [SerializeField]
        Texture2D texture;

        [SerializeField]
        Vector2 tiling = Vector2.one;

        [SerializeField]
        Vector2 offset;

        [SerializeField]
        FluxParticleBlendMode blendMode;

        [SerializeField]
        [Min(0)]
        float intensity = 1;

        [SerializeField]
        Texture2D intensityMask;

        [SerializeField]
        Bounds cullingBounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        [SerializeField]
        Color startColor = Color.white;

        [SerializeField]
        FluxParticleStartColorMode startColorMode;

        [SerializeField]
        Color startColorMin = Color.white;

        [SerializeField]
        Color startColorMax = Color.white;

        [SerializeField]
        [Min(0)]
        float startSize = 1.0f;

        [SerializeField]
        FluxParticleStartSizeMode startSizeMode;

        [SerializeField]
        [Min(0)]
        float startSizeMin = 0.5f;

        [SerializeField]
        [Min(0)]
        float startSizeMax = 1.0f;

        [SerializeField]
        Vector3 startRotation;

        [SerializeField]
        FluxParticleStartRotationMode startRotationMode;

        [SerializeField]
        Vector3 startRotationMin;

        [SerializeField]
        Vector3 startRotationMax = new Vector3(0, 0, 360);

        public FluxParticleRenderMode Mode => mode;
        public Mesh Mesh => mesh;
        public Texture2D Texture => texture;
        public Vector2 Tiling => tiling;
        public Vector2 Offset => offset;
        public FluxParticleBlendMode BlendMode => blendMode;
        public float Intensity => intensity;
        public Texture2D IntensityMask => intensityMask;
        public Bounds CullingBounds => cullingBounds;
        public Color StartColor => startColor;
        public FluxParticleStartColorMode StartColorMode => startColorMode;
        public Color StartColorMin => startColorMin;
        public Color StartColorMax => startColorMax;
        public float StartSize => startSize;
        public FluxParticleStartSizeMode StartSizeMode => startSizeMode;
        public float StartSizeMin => startSizeMin;
        public float StartSizeMax => startSizeMax;
        public Vector3 StartRotation => startRotation;
        public FluxParticleStartRotationMode StartRotationMode => startRotationMode;
        public Vector3 StartRotationMin => startRotationMin;
        public Vector3 StartRotationMax => startRotationMax;
    }
}
