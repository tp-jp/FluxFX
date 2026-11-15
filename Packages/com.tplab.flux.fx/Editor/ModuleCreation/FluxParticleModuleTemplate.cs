using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TpLab.Flux.FX.Editor.Compilation.ShaderGeneration;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.ModuleCreation
{
    /// <summary>
    /// 編集可能なソース一式を組み立てます。生成後の再生成は必要ありません。
    /// </summary>
    internal static class FluxParticleModuleTemplate
    {
        internal const string TemplateRoot = "Packages/com.tplab.flux.fx/Editor/ModuleCreation/Templates/";

        internal static Dictionary<string, string> Create(string name, string ns, bool runtimeApi)
        {
            var root = ns + "." + name;
            var id = "custom." + root.ToLowerInvariant();
            var hash = IdentityHash(id);
            var prefix = "custom.h" + hash;
            var shaderGuid = Guid.NewGuid().ToString("N");
            var udonGuid = Guid.NewGuid().ToString("N");
            var tokens = new Dictionary<string, string>
            {
                ["NAME"] = name, ["ROOT"] = root, ["ID"] = id, ["PREFIX"] = prefix,
                ["HLSL_GUID"] = shaderGuid, ["UDON_GUID"] = udonGuid,
                ["CONTEXT"] = FluxParticleHlslNames.Context(prefix + ".accelerate"),
                ["STRENGTH"] = FluxParticleHlslNames.Parameter(prefix + ".strength"),
                ["DIRECTION"] = FluxParticleHlslNames.Parameter(prefix + ".direction"),
                ["ENTRY"] = "FluxFX_Accelerate_" + hash,
                ["UDON_USING"] = runtimeApi ? "using " + root + ".Udon;\n" : "",
                ["PARAMETERS"] = runtimeApi
                    ? ".Parameter(" + name + "Parameter.Strength, Strength)\n                .Parameter(" + name + "Parameter.Direction, Direction)"
                    : ".Parameter(Strength)\n                .Parameter(Direction)"
            };
            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            void Add(string path, string template)
            {
                var content = File.ReadAllText(TemplateRoot + template + ".txt");
                foreach (var token in tokens) content = content.Replace("{{" + token.Key + "}}", token.Value);
                if (content.Contains("{{"))
                {
                    throw new InvalidOperationException("Unresolved template token: " + template);
                }
                files.Add(path, content);
            }
            Add("Scripts/" + name + "Module.cs", "Module");
            Add("Editor/" + name + "Contract.cs", "Contract");
            Add("Editor/" + name + "Definition.cs", "Definition");
            Add("Editor/" + name + "LiveInspectorProvider.cs", "LiveInspectorProvider");
            Add("Editor/" + name + ".hlsl", "Acceleration");
            files.Add("Editor/" + name + ".hlsl.meta", Meta(shaderGuid));
            files.Add("Scripts/" + root + ".Scripts.asmdef", Assembly(root + ".Scripts", false, "TpLab.Flux.FX.Scripts"));
            var editorReferences = new List<string> { root + ".Scripts", "TpLab.Flux.FX.Scripts", "TpLab.Flux.FX.Editor" };
            if (runtimeApi)
            {
                editorReferences.Add(root + ".Udon");
                Add("Udon/" + name + "Handle.cs", "Handle");
                Add("Udon/" + name + "Parameter.cs", "Parameter");
                Add("Udon/" + root + ".Udon.asset", "UdonAssembly");
                files.Add("Udon/" + root + ".Udon.asmdef", Assembly(root + ".Udon", false,
                    "UdonSharp.Runtime", "VRC.Udon", "TpLab.Flux.FX.Udon"));
                files.Add("Udon/" + root + ".Udon.asmdef.meta", Meta(udonGuid));
            }
            files.Add("Editor/" + root + ".Editor.asmdef", Assembly(root + ".Editor", true, editorReferences.ToArray()));
            return files;
        }

        internal static string IdentityHash(string moduleId)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(moduleId))).Replace("-", "").Substring(0, 16).ToLowerInvariant();
            }
        }

        static string Meta(string guid) => "fileFormatVersion: 2\nguid: " + guid + "\n";

        static string Assembly(string name, bool editor, params string[] references) => JsonUtility.ToJson(new AssemblyData
        {
            name = name, rootNamespace = name, references = references.Distinct().ToArray(),
            includePlatforms = editor ? new[] { "Editor" } : Array.Empty<string>()
        }, true) + "\n";

        [Serializable]
        sealed class AssemblyData
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public string[] includePlatforms;
            public bool autoReferenced = true;
        }
    }
}
