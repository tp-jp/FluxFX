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
            var parameters = new JObject
            {
                ["simulationSpace"] = (int)authoring.SimulationSpace
            };

            AddPlayback(parameters, authoring.Playback);
            AddEmission(parameters, authoring.Emission);
            AddLifetime(parameters, authoring.Lifetime);
            AddShape(parameters, authoring.Shape);
            AddInitialVelocity(parameters, authoring.InitialVelocity);
            AddVelocityOverLifetime(parameters, authoring.VelocityOverLifetime);
            AddGravity(parameters, authoring);
            AddForce(parameters, authoring);
            AddDrag(parameters, authoring);
            AddNoise(parameters, authoring);
            AddVortex(parameters, authoring);
            AddLimitVelocity(parameters, authoring);
            AddRender(parameters, authoring.Render);

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

        void AddVelocityOverLifetime(JObject parameters, VelocityOverLifetimeSettings velocityOverLifetime)
        {
            if (!velocityOverLifetime.Enabled) return;

            parameters["velocityOverLifetime"] = new JObject
            {
                ["startX"] = velocityOverLifetime.Start.x,
                ["startY"] = velocityOverLifetime.Start.y,
                ["startZ"] = velocityOverLifetime.Start.z,
                ["endX"] = velocityOverLifetime.End.x,
                ["endY"] = velocityOverLifetime.End.y,
                ["endZ"] = velocityOverLifetime.End.z,
                ["orbitalX"] = velocityOverLifetime.Orbital.x,
                ["orbitalY"] = velocityOverLifetime.Orbital.y,
                ["orbitalZ"] = velocityOverLifetime.Orbital.z,
                ["offsetX"] = velocityOverLifetime.Offset.x,
                ["offsetY"] = velocityOverLifetime.Offset.y,
                ["offsetZ"] = velocityOverLifetime.Offset.z,
                ["radial"] = velocityOverLifetime.Radial
            };
        }

        void AddGravity(JObject parameters, FluxParticleAuthoring authoring)
        {
            foreach (var module in authoring.Modules)
            {
                if (!(module is FluxParticleGravityModule gravityModule)) continue;

                if (gravityModule.Enabled)
                {
                    var gravity = gravityModule.Gravity;

                    parameters["gravity"] = new JObject
                    {
                        ["x"] = gravity.x,
                        ["y"] = gravity.y,
                        ["z"] = gravity.z
                    };
                }

                return;
            }
        }

        void AddForce(JObject parameters, FluxParticleAuthoring authoring)
        {
            foreach (var module in authoring.Modules)
            {
                if (!(module is FluxParticleForceModule forceModule)) continue;

                if (forceModule.Enabled)
                {
                    var force = forceModule.Force;

                    parameters["force"] = new JObject
                    {
                        ["x"] = force.x,
                        ["y"] = force.y,
                        ["z"] = force.z
                    };
                }

                return;
            }
        }

        void AddDrag(JObject parameters, FluxParticleAuthoring authoring)
        {
            foreach (var module in authoring.Modules)
            {
                if (!(module is FluxParticleDragModule dragModule)) continue;

                if (dragModule.Enabled)
                {
                    parameters["drag"] = dragModule.Drag;
                }

                return;
            }
        }

        void AddNoise(JObject parameters, FluxParticleAuthoring authoring)
        {
            foreach (var module in authoring.Modules)
            {
                if (!(module is FluxParticleNoiseModule noiseModule)) continue;

                if (noiseModule.Enabled)
                {
                    parameters["noise"] = new JObject
                    {
                        ["strength"] = noiseModule.Strength,
                        ["scale"] = noiseModule.Scale,
                        ["speed"] = noiseModule.Speed
                    };
                }

                return;
            }
        }

        void AddVortex(JObject parameters, FluxParticleAuthoring authoring)
        {
            foreach (var module in authoring.Modules)
            {
                if (!(module is FluxParticleVortexModule vortexModule)) continue;

                if (vortexModule.Enabled)
                {
                    var center = vortexModule.Center;
                    var axis = vortexModule.Axis.normalized;

                    parameters["vortex"] = new JObject
                    {
                        ["centerX"] = center.x,
                        ["centerY"] = center.y,
                        ["centerZ"] = center.z,
                        ["axisX"] = axis.x,
                        ["axisY"] = axis.y,
                        ["axisZ"] = axis.z,
                        ["strength"] = vortexModule.Strength
                    };
                }

                return;
            }
        }

        void AddLimitVelocity(JObject parameters, FluxParticleAuthoring authoring)
        {
            foreach (var module in authoring.Modules)
            {
                if (!(module is FluxParticleLimitVelocityModule limitVelocityModule)) continue;

                if (limitVelocityModule.Enabled)
                {
                    parameters["limitVelocity"] = limitVelocityModule.MaxSpeed;
                }

                return;
            }
        }

        void AddRender(JObject parameters, RenderSettings render)
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

            if (render.ColorOverLifetime.Enabled)
            {
                settings["colorOverLifetime"] = new JObject
                {
                    ["mode"] = (int)render.ColorOverLifetime.Mode,
                    ["endColorR"] = render.ColorOverLifetime.EndColor.r,
                    ["endColorG"] = render.ColorOverLifetime.EndColor.g,
                    ["endColorB"] = render.ColorOverLifetime.EndColor.b,
                    ["endColorA"] = render.ColorOverLifetime.EndColor.a
                };
            }

            if (render.SizeOverLifetime.Enabled)
            {
                settings["sizeOverLifetime"] = new JObject
                {
                    ["mode"] = (int)render.SizeOverLifetime.Mode,
                    ["endSize"] = render.SizeOverLifetime.EndSize
                };
            }

            if (render.RotationOverLifetime.Enabled)
            {
                settings["rotationOverLifetime"] = new JObject
                {
                    ["mode"] = (int)render.RotationOverLifetime.Mode,
                    ["endRotation"] = render.RotationOverLifetime.EndRotation.z,
                    ["endRotationX"] = render.RotationOverLifetime.EndRotation.x,
                    ["endRotationY"] = render.RotationOverLifetime.EndRotation.y,
                    ["endRotationZ"] = render.RotationOverLifetime.EndRotation.z
                };
            }

            if (render.TextureSheetAnimation.Enabled)
            {
                settings["textureSheetAnimation"] = new JObject
                {
                    ["tilesX"] = render.TextureSheetAnimation.TilesX,
                    ["tilesY"] = render.TextureSheetAnimation.TilesY,
                    ["cycles"] = render.TextureSheetAnimation.Cycles
                };
            }

            parameters["render"] = settings;
        }
    }
}