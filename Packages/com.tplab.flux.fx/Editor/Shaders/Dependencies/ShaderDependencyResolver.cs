using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TpLab.Flux.FX.Editor.Shaders.Dependencies
{
    public sealed class ShaderDependencyResolver
    {
        static readonly Regex IncludePattern = new Regex(
            @"^[ \t]*#[ \t]*include\b[^\r\n]*",
            RegexOptions.Compiled | RegexOptions.Multiline);

        static readonly Regex IncludePathPattern = new Regex(
            @"^[ \t]*#[ \t]*include[ \t]+""(?<path>[^""]+)""[ \t]*(?://[^\r\n]*)?$",
            RegexOptions.Compiled);

        static readonly HashSet<string> ExternalIncludes = new HashSet<string>(StringComparer.Ordinal)
        {
            "UnityCG.cginc",
            "UnityShaderVariables.cginc",
            "UnityInstancing.cginc",
            "AutoLight.cginc",
            "Lighting.cginc",
            "UnityPBSLighting.cginc",
            "HLSLSupport.cginc"
        };

        readonly string _projectRoot;
        readonly Dictionary<string, ShaderSourceDependency> _sources = new Dictionary<string, ShaderSourceDependency>(StringComparer.Ordinal);
        readonly HashSet<string> _visiting = new HashSet<string>(StringComparer.Ordinal);
        readonly List<string> _stack = new List<string>();

        public ShaderDependencyResolver(string projectRoot = null)
        {
            _projectRoot = Path.GetFullPath(projectRoot ?? Directory.GetCurrentDirectory());
        }

        public ShaderDependencyGraph Resolve(IEnumerable<string> paths)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));

            _sources.Clear();
            _visiting.Clear();
            _stack.Clear();

            var roots = new List<string>();

            foreach (var path in paths)
            {
                var normalizedPath = NormalizePath(path);

                if (!roots.Contains(normalizedPath))
                {
                    roots.Add(normalizedPath);
                }

                Visit(normalizedPath);
            }

            return new ShaderDependencyGraph(roots, _sources);
        }

        void Visit(string path)
        {
            if (_visiting.Contains(path))
            {
                var chain = string.Join(" -> ", _stack.Concat(new[] { path }));
                throw new InvalidOperationException($"Circular Shader include dependency: {chain}");
            }

            if (_sources.ContainsKey(path)) return;

            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Shader dependency was not found: {path}", fullPath);
            }

            _visiting.Add(path);
            _stack.Add(path);

            try
            {
                var source = Normalize(File.ReadAllText(fullPath));
                var includes = new List<ShaderIncludeReference>();

                foreach (Match match in IncludePattern.Matches(source))
                {
                    var includeMatch = IncludePathPattern.Match(match.Value);

                    if (!includeMatch.Success)
                    {
                        throw new InvalidOperationException($"Unsupported Shader include directive in {path}: {match.Value.Trim()}");
                    }

                    var pathGroup = includeMatch.Groups["path"];
                    var includePath = pathGroup.Value.Replace('\\', '/');
                    var external = ExternalIncludes.Contains(includePath);

                    if (!external)
                    {
                        includePath = ResolveIncludePath(path, includePath);
                    }

                    includes.Add(new ShaderIncludeReference(
                        includePath,
                        external,
                        match.Index + pathGroup.Index,
                        pathGroup.Length));

                    if (!external)
                    {
                        Visit(includePath);
                    }
                }

                _sources.Add(path, new ShaderSourceDependency(path, source, includes));
            }
            finally
            {
                _stack.RemoveAt(_stack.Count - 1);
                _visiting.Remove(path);
            }
        }

        string ResolveIncludePath(string sourcePath, string includePath)
        {
            if (includePath.StartsWith("Assets/", StringComparison.Ordinal) ||
                includePath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                return NormalizePath(includePath);
            }

            var directory = Path.GetDirectoryName(sourcePath.Replace('/', Path.DirectorySeparatorChar));
            var combined = Path.Combine(directory ?? "", includePath.Replace('/', Path.DirectorySeparatorChar));

            return NormalizePath(combined);
        }

        string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Shader dependency path is empty.", nameof(path));

            if (Path.IsPathRooted(path))
                throw new InvalidOperationException($"Absolute Shader dependency paths are not supported: {path}");

            var fullPath = GetFullPath(path);
            var relativePath = fullPath.Substring(_projectRoot.Length).TrimStart(Path.DirectorySeparatorChar).Replace('\\', '/');

            if (!relativePath.StartsWith("Assets/", StringComparison.Ordinal) &&
                !relativePath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Shader dependency must be inside Assets or Packages: {path}");
            }

            return relativePath;
        }

        string GetFullPath(string path)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_projectRoot, path.Replace('/', Path.DirectorySeparatorChar)));
            var rootPrefix = _projectRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (!fullPath.StartsWith(rootPrefix, comparison))
            {
                throw new InvalidOperationException($"Shader dependency is outside the project: {path}");
            }

            return fullPath;
        }

        static string Normalize(string source)
        {
            return source.Replace("\r\n", "\n").Replace('\r', '\n');
        }
    }
}