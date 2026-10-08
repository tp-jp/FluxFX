using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor
{
    public class FluxParticleCompiler
    {

        public string Compile(FluxParticleAuthoring authoring)
        {
            var plan = FluxParticleCompilePlan.Create(authoring);

            var validator = new FluxParticleCompileValidator();
            var validation = validator.Validate(plan);

            if (validation.HasErrors)
            {
                throw new InvalidOperationException(
                    $"FluxParticle compilation failed for '{authoring.name}':\n" +
                    validation.GetReport());
            }

            var parameters = new JObject
            {
                ["simulationSpace"] = (int)authoring.SimulationSpace
            };

            AddPlayback(parameters, authoring.Playback);
            AddEmission(parameters, authoring.Emission);
            AddLifetime(parameters, authoring.Lifetime);
            AddShape(parameters, authoring.Shape);
            AddInitialVelocity(parameters, authoring.InitialVelocity);

            AddVelocityOverLifetime(parameters, plan);
            AddGravity(parameters, plan);
            AddForce(parameters, plan);
            AddDrag(parameters, plan);
            AddNoise(parameters, plan);
            AddVortex(parameters, plan);
            AddLimitVelocity(parameters, plan);

            AddRender(parameters, authoring.Render, plan);

            return parameters.ToString(Formatting.None);
        }

        void AddPlayback(JObject parameters, PlaybackSettings playback)
        {
            parameters["playback"] = new JObject
            {
                ["startDelay"] = playback.StartDelay,
                ["duration"] = playback.Duration,
                ["loop"] = playback.Loop
            };
        }

        void AddEmission(JObject parameters, EmissionSettings emission)
        {
            var bursts = new JArray();

            foreach (var burst in emission.Bursts)
            {
                bursts.Add(new JObject
                {
                    ["time"] = burst.Time,
                    ["count"] = burst.Count
                });
            }

            parameters["emission"] = new JObject
            {
                ["rate"] = emission.Rate,
                ["bursts"] = bursts
            };
        }

        void AddLifetime(JObject parameters, LifetimeSettings lifetime)
        {
            var lifetimeMin = lifetime.Mode == FluxParticleStartLifetimeMode.Constant
                ? lifetime.Lifetime
                : UnityEngine.Mathf.Min(lifetime.Min, lifetime.Max);

            var lifetimeMax = lifetime.Mode == FluxParticleStartLifetimeMode.Constant
                ? lifetime.Lifetime
                : UnityEngine.Mathf.Max(lifetime.Min, lifetime.Max);

            parameters["lifetime"] = lifetime.Lifetime;
            parameters["lifetimeMin"] = lifetimeMin;
            parameters["lifetimeMax"] = lifetimeMax;
        }

        void AddShape(JObject parameters, ShapeSettings shape)
        {
            parameters["shape"] = new JObject
            {
                ["type"] = (int)shape.Type,
                ["radius"] = shape.Radius,
                ["angle"] = shape.Angle,
                ["sizeX"] = shape.Size.x,
                ["sizeY"] = shape.Size.y,
                ["sizeZ"] = shape.Size.z
            };
        }

        void AddInitialVelocity(JObject parameters, InitialVelocitySettings initialVelocity)
        {
            var speedMin = initialVelocity.Mode == FluxParticleStartSpeedMode.Constant
                ? initialVelocity.Speed
                : UnityEngine.Mathf.Min(initialVelocity.Min, initialVelocity.Max);

            var speedMax = initialVelocity.Mode == FluxParticleStartSpeedMode.Constant
                ? initialVelocity.Speed
                : UnityEngine.Mathf.Max(initialVelocity.Min, initialVelocity.Max);

            parameters["initialVelocity"] = new JObject
            {
                ["speed"] = initialVelocity.Speed,
                ["speedMin"] = speedMin,
                ["speedMax"] = speedMax
            };
        }

        void AddVelocityOverLifetime(JObject parameters, FluxParticleCompilePlan plan)
        {
            var module = plan.GetModule<FluxParticleVelocityOverLifetimeModule>();
            if (module == null) return;

            parameters["velocityOverLifetime"] = new JObject
            {
                ["startX"] = module.Start.x,
                ["startY"] = module.Start.y,
                ["startZ"] = module.Start.z,
                ["endX"] = module.End.x,
                ["endY"] = module.End.y,
                ["endZ"] = module.End.z,
                ["orbitalX"] = module.Orbital.x,
                ["orbitalY"] = module.Orbital.y,
                ["orbitalZ"] = module.Orbital.z,
                ["offsetX"] = module.Offset.x,
                ["offsetY"] = module.Offset.y,
                ["offsetZ"] = module.Offset.z,
                ["radial"] = module.Radial
            };
        }

        void AddGravity(JObject parameters, FluxParticleCompilePlan plan)
        {
            var module = plan.GetModule<FluxParticleGravityModule>();
            if (module == null) return;

            var gravity = module.Gravity;

            parameters["gravity"] = new JObject
            {
                ["x"] = gravity.x,
                ["y"] = gravity.y,
                ["z"] = gravity.z
            };
        }

        void AddForce(JObject parameters, FluxParticleCompilePlan plan)
        {
            var module = plan.GetModule<FluxParticleForceModule>();
            if (module == null) return;

            var force = module.Force;

            parameters["force"] = new JObject
            {
                ["x"] = force.x,
                ["y"] = force.y,
                ["z"] = force.z
            };
        }

        void AddDrag(JObject parameters, FluxParticleCompilePlan plan)
        {
            var module = plan.GetModule<FluxParticleDragModule>();
            if (module == null) return;

            parameters["drag"] = module.Drag;
        }

        void AddNoise(JObject parameters, FluxParticleCompilePlan plan)
        {
            var module = plan.GetModule<FluxParticleNoiseModule>();
            if (module == null) return;

            parameters["noise"] = new JObject
            {
                ["strength"] = module.Strength,
                ["scale"] = module.Scale,
                ["speed"] = module.Speed
            };
        }

        void AddVortex(JObject parameters, FluxParticleCompilePlan plan)
        {
            var module = plan.GetModule<FluxParticleVortexModule>();
            if (module == null) return;

            var center = module.Center;
            var axis = module.Axis.normalized;

            parameters["vortex"] = new JObject
            {
                ["centerX"] = center.x,
                ["centerY"] = center.y,
                ["centerZ"] = center.z,
                ["axisX"] = axis.x,
                ["axisY"] = axis.y,
                ["axisZ"] = axis.z,
                ["strength"] = module.Strength
            };
        }

        void AddLimitVelocity(JObject parameters, FluxParticleCompilePlan plan)
        {
            var module = plan.GetModule<FluxParticleLimitVelocityModule>();
            if (module == null) return;

            parameters["limitVelocity"] = module.MaxSpeed;
        }

        void AddRender(JObject parameters, RenderSettings render, FluxParticleCompilePlan plan)
        {
            var startColorMin = render.StartColorMode == FluxParticleStartColorMode.Constant
                ? render.StartColor
                : render.StartColorMin;

            var startColorMax = render.StartColorMode == FluxParticleStartColorMode.Constant
                ? render.StartColor
                : render.StartColorMax;

            var settings = new JObject
            {
                ["startColorR"] = render.StartColor.r,
                ["startColorG"] = render.StartColor.g,
                ["startColorB"] = render.StartColor.b,
                ["startColorA"] = render.StartColor.a,
                ["startColorMinR"] = startColorMin.r,
                ["startColorMinG"] = startColorMin.g,
                ["startColorMinB"] = startColorMin.b,
                ["startColorMinA"] = startColorMin.a,
                ["startColorMaxR"] = startColorMax.r,
                ["startColorMaxG"] = startColorMax.g,
                ["startColorMaxB"] = startColorMax.b,
                ["startColorMaxA"] = startColorMax.a,
                ["startSize"] = render.StartSize,
                ["startSizeMin"] = render.StartSizeMode == FluxParticleStartSizeMode.Constant
                    ? render.StartSize
                    : UnityEngine.Mathf.Min(render.StartSizeMin, render.StartSizeMax),
                ["startSizeMax"] = render.StartSizeMode == FluxParticleStartSizeMode.Constant
                    ? render.StartSize
                    : UnityEngine.Mathf.Max(render.StartSizeMin, render.StartSizeMax),
                ["startRotationX"] = render.StartRotation.x,
                ["startRotationY"] = render.StartRotation.y,
                ["startRotationZ"] = render.StartRotation.z
            };

            AddColorOverLifetime(settings, plan);
            AddSizeOverLifetime(settings, plan);
            AddRotationOverLifetime(settings, plan);
            AddTextureSheetAnimation(settings, plan);

            parameters["render"] = settings;
        }

        void AddColorOverLifetime(JObject settings, FluxParticleCompilePlan plan)
        {
            var module = plan.GetModule<FluxParticleColorOverLifetimeModule>();
            if (module == null) return;

            settings["colorOverLifetime"] = new JObject
            {
                ["mode"] = (int)module.Mode,
                ["endColorR"] = module.EndColor.r,
                ["endColorG"] = module.EndColor.g,
                ["endColorB"] = module.EndColor.b,
                ["endColorA"] = module.EndColor.a
            };
        }

        void AddSizeOverLifetime(JObject settings, FluxParticleCompilePlan plan)
        {
            var module = plan.GetModule<FluxParticleSizeOverLifetimeModule>();
            if (module == null) return;

            settings["sizeOverLifetime"] = new JObject
            {
                ["mode"] = (int)module.Mode,
                ["endSize"] = module.EndSize
            };
        }

        void AddRotationOverLifetime(JObject settings, FluxParticleCompilePlan plan)
        {
            var module = plan.GetModule<FluxParticleRotationOverLifetimeModule>();
            if (module == null) return;

            var endRotation = module.EndRotation;

            settings["rotationOverLifetime"] = new JObject
            {
                ["mode"] = (int)module.Mode,
                ["endRotation"] = endRotation.z,
                ["endRotationX"] = endRotation.x,
                ["endRotationY"] = endRotation.y,
                ["endRotationZ"] = endRotation.z
            };
        }

        void AddTextureSheetAnimation(JObject settings, FluxParticleCompilePlan plan)
        {
            var module = plan.GetModule<FluxParticleTextureSheetAnimationModule>();
            if (module == null) return;

            settings["textureSheetAnimation"] = new JObject
            {
                ["tilesX"] = module.TilesX,
                ["tilesY"] = module.TilesY,
                ["cycles"] = module.Cycles
            };
        }
    }
}
