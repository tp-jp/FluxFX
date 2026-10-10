using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class FluxParticleGpuModuleValidator
    {
        const string VelocityShaderPath = "Packages/com.tplab.flux.fx/Runtime/Shaders/Templates/ParticleVelocityUpdate.shader.template";

        static readonly Regex IdentifierPattern = new Regex(@"^_[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);
        static readonly Regex UniformPattern = new Regex(@"\b(?:float|float2|float3|float4|int|uint|sampler2D_float|sampler2D)\s+(_[A-Za-z0-9_]+)\s*;", RegexOptions.Compiled);

        readonly FluxParticleGpuModuleRegistry _registry;

        public FluxParticleGpuModuleValidator(FluxParticleGpuModuleRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public FluxParticleValidationResult Validate(FluxParticleCompilePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var result = new FluxParticleValidationResult();
            var collector = new FluxParticleParameterCollector();
            var source = File.ReadAllText(VelocityShaderPath);
            var reservedNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match match in UniformPattern.Matches(source))
            {
                reservedNames.Add(match.Groups[1].Value);
            }

            foreach (var module in plan.Modules)
            {
                var type = module.GetType();
                var stage = module.Stage;

                if (!_registry.TryGet(type, out var definition))
                {
                    if (IsLegacyModule(type)) continue;

                    result.Add(
                        "G001",
                        "GPU Module Definition is not registered.",
                        type,
                        stage,
                        FluxParticleValidationSeverity.Error);

                    continue;
                }

                if (stage != FluxParticleExecutionStage.VelocityUpdate)
                {
                    result.Add(
                        "G002",
                        $"GPU Module stage is not supported by the Shader Compiler: {stage}.",
                        type,
                        stage,
                        FluxParticleValidationSeverity.Error);

                    continue;
                }

                var previousCount = collector.Parameters.Count;

                try
                {
                    definition.CollectParameters(module, collector);
                }
                catch (InvalidOperationException exception)
                {
                    result.Add(
                        "G005",
                        exception.Message,
                        type,
                        stage,
                        FluxParticleValidationSeverity.Error);

                    continue;
                }
                catch (ArgumentException exception)
                {
                    result.Add(
                        "G003",
                        exception.Message,
                        type,
                        stage,
                        FluxParticleValidationSeverity.Error);

                    continue;
                }

                for (var i = previousCount; i < collector.Parameters.Count; i++)
                {
                    var parameter = collector.Parameters[i];

                    if (!IdentifierPattern.IsMatch(parameter.Name))
                    {
                        result.Add(
                            "G003",
                            $"Invalid GPU parameter name: {parameter.Name}.",
                            type,
                            stage,
                            FluxParticleValidationSeverity.Error);
                    }

                    if (reservedNames.Contains(parameter.Name))
                    {
                        result.Add(
                            "G004",
                            $"GPU parameter conflicts with a built-in Shader uniform: {parameter.Name}.",
                            type,
                            stage,
                            FluxParticleValidationSeverity.Error);
                    }
                }
            }

            return result;
        }

        static bool IsLegacyModule(Type type)
        {
            return type == typeof(FluxParticleVelocityOverLifetimeModule) ||
                   type == typeof(FluxParticleNoiseModule) ||
                   type == typeof(FluxParticleVortexModule) ||
                   type == typeof(FluxParticleLimitVelocityModule) ||
                   type == typeof(FluxParticleColorOverLifetimeModule) ||
                   type == typeof(FluxParticleSizeOverLifetimeModule) ||
                   type == typeof(FluxParticleRotationOverLifetimeModule) ||
                   type == typeof(FluxParticleTextureSheetAnimationModule);
        }
    }
}