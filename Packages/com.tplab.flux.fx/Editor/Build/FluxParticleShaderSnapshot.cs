using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 生成Shaderと、内容で識別される変更不可の依存HLSLです。
    /// 子includeの固定先を親へ埋め込むため、子の変更も親の識別子へ反映します。
    /// </summary>
    public sealed class FluxParticleShaderSnapshot
    {
        static readonly Regex _includes = new Regex(
            @"^[ \t]*#[ \t]*include\w*\b[^\r\n]*", RegexOptions.Multiline);
        static readonly Regex _includePath = new Regex(
            "^[ \\t]*#[ \\t]*include(?:_with_pragmas)?[ \\t]+(?:\"(?<path>[^\"<>]+)\"|<(?<path>[^\"<>]+)>)[ \\t]*$");
        static readonly Regex _continuedDirectives = new Regex(@"^[ \t]*#(?:[^\n]*\\\n)+[^\n]*", RegexOptions.Multiline);
        static readonly HashSet<string> _externalIncludes = new HashSet<string>(StringComparer.Ordinal)
        {
            "UnityCG.cginc", "UnityShaderVariables.cginc", "UnityInstancing.cginc",
            "AutoLight.cginc", "Lighting.cginc", "UnityPBSLighting.cginc", "HLSLSupport.cginc"
        };

        /// <summary>
        /// 固定includeを参照する生成Shader本文
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// 保存先Assetパスをキーとする固定includeの本文
        /// </summary>
        public IReadOnlyDictionary<string, string> IncludeSources { get; }

        /// <summary>
        /// 依存元Assetパスと固定先の対応
        /// </summary>
        public IReadOnlyDictionary<string, string> OriginalPaths { get; }

        /// <summary>
        /// 固定includeごとの直接依存先
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> IncludeDependencies { get; }

        FluxParticleShaderSnapshot(string source, IDictionary<string, string> includes,
            IDictionary<string, string> paths, IDictionary<string, IReadOnlyList<string>> dependencies)
        {
            Source = source;
            IncludeSources = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(includes));
            OriginalPaths = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(paths));
            IncludeDependencies = new ReadOnlyDictionary<string, IReadOnlyList<string>>(dependencies);
        }

        /// <summary>
        /// Assets・Packages内の依存を再帰的に固定します。Unity組み込みincludeは維持します。
        /// 循環依存・マクロによるincludeは、固定化を保証できないため拒否します。
        /// </summary>
        /// <param name="source">生成Shader本文</param>
        /// <param name="directory">固定includeの保存先Assetフォルダ</param>
        /// <param name="readSource">正規化したAssetパスから本文を読み取る関数</param>
        /// <returns>ファイルへの書き込みを伴わない固定化結果</returns>
        public static FluxParticleShaderSnapshot Create(string source, string directory, Func<string, string> readSource)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (readSource == null) throw new ArgumentNullException(nameof(readSource));
            directory = NormalizePath(directory);
            if (!directory.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Shader snapshots must be stored inside Assets.", nameof(directory));
            }
            var paths = new Dictionary<string, string>(StringComparer.Ordinal);
            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            var dependencies = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            var stack = new List<string>();

            string Freeze(string text, string sourcePath, out IReadOnlyList<string> children)
            {
                text = text.Replace("\r\n", "\n").Replace('\r', '\n');
                var visible = WithoutComments(text);
                foreach (Match directive in _continuedDirectives.Matches(visible))
                {
                    if (Regex.IsMatch(directive.Value.Replace("\\\n", ""), @"^[ \t]*#[ \t]*include"))
                    {
                        throw new InvalidOperationException($"Unsupported continued Shader include in {sourcePath}: {directive.Value}");
                    }
                }
                var replacements = new List<(int start, int length, string path)>();
                foreach (Match directive in _includes.Matches(visible))
                {
                    var match = _includePath.Match(directive.Value);
                    if (!match.Success)
                    {
                        throw new InvalidOperationException($"Unsupported Shader include directive in {sourcePath}: {directive.Value.Trim()}");
                    }
                    var group = match.Groups["path"];
                    var path = group.Value.Replace('\\', '/');
                    if (_externalIncludes.Contains(path)) continue;
                    path = NormalizePath(path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("Packages/", StringComparison.Ordinal)
                        ? path : Path.GetDirectoryName(sourcePath).Replace('\\', '/') + "/" + path);
                    if (stack.Contains(path))
                    {
                        throw new InvalidOperationException("Circular Shader include dependency: " + string.Join(" -> ", stack.Concat(new[] { path })));
                    }
                    if (!paths.TryGetValue(path, out var frozenPath))
                    {
                        stack.Add(path);
                        string dependencySource;
                        try
                        {
                            dependencySource = readSource(path);
                        }
                        catch (Exception error)
                        {
                            throw new FileNotFoundException($"Cannot read Shader dependency '{path}', included by '{sourcePath}': {error.Message}", path, error);
                        }
                        var frozen = Freeze(dependencySource, path, out var childDependencies);
                        stack.RemoveAt(stack.Count - 1);
                        using (var sha = SHA256.Create())
                        {
                            var hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(frozen))).Replace("-", "").ToLowerInvariant();
                            frozenPath = directory + "/Include-" + hash + ".hlsl";
                        }
                        paths.Add(path, frozenPath);
                        if (files.TryGetValue(frozenPath, out var existing) && existing != frozen)
                        {
                            throw new InvalidOperationException($"Shader include content collision: {path} -> {frozenPath}");
                        }
                        files[frozenPath] = frozen;
                        dependencies[frozenPath] = childDependencies;
                    }
                    replacements.Add((directive.Index + group.Index, group.Length, frozenPath));
                }
                foreach (var replacement in replacements.OrderByDescending(value => value.start))
                {
                    text = text.Substring(0, replacement.start) + replacement.path + text.Substring(replacement.start + replacement.length);
                }
                children = Array.AsReadOnly(replacements.Select(value => value.path).Distinct().ToArray());
                return text;
            }

            var result = Freeze(source, "Assets/FluxFXGenerated.shader", out _);
            return new FluxParticleShaderSnapshot(result, files, paths, dependencies);
        }

        static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
            {
                throw new ArgumentException("Shader dependency requires a project-relative path: " + path);
            }
            var parts = new List<string>();
            foreach (var part in path.Replace('\\', '/').Split('/'))
            {
                if (part == "" || part == ".") continue;
                if (part == "..")
                {
                    if (parts.Count <= 1) throw new InvalidOperationException("Shader dependency escapes its asset root: " + path);
                    parts.RemoveAt(parts.Count - 1);
                }
                else
                {
                    parts.Add(part);
                }
            }
            if (parts.Count < 2 || (parts[0] != "Assets" && parts[0] != "Packages"))
            {
                throw new InvalidOperationException("Shader dependency must be inside Assets or Packages: " + path);
            }
            return string.Join("/", parts);
        }

        // コメントを同じ長さの空白へ置換し、置換位置と改行を保持します。
        static string WithoutComments(string source)
        {
            var result = source.ToCharArray();
            var quoted = false;
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] == '"' && (i == 0 || source[i - 1] != '\\')) quoted = !quoted;
                if (quoted || source[i] != '/' || i + 1 >= source.Length) continue;
                var block = source[i + 1] == '*';
                if (!block && source[i + 1] != '/') continue;
                result[i++] = ' ';
                result[i] = ' ';
                for (i++; i < source.Length; i++)
                {
                    if (!block && (source[i] == '\n' || source[i] == '\r')) break;
                    if (block && source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/')
                    {
                        result[i++] = ' ';
                        result[i] = ' ';
                        break;
                    }
                    if (source[i] != '\n' && source[i] != '\r') result[i] = ' ';
                }
            }
            return new string(result);
        }
    }
}
