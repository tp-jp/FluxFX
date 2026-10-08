
using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts
{
    [DisallowMultipleComponent]
    public class FluxParticleAuthoring : MonoBehaviour
    {
        [SerializeField]
        FluxParticleSimulationSpace simulationSpace;

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

        public FluxParticleSimulationSpace SimulationSpace => simulationSpace;

        public PlaybackSettings Playback => playback;

        public EmissionSettings Emission => emission;

        public LifetimeSettings Lifetime => lifetime;

        public ShapeSettings Shape => shape;

        public InitialVelocitySettings InitialVelocity => initialVelocity;

        public RenderSettings Render => render;

        public IReadOnlyList<FluxParticleModule> Modules => modules;
    }

    public enum FluxParticleStartSizeMode
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
        Material material;

        [SerializeField]
        FluxParticleBlendMode blendMode;

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
        SizeOverLifetimeSettings sizeOverLifetime = new SizeOverLifetimeSettings();

        [SerializeField]
        RotationOverLifetimeSettings rotationOverLifetime = new RotationOverLifetimeSettings();

        public FluxParticleRenderMode Mode => mode;

        public Mesh Mesh => mesh;

        public Material Material => material;

        public FluxParticleBlendMode BlendMode => blendMode;

        public Color StartColor => startColor;

        public FluxParticleStartColorMode StartColorMode => startColorMode;

        public Color StartColorMin => startColorMin;

        public Color StartColorMax => startColorMax;

        public float StartSize => startSize;

        public FluxParticleStartSizeMode StartSizeMode => startSizeMode;

        public float StartSizeMin => startSizeMin;

        public float StartSizeMax => startSizeMax;

        public Vector3 StartRotation => startRotation;

        public SizeOverLifetimeSettings SizeOverLifetime => sizeOverLifetime;

        public RotationOverLifetimeSettings RotationOverLifetime => rotationOverLifetime;
    }

    [Serializable]
    public class SizeOverLifetimeSettings
    {
        [SerializeField]
        bool enabled;

        [SerializeField]
        FluxParticleSizeOverLifetimeMode mode;

        [SerializeField]
        [Min(0)]
        float endSize;

        [SerializeField]
        AnimationCurve curve = AnimationCurve.Linear(0, 1, 1, 1);

        public bool Enabled => enabled;

        public FluxParticleSizeOverLifetimeMode Mode => mode;

        public float EndSize => endSize;

        public AnimationCurve Curve => curve;
    }

    [Serializable]
    public class RotationOverLifetimeSettings
    {
        [SerializeField]
        bool enabled;

        [SerializeField]
        FluxParticleRotationOverLifetimeMode mode;

        [SerializeField]
        Vector3 endRotation;

        [SerializeField]
        AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        public bool Enabled => enabled;

        public FluxParticleRotationOverLifetimeMode Mode => mode;

        public Vector3 EndRotation => endRotation;

        public AnimationCurve Curve => curve;
    }
}
