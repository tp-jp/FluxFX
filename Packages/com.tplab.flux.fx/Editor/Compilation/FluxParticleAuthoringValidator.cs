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
            Require(authoring.SimulationSpace == FluxParticleSimulationSpace.Local ||
                authoring.SimulationSpace == FluxParticleSimulationSpace.World, "Supported simulation space");
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
            if (authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.Constant)
            {
                Require(FiniteNonNegative(authoring.InitialVelocity.Speed), "Finite non-negative start speed");
            }
            Require(authoring.Lifetime.Mode == FluxParticleStartLifetimeMode.Constant ||
                authoring.Lifetime.Mode == FluxParticleStartLifetimeMode.RandomBetweenTwoConstants, "Supported start lifetime mode");
            Require(authoring.Lifetime.Mode == FluxParticleStartLifetimeMode.Constant
                ? FiniteNonNegative(authoring.Lifetime.Lifetime)
                : FiniteNonNegative(authoring.Lifetime.Min) && FiniteNonNegative(authoring.Lifetime.Max),
                "Finite non-negative lifetime");
            Require(authoring.Render.Mode == FluxParticleRenderMode.Billboard || authoring.Render.Mode == FluxParticleRenderMode.Mesh,
                "Supported render mode");
            if (authoring.Render.Mode == FluxParticleRenderMode.Mesh)
            {
                var mesh = authoring.Render.Mesh;
                Require(mesh != null && mesh.isReadable && mesh.vertexCount > 0, "Readable non-empty render Mesh");
                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    Require(mesh.GetTopology(subMesh) == MeshTopology.Triangles, "Triangle render Mesh");
                }
                Require(mesh.triangles.Length > 0, "Render Mesh with triangles");
            }
            Require(authoring.Render.BlendMode == FluxParticleBlendMode.Opaque ||
                authoring.Render.BlendMode == FluxParticleBlendMode.Alpha ||
                authoring.Render.BlendMode == FluxParticleBlendMode.Additive, "Supported blend mode");
            Require(authoring.Render.StartColorMode == FluxParticleStartColorMode.Constant ||
                authoring.Render.StartColorMode == FluxParticleStartColorMode.RandomBetweenTwoConstants, "Supported start color mode");
            Require(authoring.Render.StartSizeMode == FluxParticleStartSizeMode.Constant ||
                authoring.Render.StartSizeMode == FluxParticleStartSizeMode.RandomBetweenTwoConstants, "Supported start size mode");
            Require(authoring.Render.StartSizeMode == FluxParticleStartSizeMode.Constant
                ? FiniteNonNegative(authoring.Render.StartSize)
                : FiniteNonNegative(authoring.Render.StartSizeMin) && FiniteNonNegative(authoring.Render.StartSizeMax),
                "Finite non-negative start size");
            Require(authoring.Render.StartRotationMode == FluxParticleStartRotationMode.Constant ||
                authoring.Render.StartRotationMode == FluxParticleStartRotationMode.RandomBetweenTwoConstants,
                "Supported start rotation mode");
            if (authoring.Render.StartRotationMode == FluxParticleStartRotationMode.Constant)
            {
                ValidateStartRotation(authoring.Render.StartRotation, authoring.Render.Mode);
            }
            else
            {
                ValidateStartRotation(authoring.Render.StartRotationMin, authoring.Render.Mode);
                ValidateStartRotation(authoring.Render.StartRotationMax, authoring.Render.Mode);
            }
            Require(authoring.Render.Material == null || authoring.Render.Material.shader.name == "FluxFX/ParticleMaterial", "FluxFX/ParticleMaterial material or no material");
            Require(FiniteNonNegative(authoring.Emission.Rate) && FiniteNonNegative(authoring.Playback.StartDelay),
                "Finite playback and emission settings");
            Require(FiniteNonNegative(authoring.Playback.Duration) &&
                authoring.Playback.Duration >= FluxParticleEmissionScheduler.MinimumDuration, "Duration of at least 0.01 seconds");
            foreach (var burst in authoring.Emission.Bursts)
            {
                Require(burst != null && FiniteNonNegative(burst.Time) && burst.Count > 0, "Valid emission bursts");
            }
        }

        static void ValidateStartRotation(Vector3 rotation, FluxParticleRenderMode mode)
        {
            Require(!float.IsNaN(rotation.x) && !float.IsInfinity(rotation.x) &&
                !float.IsNaN(rotation.y) && !float.IsInfinity(rotation.y) &&
                !float.IsNaN(rotation.z) && !float.IsInfinity(rotation.z), "Finite start rotation");
            if (mode == FluxParticleRenderMode.Billboard)
            {
                Require(rotation.x == 0 && rotation.y == 0, "Billboard start rotation around Z only");
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
