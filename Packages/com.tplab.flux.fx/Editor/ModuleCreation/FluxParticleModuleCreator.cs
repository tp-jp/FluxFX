using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TpLab.Flux.FX.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.ModuleCreation
{
    /// <summary>
    /// Assets内の新規フォルダーへカスタムModuleのソースを一度だけ生成します。
    /// </summary>
    public static class FluxParticleModuleCreator
    {
        static readonly HashSet<string> Keywords = new HashSet<string>((
            "abstract as base bool break byte case catch char checked class const continue decimal default delegate do " +
            "double else enum event explicit extern false finally fixed float for foreach goto if implicit in int " +
            "interface internal is lock long namespace new null object operator out override params private protected " +
            "public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw " +
            "true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while " +
            "__arglist __makeref __reftype __refvalue global var dynamic record file required").Split(' '));

        /// <summary>
        /// 入力と現在のProjectを検査します。ファイルは変更しません。
        /// </summary>
        /// <param name="name">Module接尾辞を含めないクラス名の基部</param>
        /// <param name="namespaceName">生成する型の親namespace</param>
        /// <param name="parentFolder">Assets内の既存フォルダー</param>
        /// <returns>検出した問題。空なら生成可能</returns>
        public static IReadOnlyList<string> Validate(string name, string namespaceName, string parentFolder)
        {
            var errors = new List<string>();
            if (!IsIdentifier(name) || name.Length > 64)
            {
                errors.Add("Module name must be an ASCII C# identifier of at most 64 characters, not a keyword.");
            }
            if (string.IsNullOrEmpty(namespaceName) || namespaceName.Length > 128 || !namespaceName.Split('.').All(IsIdentifier))
            {
                errors.Add("Namespace must contain ASCII C# identifiers separated by dots (at most 128 characters).");
            }
            if (name != null && Regex.IsMatch(name, @"\A(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])\z", RegexOptions.IgnoreCase))
            {
                errors.Add("Module name cannot be a reserved device name.");
            }
            if (typeof(TpLab.Flux.FX.Udon.FluxParticleModules).GetMethods().Any(method => method.Name == name))
            {
                errors.Add("Module name conflicts with an existing Runtime API method.");
            }
            if (errors.Count != 0) return errors;
            try
            {
                var parent = ValidateParent(parentFolder);
                var destination = Path.Combine(parent, name);
                if (Directory.Exists(destination) || File.Exists(destination) || File.Exists(destination + ".meta"))
                {
                    errors.Add("The destination already exists. Choose a new module name or parent folder; existing files will not be overwritten.");
                }
                var root = namespaceName + "." + name;
                var id = "custom." + root.ToLowerInvariant();
                var prefix = "custom.h" + FluxParticleModuleTemplate.IdentityHash(id);
                var ids = new[] { id, prefix + ".strength", prefix + ".direction", prefix + ".enabled", prefix + ".accelerate" };
                if (Path.Combine(destination, "Scripts", root + ".Scripts.asmdef.meta").Length >= 240)
                {
                    errors.Add("Generated paths must be shorter than 240 characters. Shorten the namespace, name or parent path.");
                }
                var assemblies = new[] { root + ".Scripts", root + ".Editor", root + ".Udon" };
                if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assemblies.Contains(assembly.GetName().Name)))
                {
                    errors.Add("A generated assembly name is already loaded: " + root);
                }
                var typeNames = new[] { root + ".Scripts." + name + "Module", root + ".Editor." + name + "Definition",
                    root + ".Editor." + name + "Contract", root + ".Editor." + name + "LiveInspectorProvider",
                    root + ".Udon." + name + "Handle", root + ".Udon." + name + "HandleExtensions", root + ".Udon." + name + "Parameter" };
                if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly => typeNames.Any(type => assembly.GetType(type, false) != null)))
                {
                    errors.Add("A generated type already exists: " + root);
                }
                if (new FluxParticleModuleRegistry().GetAll().Any(definition => string.Equals(definition.ModuleId, id, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add("Module ID already registered: " + id);
                }
                // Also cover sources generated earlier in the same batch, before assembly reload.
                foreach (var path in AssetDatabase.GetAllAssetPaths().Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!File.Exists(path)) continue;
                    var source = File.ReadAllText(path);
                    if (path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
                    {
                        if (assemblies.Contains(JsonUtility.FromJson<AssemblyName>(source)?.name))
                        {
                            errors.Add("Assembly name already declared in " + path);
                        }
                    }
                    else if ((source.IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0 || source.Contains(prefix) || source.Contains(root)) &&
                        (ids.Any(candidate => Regex.IsMatch(source,
                        @"(?:=>|(?<![=!<>])=(?!=)|new\s+FluxParticleModuleContractBuilder\s*\(|FluxParticleParameter\.\w+\s*\(|\.Operation\s*\()\s*""" + Regex.Escape(candidate) + "\"", RegexOptions.IgnoreCase)) ||
                        Regex.IsMatch(source, @"\bnamespace\s+" + Regex.Escape(root) + @"\.(Scripts|Editor|Udon)\b")))
                    {
                        errors.Add("Module/Parameter/Operation ID or generated namespace already declared in " + path);
                    }
                }
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException ||
                exception is UnauthorizedAccessException || exception is InvalidOperationException)
            {
                errors.Add(exception.Message);
            }
            return errors;
        }

        /// <summary>
        /// 入力を再検査してソースを生成し、Unityへインポートします。既存ファイルは変更しません。
        /// </summary>
        /// <param name="name">Moduleクラス名の基部</param>
        /// <param name="namespaceName">生成する型の親namespace</param>
        /// <param name="parentFolder">Assets内の既存フォルダー</param>
        /// <param name="runtimeApi">Udon用Handleとアセンブリを生成するか</param>
        /// <returns>生成したフォルダーのAssetパス</returns>
        public static string Create(string name, string namespaceName, string parentFolder, bool runtimeApi = false)
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Wait for compilation to finish and leave Play Mode before creating a module.");
            }
            var errors = Validate(name, namespaceName, parentFolder);
            if (errors.Count != 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }
            var files = FluxParticleModuleTemplate.Create(name, namespaceName, runtimeApi);
            var destination = parentFolder + "/" + name;
            var absolute = Path.Combine(ValidateParent(parentFolder), name);
            var written = new List<string>();
            var directories = new List<string>();
            AssetDatabase.StartAssetEditing();
            try
            {
                // CreateNew protects files even if a competing write happens after validation.
                if (Directory.Exists(absolute) || File.Exists(absolute) || File.Exists(absolute + ".meta"))
                {
                    throw new IOException("Destination appeared during generation: " + destination);
                }
                Directory.CreateDirectory(absolute);
                directories.Add(absolute);
                foreach (var file in files)
                {
                    var target = Path.Combine(absolute, file.Key);
                    var directory = Path.GetDirectoryName(target);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                        directories.Add(directory);
                    }
                    using (var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write))
                    {
                        written.Add(target);
                        using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(file.Value);
                    }
                }
            }
            catch
            {
                // Roll back only files written by this call; never recursively delete an existing tree.
                foreach (var path in written.AsEnumerable().Reverse()) File.Delete(path);
                foreach (var path in directories.AsEnumerable().Reverse())
                {
                    if (!Directory.EnumerateFileSystemEntries(path).Any())
                    {
                        Directory.Delete(path);
                    }
                }
                throw;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            return destination;
        }

        static bool IsIdentifier(string value) => !string.IsNullOrEmpty(value) && !Keywords.Contains(value) &&
            Regex.IsMatch(value, @"\A[A-Za-z_][A-Za-z0-9_]*\z");

        static string ValidateParent(string path)
        {
            if (string.IsNullOrEmpty(path) || path != path.Trim() || path.Contains('\\') ||
                !(path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Select an existing folder inside Assets.");
            }
            var segments = path.Split('/');
            if (segments.Any(segment => string.IsNullOrEmpty(segment) || segment == "." || segment == ".." ||
                string.Equals(segment, "Editor", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(segment, "StreamingAssets", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(segment, "Editor Default Resources", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("The destination cannot contain traversal segments, Editor, Editor Default Resources or StreamingAssets.");
            }
            var assets = Path.GetFullPath(Application.dataPath);
            var absolute = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assets), path));
            if (absolute != assets && !absolute.StartsWith(assets + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Destination must remain inside this project's Assets.");
            }
            var current = new DirectoryInfo(absolute);
            while (current != null && current.FullName.Length >= assets.Length)
            {
                if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new ArgumentException("Symbolic links and junctions are not supported as generation destinations.");
                }
                current = current.Parent;
            }
            if (!AssetDatabase.IsValidFolder(path) || !Directory.Exists(absolute))
            {
                throw new ArgumentException("Select an existing folder inside Assets.");
            }
            return absolute;
        }

        [Serializable]
        sealed class AssemblyName
        {
            public string name = "";
        }
    }
}
