using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts
{
    [DisallowMultipleComponent]
    public class FluxParticleAuthoring : MonoBehaviour
    {
        [SerializeField]
        EmissionSettings emission = new EmissionSettings();

        [SerializeField]
        LifetimeSettings lifetime = new LifetimeSettings();

        [SerializeField]
        ShapeSettings shape = new ShapeSettings();

        [SerializeField]
        InitialVelocitySettings initialVelocity = new InitialVelocitySettings();

        [SerializeField]
        GravitySettings gravity = new GravitySettings();

        [SerializeField]
        DragSettings drag = new DragSettings();

        [SerializeField]
        NoiseSettings noise = new NoiseSettings();

        [SerializeField]
        VortexSettings vortex = new VortexSettings();

        [SerializeField]
        RenderSettings render = new RenderSettings();

        public EmissionSettings Emission => emission;

        public LifetimeSettings Lifetime => lifetime;

        public ShapeSettings Shape => shape;

        public InitialVelocitySettings InitialVelocity => initialVelocity;

        public GravitySettings Gravity => gravity;

        public DragSettings Drag => drag;

        public NoiseSettings Noise => noise;

        public VortexSettings Vortex => vortex;

        public RenderSettings Render => render;
    }

    public enum FluxParticleShapeType
    {
        Point,
        Sphere
    }

    [Serializable]
    public class EmissionSettings
    {
        [SerializeField]
        [Min(0)]
        float rate = 8.0f;

        public float Rate => rate;
    }

    [Serializable]
    public class LifetimeSettings
    {
        [SerializeField]
        [Min(0)]
        float lifetime = 3.0f;

        public float Lifetime => lifetime;
    }

    [Serializable]
    public class ShapeSettings
    {
        [SerializeField]
        FluxParticleShapeType type;

        [SerializeField]
        [Min(0)]
        float radius = 1.0f;

        public FluxParticleShapeType Type => type;

        public float Radius => radius;
    }

    [Serializable]
    public class InitialVelocitySettings
    {
        [SerializeField]
        [Min(0)]
        float speed = 1.0f;

        public float Speed => speed;
    }

    [Serializable]
    public class GravitySettings
    {
        [SerializeField]
        bool enabled;

        [SerializeField]
        Vector3 gravity = new Vector3(0, -1.0f, 0);

        public bool Enabled => enabled;

        public Vector3 Gravity => gravity;
    }

    [Serializable]
    public class DragSettings
    {
        [SerializeField]
        bool enabled;

        [SerializeField]
        [Min(0)]
        float drag = 0.5f;

        public bool Enabled => enabled;

        public float Drag => drag;
    }

    [Serializable]
    public class NoiseSettings
    {
        [SerializeField]
        bool enabled;

        [SerializeField]
        [Min(0)]
        float strength = 1.0f;

        [SerializeField]
        [Min(0)]
        float scale = 1.0f;

        [SerializeField]
        [Min(0)]
        float speed = 1.0f;

        public bool Enabled => enabled;

        public float Strength => strength;

        public float Scale => scale;

        public float Speed => speed;
    }

    [Serializable]
    public class VortexSettings
    {
        [SerializeField]
        bool enabled;

        [SerializeField]
        Vector3 center;

        [SerializeField]
        Vector3 axis = Vector3.up;

        [SerializeField]
        [Min(0)]
        float strength = 1.0f;

        public bool Enabled => enabled;

        public Vector3 Center => center;

        public Vector3 Axis => axis;

        public float Strength => strength;
    }

    [Serializable]
    public class RenderSettings
    {
        [SerializeField]
        Color startColor = Color.white;

        [SerializeField]
        [Min(0)]
        float startSize = 1.0f;

        [SerializeField]
        ColorOverLifetimeSettings colorOverLifetime = new ColorOverLifetimeSettings();

        [SerializeField]
        SizeOverLifetimeSettings sizeOverLifetime = new SizeOverLifetimeSettings();

        public Color StartColor => startColor;

        public float StartSize => startSize;

        public ColorOverLifetimeSettings ColorOverLifetime => colorOverLifetime;

        public SizeOverLifetimeSettings SizeOverLifetime => sizeOverLifetime;
    }

    [Serializable]
    public class ColorOverLifetimeSettings
    {
        [SerializeField]
        bool enabled;

        [SerializeField]
        Color endColor = Color.white;

        public bool Enabled => enabled;

        public Color EndColor => endColor;
    }

    [Serializable]
    public class SizeOverLifetimeSettings
    {
        [SerializeField]
        bool enabled;

        [SerializeField]
        [Min(0)]
        float endSize;

        public bool Enabled => enabled;

        public float EndSize => endSize;
    }
}