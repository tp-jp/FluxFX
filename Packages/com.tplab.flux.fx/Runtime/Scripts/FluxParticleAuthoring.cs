using System;
using UnityEngine;
using VRC.SDKBase;

namespace TpLab.Flux.FX.Scripts
{
    [DisallowMultipleComponent]
    public class FluxParticleAuthoring : MonoBehaviour, IEditorOnly
    {
        [SerializeField]
        GravitySettings gravity = new GravitySettings();

        [SerializeField]
        DragSettings drag = new DragSettings();

        [SerializeField]
        NoiseSettings noise = new NoiseSettings();

        [SerializeField]
        VortexSettings vortex = new VortexSettings();

        public GravitySettings Gravity => gravity;

        public DragSettings Drag => drag;

        public NoiseSettings Noise => noise;

        public VortexSettings Vortex => vortex;
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
}