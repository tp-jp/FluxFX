using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TpLab.Flux.FX.Editor.Shaders.Dependencies;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class FluxParticleGpuSymbolValidator
    {
        static readonly Regex FunctionPattern = new Regex(
            @"\b(?:void|float|float2|float3|float4|half|half2|half3|half4|fixed|fixed2|fixed3|fixed4|int|int2|int3|int4|uint|uint2|uint3|uint4|bool|bool2|bool3|bool4)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\([^;{}]*\)\s*(?::\s*[A-Za-z_][A-Za-z0-9_]*)?\s*\{",
            RegexOptions.Compiled);

        static readonly Regex CommentPattern = new Regex(
            @"/\*[\s\S]*?\*/|//[^\r\n]*",
            RegexOptions.Compiled);

        public void Validate(
            FluxParticleGpuStageDescriptor descriptor,
            IReadOnlyList<FluxParticleGpuModuleCompilation> modules,
            FluxParticleValidationResult validation)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            if (modules == null) throw new ArgumentNullException(nameof(modules));
            if (validation == null) throw new ArgumentNullException(nameof(validation));

            if (modules.Count == 0) return;

            var paths = new List<string> { descriptor.ShaderTemplatePath };

            foreach (var module in modules)
            {
                paths.Add(module.HlslPath);
            }

            var graph = new ShaderDependencyResolver().Resolve(paths);
            var symbols = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var source in graph.Sources.Values)
            {
                var content = CommentPattern.Replace(source.Source, "");
                var sourceNames = new HashSet<string>(StringComparer.Ordinal);

                foreach (Match match in FunctionPattern.Matches(content))
                {
                    var name = match.Groups["name"].Value;

                    if (!sourceNames.Add(name)) continue;

                    if (!symbols.TryGetValue(name, out var existingPath))
                    {
                        symbols.Add(name, source.Path);
                        continue;
                    }

                    if (existingPath == source.Path) continue;

                    validation.Add(
                        "G008",
                        $"Duplicate GPU function '{name}' is defined in '{existingPath}' and '{source.Path}'.",
                        FindModuleType(modules, graph, source.Path, existingPath),
                        descriptor.Stage,
                        FluxParticleValidationSeverity.Error);
                }
            }
        }

        static Type FindModuleType(
            IReadOnlyList<FluxParticleGpuModuleCompilation> modules,
            ShaderDependencyGraph graph,
            string firstPath,
            string secondPath)
        {
            foreach (var module in modules)
            {
                var moduleGraph = new ShaderDependencyResolver().Resolve(new[] { module.HlslPath });

                if (moduleGraph.Sources.ContainsKey(firstPath) ||
                    moduleGraph.Sources.ContainsKey(secondPath))
                {
                    return module.ModuleType;
                }
            }

            return null;
        }
    }
}