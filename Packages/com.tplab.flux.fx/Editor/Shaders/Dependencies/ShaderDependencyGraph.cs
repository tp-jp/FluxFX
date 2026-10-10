using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TpLab.Flux.FX.Editor.Shaders.Dependencies
{
    public sealed class ShaderIncludeReference
    {
        public string Path { get; }
        public bool IsExternal { get; }
        public int StartIndex { get; }
        public int Length { get; }

        public ShaderIncludeReference(string path, bool isExternal, int startIndex, int length)
        {
            Path = path;
            IsExternal = isExternal;
            StartIndex = startIndex;
            Length = length;
        }
    }

    public sealed class ShaderSourceDependency
    {
        public string Path { get; }
        public string Source { get; }
        public IReadOnlyList<ShaderIncludeReference> Includes { get; }

        public ShaderSourceDependency(string path, string source, IList<ShaderIncludeReference> includes)
        {
            Path = path;
            Source = source;
            Includes = new ReadOnlyCollection<ShaderIncludeReference>(new List<ShaderIncludeReference>(includes));
        }
    }

    public sealed class ShaderDependencyGraph
    {
        public IReadOnlyList<string> Roots { get; }
        public IReadOnlyDictionary<string, ShaderSourceDependency> Sources { get; }

        public ShaderDependencyGraph(IList<string> roots, IDictionary<string, ShaderSourceDependency> sources)
        {
            Roots = new ReadOnlyCollection<string>(new List<string>(roots));
            Sources = new ReadOnlyDictionary<string, ShaderSourceDependency>(
                new Dictionary<string, ShaderSourceDependency>(sources, StringComparer.Ordinal));
        }

        public ShaderSourceDependency GetSource(string path)
        {
            if (Sources.TryGetValue(path, out var source)) return source;

            throw new InvalidOperationException($"Shader dependency was not found: {path}");
        }
    }
}