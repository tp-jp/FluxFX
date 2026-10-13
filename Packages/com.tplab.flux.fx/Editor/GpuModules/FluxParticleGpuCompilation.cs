using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class FluxParticleGpuParameterCompilation
    {
        public string Name { get; }
        public FluxParticleParameterType Type { get; }
        public float FloatValue { get; }
        public Vector4 VectorValue { get; }

        internal FluxParticleGpuParameterCompilation(FluxParticleParameterCollector.Parameter parameter)
        {
            Name = parameter.Name;
            Type = parameter.Type;
            FloatValue = parameter.FloatValue;
            VectorValue = parameter.VectorValue;
        }

        internal FluxParticleGpuParameterCompilation(string name, float value)
        {
            Name = name;
            Type = FluxParticleParameterType.Float;
            FloatValue = value;
        }
    }

    public sealed class FluxParticleGpuModuleCompilation
    {
        public string ModuleId { get; }
        public Type ModuleType { get; }
        public FluxParticleExecutionStage Stage { get; }
        public int Order { get; }
        public string HlslPath { get; }
        public string EntryPoint { get; }

        public FluxParticleGpuParameterCompilation EnabledParameter { get; }
        public IReadOnlyList<FluxParticleGpuParameterCompilation> Parameters { get; }

        internal FluxParticleGpuModuleCompilation(
            IFluxParticleGpuModuleDefinition definition,
            FluxParticleExecutionStage stage,
            IReadOnlyList<FluxParticleGpuParameterCompilation> parameters,
            FluxParticleGpuParameterCompilation enabledParameter)
        {
            ModuleId = definition.ModuleId;
            ModuleType = definition.ModuleType;
            Stage = stage;
            Order = definition.Order;
            HlslPath = definition.HlslPath.Replace('\\', '/');
            EntryPoint = definition.EntryPoint;
            EnabledParameter = enabledParameter;

            Parameters = new List<FluxParticleGpuParameterCompilation>(parameters).AsReadOnly();
        }
    }

    public sealed class FluxParticleGpuStageCompilation
    {
        public FluxParticleExecutionStage Stage { get; }
        public IReadOnlyList<FluxParticleGpuModuleCompilation> Modules { get; }
        public IReadOnlyList<FluxParticleGpuParameterCompilation> Parameters { get; }

        internal FluxParticleGpuStageCompilation(
            FluxParticleExecutionStage stage,
            IReadOnlyList<FluxParticleGpuModuleCompilation> modules)
        {
            Stage = stage;
            Modules = new List<FluxParticleGpuModuleCompilation>(modules).AsReadOnly();

            var parameters = new List<FluxParticleGpuParameterCompilation>();

            foreach (var module in Modules)
            {
                parameters.AddRange(module.Parameters);
                parameters.Add(module.EnabledParameter);
            }

            parameters.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            Parameters = parameters.AsReadOnly();
        }
    }

    public sealed class FluxParticleGpuCompilation
    {
        public IReadOnlyList<FluxParticleGpuStageCompilation> Stages { get; }

        internal FluxParticleGpuCompilation(IReadOnlyList<FluxParticleGpuStageCompilation> stages)
        {
            Stages = new List<FluxParticleGpuStageCompilation>(stages).AsReadOnly();
        }

        public FluxParticleGpuStageCompilation GetStage(FluxParticleExecutionStage stage)
        {
            foreach (var entry in Stages)
            {
                if (entry.Stage == stage) return entry;
            }

            return null;
        }
    }

    public sealed class FluxParticleGpuCompileResult
    {
        public FluxParticleGpuCompilation Compilation { get; }
        public FluxParticleValidationResult Validation { get; }

        public bool Success => Compilation != null && !Validation.HasErrors;

        internal FluxParticleGpuCompileResult(
            FluxParticleGpuCompilation compilation,
            FluxParticleValidationResult validation)
        {
            Compilation = compilation;
            Validation = validation;
        }
    }
}