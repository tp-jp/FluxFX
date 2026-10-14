using System;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// Particle Authoringの設定を検証します。
    /// </summary>
    public static class FluxParticleAuthoringValidator
    {
        /// <summary>
        /// サポート対象外の設定を検出します。
        /// </summary>
        /// <param name="authoring">検証対象。</param>
        public static void Validate(FluxParticleAuthoring authoring)
        {
            if (authoring == null)
            {
                throw new ArgumentNullException(nameof(authoring));
            }
            Require(authoring.SimulationSpace == FluxParticleSimulationSpace.Local, "Local simulation space");
            Require(authoring.Shape.Type == FluxParticleShapeType.Point || authoring.Shape.Type == FluxParticleShapeType.Sphere ||
                authoring.Shape.Type == FluxParticleShapeType.Hemisphere || authoring.Shape.Type == FluxParticleShapeType.Circle ||
                authoring.Shape.Type == FluxParticleShapeType.Box || authoring.Shape.Type == FluxParticleShapeType.Cone, "Supported shape");
            if (authoring.Shape.Type == FluxParticleShapeType.Sphere || authoring.Shape.Type == FluxParticleShapeType.Hemisphere ||
                authoring.Shape.Type == FluxParticleShapeType.Circle || authoring.Shape.Type == FluxParticleShapeType.Cone)
            {
                Require(FiniteNonNegative(authoring.Shape.Radius), "Non-negative shape radius");
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Cone)
            {
                Require(FiniteNonNegative(authoring.Shape.Angle) && authoring.Shape.Angle < 90f, "Cone angle in [0, 90) degrees");
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Box)
            {
                var size = authoring.Shape.Size;
                Require(FiniteNonNegative(size.x) && FiniteNonNegative(size.y) && FiniteNonNegative(size.z), "Non-negative Box size");
            }
            Require(authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.Constant ||
                authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.RandomBetweenTwoConstants, "Supported start speed mode");
            if (authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.RandomBetweenTwoConstants)
            {
                Require(FiniteNonNegative(authoring.InitialVelocity.Min) && FiniteNonNegative(authoring.InitialVelocity.Max),
                    "Finite non-negative start speed range");
            }
            Require(authoring.Render.Mode == FluxParticleRenderMode.Billboard, "Billboard rendering");
            Require(authoring.Render.StartColorMode == FluxParticleStartColorMode.Constant ||
                authoring.Render.StartColorMode == FluxParticleStartColorMode.RandomBetweenTwoConstants, "Supported start color mode");
            Require(authoring.Render.StartSizeMode == FluxParticleStartSizeMode.Constant ||
                authoring.Render.StartSizeMode == FluxParticleStartSizeMode.RandomBetweenTwoConstants, "Supported start size mode");
            var startRotation = authoring.Render.StartRotation;
            Require(startRotation.x == 0 && startRotation.y == 0 && !float.IsNaN(startRotation.z) &&
                !float.IsInfinity(startRotation.z), "Billboard start rotation around Z only");
            Require(authoring.Render.Material == null || authoring.Render.Material.shader.name == "FluxFX/ParticleRender", "FluxFX/ParticleRender material or no material");
            Require(FiniteNonNegative(authoring.Emission.Rate) && FiniteNonNegative(authoring.Playback.StartDelay) &&
                FiniteNonNegative(authoring.Playback.Duration) && authoring.Playback.Duration > 0, "Finite playback and emission settings");
            foreach (var burst in authoring.Emission.Bursts)
            {
                Require(burst != null && FiniteNonNegative(burst.Time) && burst.Count > 0, "Valid emission bursts");
            }
        }

        static bool FiniteNonNegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;

        static void Require(bool valid, string requirement)
        {
            if (valid) return;
            throw new InvalidOperationException($"Program Backend currently requires: {requirement}.");
        }
    }
}
