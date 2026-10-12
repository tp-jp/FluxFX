using System;
using System.IO;
using System.Text.RegularExpressions;
using TpLab.Flux.FX.Editor.Shaders.Dependencies;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class FluxParticleGpuModuleSourceValidator
    {
        readonly ShaderDependencyResolver _resolver = new ShaderDependencyResolver();

        public bool Validate(
            FluxParticleModule module,
            IFluxParticleGpuModuleDefinition definition,
            FluxParticleValidationResult validation)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (validation == null) throw new ArgumentNullException(nameof(validation));

            ShaderDependencyGraph graph;

            try
            {
                graph = _resolver.Resolve(new[] { definition.HlslPath });
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is InvalidOperationException ||
                exception is ArgumentException)
            {
                AddError(
                    validation,
                    "G006",
                    $"GPU Module HLSL dependency is invalid: {definition.HlslPath}. {exception.Message}",
                    module);

                return false;
            }

            var signature = @"\bvoid\s+" +
                            Regex.Escape(definition.EntryPoint) +
                            @"\s*\(\s*inout\s+FluxFXGpuContext\s+[A-Za-z_][A-Za-z0-9_]*\s*\)";

            foreach (var source in graph.Sources.Values)
            {
                if (Regex.IsMatch(source.Source, signature)) return true;
            }

            AddError(
                validation,
                "G007",
                $"GPU Module entry point was not found: {definition.EntryPoint} ({definition.HlslPath}).",
                module);

            return false;
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