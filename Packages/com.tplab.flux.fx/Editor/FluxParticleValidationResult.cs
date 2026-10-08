using System;
using System.Collections.Generic;
using System.Text;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor
{
    public enum FluxParticleValidationSeverity
    {
        Warning,
        Error
    }

    public sealed class FluxParticleValidationIssue
    {
        public string Code { get; }

        public string Message { get; }

        public Type ModuleType { get; }

        public FluxParticleExecutionStage Stage { get; }

        public FluxParticleValidationSeverity Severity { get; }

        public FluxParticleValidationIssue(
            string code,
            string message,
            Type moduleType,
            FluxParticleExecutionStage stage,
            FluxParticleValidationSeverity severity)
        {
            Code = code;
            Message = message;
            ModuleType = moduleType;
            Stage = stage;
            Severity = severity;
        }

        public override string ToString()
        {
            var moduleName = ModuleType != null
                ? ModuleType.Name
                : "Unknown";

            return $"[{Code}] {Severity}: {moduleName} ({Stage}) - {Message}";
        }
    }

    public sealed class FluxParticleValidationResult
    {
        readonly List<FluxParticleValidationIssue> _issues =
            new List<FluxParticleValidationIssue>();

        readonly IReadOnlyList<FluxParticleValidationIssue> _readOnlyIssues;

        public IReadOnlyList<FluxParticleValidationIssue> Issues => _readOnlyIssues;

        public bool HasErrors { get; private set; }

        public bool IsValid => !HasErrors;

        public FluxParticleValidationResult()
        {
            _readOnlyIssues = _issues.AsReadOnly();
        }

        public void Add(
            string code,
            string message,
            Type moduleType,
            FluxParticleExecutionStage stage,
            FluxParticleValidationSeverity severity)
        {
            _issues.Add(new FluxParticleValidationIssue(
                code,
                message,
                moduleType,
                stage,
                severity));

            if (severity == FluxParticleValidationSeverity.Error)
            {
                HasErrors = true;
            }
        }

        public string GetReport()
        {
            if (_issues.Count == 0)
            {
                return "FluxParticle validation passed.";
            }

            var builder = new StringBuilder();

            foreach (var issue in _issues)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(issue);
            }

            return builder.ToString();
        }
    }
}
