using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TpLab.Flux.FX.Scripts;

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

            AddEmission(parameters, authoring.Emission);
            AddLifetime(parameters, authoring.Lifetime);
            AddShape(parameters, authoring.Shape);
            AddInitialVelocity(parameters, authoring.InitialVelocity);
            AddGravity(parameters, authoring.Gravity);
            AddDrag(parameters, authoring.Drag);
            AddNoise(parameters, authoring.Noise);
            AddVortex(parameters, authoring.Vortex);
            AddRender(parameters, authoring.Render);

            return parameters.ToString(Formatting.None);
        }

        void AddEmission(JObject parameters, EmissionSettings emission)
        {
            parameters["emission"] = new JObject
            {
                ["rate"] = emission.Rate
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
                ["radius"] = shape.Radius
            };
        }

        void AddInitialVelocity(JObject parameters, InitialVelocitySettings initialVelocity)
        {
            var speedMin = initialVelocity.Mode == FluxParticleInitialSpeedMode.Constant
                ? initialVelocity.Speed
                : UnityEngine.Mathf.Min(initialVelocity.Min, initialVelocity.Max);
            var speedMax = initialVelocity.Mode == FluxParticleInitialSpeedMode.Constant
                ? initialVelocity.Speed
                : UnityEngine.Mathf.Max(initialVelocity.Min, initialVelocity.Max);

            parameters["initialVelocity"] = new JObject
            {
                ["speed"] = initialVelocity.Speed,
                ["speedMin"] = speedMin,
                ["speedMax"] = speedMax
            };
        }

        void AddGravity(JObject parameters, GravitySettings gravity)
        {
            if (!gravity.Enabled) return;

            parameters["gravity"] = new JObject
            {
                ["x"] = gravity.Gravity.x,
                ["y"] = gravity.Gravity.y,
                ["z"] = gravity.Gravity.z
            };
        }

        void AddDrag(JObject parameters, DragSettings drag)
        {
            if (!drag.Enabled) return;

            parameters["drag"] = drag.Drag;
        }

        void AddNoise(JObject parameters, NoiseSettings noise)
        {
            if (!noise.Enabled) return;

            parameters["noise"] = new JObject
            {
                ["strength"] = noise.Strength,
                ["scale"] = noise.Scale,
                ["speed"] = noise.Speed
            };
        }

        void AddVortex(JObject parameters, VortexSettings vortex)
        {
            if (!vortex.Enabled) return;

            var axis = vortex.Axis.normalized;

            parameters["vortex"] = new JObject
            {
                ["centerX"] = vortex.Center.x,
                ["centerY"] = vortex.Center.y,
                ["centerZ"] = vortex.Center.z,
                ["axisX"] = axis.x,
                ["axisY"] = axis.y,
                ["axisZ"] = axis.z,
                ["strength"] = vortex.Strength
            };
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
                ["startColorMinR"] = startColorMin.r,
                ["startColorMinG"] = startColorMin.g,
                ["startColorMinB"] = startColorMin.b,
                ["startColorMaxR"] = startColorMax.r,
                ["startColorMaxG"] = startColorMax.g,
                ["startColorMaxB"] = startColorMax.b,
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
                    ["endColorB"] = render.ColorOverLifetime.EndColor.b
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

            parameters["render"] = settings;
        }
    }
}