using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Authoring
{
    /// <summary>
    /// Authoring設定とModuleの初期値からParameter初期値を構築します。
    /// </summary>
    public static class FluxParticleAuthoringInitialValues
    {
        /// <summary>
        /// Parameter初期値を構築し、値の妥当性を検証します。
        /// </summary>
        /// <param name="authoring">Particleの設定</param>
        /// <param name="moduleValues">Moduleが登録した初期値</param>
        /// <param name="hasRotation">Rotationの初期値を使用するか</param>
        /// <returns>論理Parameter IDに対応する初期値</returns>
        public static Dictionary<string, Vector4> Create(FluxParticleAuthoring authoring,
            IReadOnlyDictionary<string, Vector4> moduleValues, bool hasRotation)
        {
            var randomColor = authoring.Render.StartColorMode == FluxParticleStartColorMode.RandomBetweenTwoConstants;
            var randomSize = authoring.Render.StartSizeMode == FluxParticleStartSizeMode.RandomBetweenTwoConstants;
            var startRotation = authoring.Render.StartRotation;
            var life = authoring.Lifetime;
            var min = life.Mode == FluxParticleStartLifetimeMode.Constant ? life.Lifetime : Mathf.Min(life.Min, life.Max);
            var max = life.Mode == FluxParticleStartLifetimeMode.Constant ? life.Lifetime : Mathf.Max(life.Min, life.Max);
            var values = new Dictionary<string, Vector4>(StringComparer.Ordinal)
            {
                ["spawn.position"] = Vector4.zero,
                ["lifetime.range"] = new Vector4(min, max, 0, 0)
            };
            if (randomColor)
            {
                values.Add("color.start-min", authoring.Render.StartColorMin);
                values.Add("color.start-max", authoring.Render.StartColorMax);
            }
            else
            {
                values.Add("color.start", authoring.Render.StartColor);
            }
            if (randomSize)
            {
                values.Add("render.size-range", new Vector4(Mathf.Min(authoring.Render.StartSizeMin, authoring.Render.StartSizeMax),
                    Mathf.Max(authoring.Render.StartSizeMin, authoring.Render.StartSizeMax), 0, 0));
            }
            else
            {
                values.Add("render.size", new Vector4(authoring.Render.StartSize, 0, 0, 0));
            }
            if (hasRotation)
            {
                if (authoring.Render.StartRotationMode == FluxParticleStartRotationMode.RandomBetweenTwoConstants)
                {
                    values.Add("rotation.start-min", Vector3.Min(authoring.Render.StartRotationMin, authoring.Render.StartRotationMax));
                    values.Add("rotation.start-max", Vector3.Max(authoring.Render.StartRotationMin, authoring.Render.StartRotationMax));
                }
                else
                {
                    values.Add("rotation.start", startRotation);
                }
            }
            foreach (var parameter in moduleValues)
            {
                values[parameter.Key] = parameter.Value;
            }
            if (authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.RandomBetweenTwoConstants)
            {
                values.Add("spawn.speed-range", new Vector4(Mathf.Min(authoring.InitialVelocity.Min, authoring.InitialVelocity.Max),
                    Mathf.Max(authoring.InitialVelocity.Min, authoring.InitialVelocity.Max), 0, 0));
            }
            else
            {
                values.Add("spawn.speed", new Vector4(authoring.InitialVelocity.Speed, 0, 0, 0));
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Sphere || authoring.Shape.Type == FluxParticleShapeType.Hemisphere ||
                authoring.Shape.Type == FluxParticleShapeType.Circle || authoring.Shape.Type == FluxParticleShapeType.Cone)
            {
                values.Add("shape.radius", new Vector4(authoring.Shape.Radius, 0, 0, 0));
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Cone)
            {
                values.Add("shape.angle", new Vector4(authoring.Shape.Angle, 0, 0, 0));
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Box)
            {
                values.Add("shape.size", authoring.Shape.Size);
            }
            foreach (var value in values)
            {
                for (var c = 0; c < 4; c++)
                {
                    if (float.IsNaN(value.Value[c]) || float.IsInfinity(value.Value[c]))
                    {
                        throw new InvalidOperationException($"Non-finite Parameter: {value.Key}");
                    }
                }
            }
            return values;
        }

    }
}
