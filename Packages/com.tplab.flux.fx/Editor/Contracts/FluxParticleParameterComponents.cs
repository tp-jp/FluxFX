using System;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Contracts
{
    static class FluxParticleParameterComponents
    {
        internal static float[] Get<T>(T value) where T : struct
        {
            if (typeof(T) == typeof(float)) return new[] { (float)(object)value };
            if (typeof(T) == typeof(Vector2))
            {
                var vector = (Vector2)(object)value;
                return new[] { vector.x, vector.y };
            }
            if (typeof(T) == typeof(Vector3))
            {
                var vector = (Vector3)(object)value;
                return new[] { vector.x, vector.y, vector.z };
            }
            if (typeof(T) == typeof(Vector4))
            {
                var vector = (Vector4)(object)value;
                return new[] { vector.x, vector.y, vector.z, vector.w };
            }
            if (typeof(T) == typeof(Color))
            {
                var color = (Color)(object)value;
                return new[] { color.r, color.g, color.b, color.a };
            }
            throw new InvalidOperationException($"Not a numeric Parameter type: {typeof(T).FullName}");
        }
    }
}
