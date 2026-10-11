using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class FluxParticleGpuCompiler
    {
        const string PositionShaderPath = "Packages/com.tplab.flux.fx/Runtime/Shaders/ParticlePositionUpdate.shader";

        static readonly Regex IdentifierPattern = new Regex(
            @"^_[A-Za-z][A-Za-z0-9_]*$",
            RegexOptions.Compiled);

        static readonly Regex UniformPattern = new Regex(
            @"\b(?:float|float2|float3|float4|int|uint|sampler2D_float|sampler2D)\s+(_[A-Za-z0-9_]+)\s*;",
            RegexOptions.Compiled);

        readonly FluxParticleGpuModuleRegistry _registry;

        public FluxParticleGpuCompiler(FluxParticleGpuModuleRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public FluxParticleGpuCompileResult Compile(FluxParticleCompilePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var validation = new FluxParticleValidationResult();
            var standardValidation = new FluxParticleCompileValidator().Validate(plan);

            foreach (var issue in standardValidation.Issues)
            {
                validation.Add(issue.Code, issue.Message, issue.ModuleType, issue.Stage, issue.Severity);
            }

            var stageModules = new Dictionary<FluxParticleExecutionStage, List<FluxParticleGpuModuleCompilation>>();
            var stageNames = new Dictionary<FluxParticleExecutionStage, HashSet<string>>();
            var reservedNames = new Dictionary<FluxParticleExecutionStage, HashSet<string>>();

            foreach (var descriptor in FluxParticleGpuStageRegistry.GetAll())
            {
                stageModules.Add(descriptor.Stage, new List<FluxParticleGpuModuleCompilation>());
                stageNames.Add(descriptor.Stage, new HashSet<string>(StringComparer.Ordinal));
                reservedNames.Add(descriptor.Stage, CollectReservedNames(GetValidationShaderPath(descriptor)));
            }

            foreach (var module in plan.Modules)
            {
                if (!_registry.TryGet(module.GetType(), out var definition))
                {
                    if (IsLegacyModule(module.GetType())) continue;

                    AddError(validation, "G001", "GPU Module Definition is not registered.", module);
                    continue;
                }

                if (!FluxParticleGpuStageRegistry.TryGet(module.Stage, out _))
                {
                    AddError(validation, "G002", $"GPU Module stage is not supported: {module.Stage}.", module);
                    continue;
                }

                if (!TryCollectModule(
                    module,
                    definition,
                    reservedNames[module.Stage],
                    stageNames[module.Stage],
                    validation,
                    out var compiledModule))
                {
                    continue;
                }

                stageModules[module.Stage].Add(compiledModule);
            }

            if (validation.HasErrors)
            {
                return new FluxParticleGpuCompileResult(null, validation);
            }

            var stages = new List<FluxParticleGpuStageCompilation>();

            foreach (var descriptor in FluxParticleGpuStageRegistry.GetAll())
            {
                var modules = stageModules[descriptor.Stage];

                modules.Sort((a, b) =>
                {
                    var order = a.Order.CompareTo(b.Order);
                    return order != 0 ? order : string.CompareOrdinal(a.ModuleId, b.ModuleId);
                });

                stages.Add(new FluxParticleGpuStageCompilation(descriptor.Stage, modules));
            }

            return new FluxParticleGpuCompileResult(new FluxParticleGpuCompilation(stages), validation);
        }

        bool TryCollectModule(
            FluxParticleModule module,
            IFluxParticleGpuModuleDefinition definition,
            HashSet<string> reservedNames,
            HashSet<string> stageNames,
            FluxParticleValidationResult validation,
            out FluxParticleGpuModuleCompilation compiledModule)
        {
            compiledModule = null;

            var collector = new FluxParticleParameterCollector();

            try
            {
                definition.CollectParameters(module, collector);
            }
            catch (InvalidOperationException exception)
            {
                AddError(validation, "G005", exception.Message, module);
                return false;
            }
            catch (ArgumentException exception)
            {
                AddError(validation, "G003", exception.Message, module);
                return false;
            }

            var parameters = new List<FluxParticleGpuParameterCompilation>();

            foreach (var parameter in collector.Parameters)
            {
                parameters.Add(new FluxParticleGpuParameterCompilation(parameter));
            }

            var enabledName = FluxParticleGpuModuleEnabled.GetParameterName(definition.ModuleId);
            var enabledParameter = new FluxParticleGpuParameterCompilation(enabledName, module.Enabled ? 1.0f : 0.0f);

            var valid = true;

            foreach (var parameter in parameters)
            {
                if (!ValidateName(parameter.Name, module, reservedNames, stageNames, validation))
                {
                    valid = false;
                }
            }

            if (!ValidateName(enabledParameter.Name, module, reservedNames, stageNames, validation))
            {
                valid = false;
            }

            if (!valid) return false;

            compiledModule = new FluxParticleGpuModuleCompilation(
                definition,
                module.Stage,
                parameters,
                enabledParameter);

            return true;
        }

        static bool ValidateName(
            string name,
            FluxParticleModule module,
            HashSet<string> reservedNames,
            HashSet<string> stageNames,
            FluxParticleValidationResult validation)
        {
            var valid = true;

            if (!IdentifierPattern.IsMatch(name))
            {
                AddError(validation, "G003", $"Invalid GPU parameter name: {name}.", module);
                valid = false;
            }

            if (reservedNames.Contains(name))
            {
                AddError(validation, "G004", $"GPU parameter conflicts with a built-in Shader uniform: {name}.", module);
                valid = false;
            }

            if (!stageNames.Add(name))
            {
                AddError(validation, "G005", $"Duplicate GPU parameter: {name}", module);
                valid = false;
            }

            return valid;
        }

        static string GetValidationShaderPath(FluxParticleGpuStageDescriptor descriptor)
        {
            if (descriptor.Stage == FluxParticleExecutionStage.PositionUpdate)
            {
                return PositionShaderPath;
            }

            return descriptor.ShaderTemplatePath;
        }

        static HashSet<string> CollectReservedNames(string shaderPath)
        {
            var source = File.ReadAllText(shaderPath);
            var names = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match match in UniformPattern.Matches(source))
            {
                names.Add(match.Groups[1].Value);
            }

            return names;
        }

        static bool IsLegacyModule(Type type)
        {
            return type == typeof(FluxParticleVelocityOverLifetimeModule) ||
                   type == typeof(FluxParticleColorOverLifetimeModule) ||
                   type == typeof(FluxParticleSizeOverLifetimeModule) ||
                   type == typeof(FluxParticleRotationOverLifetimeModule) ||
                   type == typeof(FluxParticleTextureSheetAnimationModule);
        }

        static void AddError(
            FluxParticleValidationResult validation,
            string code,
            string message,
            FluxParticleModule module)
        {
            validation.Add(
                code,
                message,
                module.GetType(),
                module.Stage,
                FluxParticleValidationSeverity.Error);
        }
    }
}