using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.PackageManager;
using System.Text;
using System.Text.RegularExpressions;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// State LayoutとOperation宣言からShaderとBindingを生成します。
    /// Operationは専用Contextを更新し、Derived Providerは宣言順の入力・Parameterを引数として値を返します。
    /// </summary>
    public sealed class FluxParticleProgramShaderGenerator
    {
        /// <summary>
        /// コンパイル済みの定義と実行計画からShaderを生成します。
        /// </summary>
        /// <param name="program">検証済みのコンパイル結果</param>
        /// <param name="shaderName">生成Shader名の接頭辞</param>
        /// <param name="lifecycle">生存判定の契約。nullなら全非Spawn粒子をUpdateします</param>
        /// <returns>Shaderソース、Page入力、Parameter契約を含む生成結果</returns>
        public FluxParticleShaderProgram Generate(FluxParticleCompiledProgram program, string shaderName,
            FluxParticleShaderLifecycle lifecycle = null)
        {
            if (program == null) throw new ArgumentNullException(nameof(program));
            if (string.IsNullOrWhiteSpace(shaderName) || shaderName.Any(c => c == '"' || c == '\\' || char.IsControl(c)))
            {
                throw new ArgumentException("A safe Shader name is required.", nameof(shaderName));
            }
            var emitter = new Emitter(program, lifecycle);
            emitter.Validate();
            var passes = program.ExecutionPlan.Passes.Select(pass => emitter.GeneratePass(pass, shaderName)).ToArray();
            var clear = program.StateLayout.Pages.Select(page => emitter.GenerateClear(page, shaderName)).ToArray();
            return new FluxParticleShaderProgram(program, passes, clear, emitter.GenerateRender(),
                program.Providers.Values.Where(p => (p.Kind == FluxParticleProviderKind.Uniform || p.Kind == FluxParticleProviderKind.SpawnInput)).OrderBy(p => p.ValueId, StringComparer.Ordinal), emitter.GenerateAvailability(shaderName));
        }

        sealed class Emitter
        {
            readonly FluxParticleCompiledProgram program;
            readonly FluxParticleShaderLifecycle lifecycle;
            readonly Dictionary<string, FluxParticleOperationDefinition> originals;
            Dictionary<string, FluxParticlePageRead> pageReads;
            HashSet<string> localOperations;
            HashSet<string> usedProviders;
            StringBuilder body;

            internal Emitter(FluxParticleCompiledProgram program, FluxParticleShaderLifecycle lifecycle)
            {
                this.program = program;
                this.lifecycle = lifecycle;
                originals = program.Operations.ToDictionary(op => op.Id, StringComparer.Ordinal);
            }

            internal void Validate()
            {
                var collision = program.Operations.GroupBy(op => FluxParticleHlslNames.Context(op.Id), StringComparer.Ordinal)
                    .FirstOrDefault(group => group.Count() > 1);
                if (collision != null)
                {
                    throw new InvalidOperationException($"Context type name collision: {collision.Key} ({string.Join(", ", collision.Select(op => op.Id))})");
                }
                var valueCollision = program.Values.Keys.GroupBy(FluxParticleHlslNames.Value, StringComparer.Ordinal)
                    .FirstOrDefault(group => group.Count() > 1);
                if (valueCollision != null)
                {
                    throw new InvalidOperationException($"Value field name collision: {valueCollision.Key} ({string.Join(", ", valueCollision)})");
                }
                var parameterCollision = program.Parameters.Keys.GroupBy(FluxParticleHlslNames.Parameter, StringComparer.Ordinal)
                    .FirstOrDefault(group => group.Count() > 1);
                if (parameterCollision != null)
                {
                    throw new InvalidOperationException($"Parameter field name collision: {parameterCollision.Key} ({string.Join(", ", parameterCollision)})");
                }
                foreach (var op in program.Operations)
                {
                    ValidateImplementation(op.HlslPath, op.EntryPoint);
                    if (op.Reads.GroupBy(r => r.ValueId).Any(g => g.Count() > 1))
                    {
                        throw new InvalidOperationException($"Multiple read versions in one Context are not supported: {op.Id}");
                    }
                    foreach (var write in op.Writes.Where(w => w.Kind == FluxParticleWriteKind.Modify))
                    {
                        if (!op.Reads.Any(r => r.ValueId == write.ValueId))
                        {
                            throw new InvalidOperationException($"Modify requires an explicit read: {op.Id}/{write.ValueId}");
                        }
                    }
                }
                foreach (var provider in program.Providers.Values.Where(p => p.Kind == FluxParticleProviderKind.Derived))
                {
                    ValidateImplementation(provider.HlslPath, provider.EntryPoint);
                }
                // Updated Page contains only the final writer's version, never an intermediate writer's result.
                foreach (var pass in program.ExecutionPlan.Passes)
                {
                    foreach (var read in pass.UpdateOperations.SelectMany(op => op.Reads).Where(r => r.Version == FluxParticleReadVersion.AfterOperation))
                    {
                        if (!program.StateLayout.TryGetBinding(read.ValueId, out var binding) || binding.PageId == pass.OutputPageId) continue;
                        var writer = program.Operations.Last(op => op.Phase == FluxParticlePhase.Update && op.Writes.Any(w => w.ValueId == read.ValueId));
                        if (writer.Id != read.ProducerOperationId)
                        {
                            throw new InvalidOperationException($"Intermediate cross-Page version is not materialized: {read.ProducerOperationId}/{read.ValueId}");
                        }
                    }
                }
                if (lifecycle == null) return;
                if (!program.Values.TryGetValue(lifecycle.AgeValueId, out var age) || age.Type != FluxParticleValueType.Float ||
                    !program.StateLayout.TryGetBinding(age.Id, out _) ||
                    !program.Values.TryGetValue(lifecycle.LifetimeValueId, out var lifetime) || lifetime.Type != FluxParticleValueType.Float)
                {
                    throw new InvalidOperationException("Lifecycle requires persistent Float Age and Float Lifetime.");
                }
                ValidateLifecycleProvider(lifecycle.LifetimeValueId, new HashSet<string>(StringComparer.Ordinal));
            }

            void ValidateLifecycleProvider(string id, HashSet<string> visited)
            {
                if (!visited.Add(id)) return;
                if (!program.Providers.TryGetValue(id, out var provider))
                {
                    if (!program.StateLayout.TryGetBinding(id, out _)) throw new InvalidOperationException($"Unbound Lifecycle Value: {id}");
                    return;
                }
                if (provider.Kind != FluxParticleProviderKind.Derived ||
                    provider.Parameters.Any(p => program.Parameters[p].Policy != FluxParticleParameterPolicy.Static))
                {
                    throw new InvalidOperationException($"Recomputed Lifecycle requires Derived Providers with Static Parameters: {id}");
                }
                foreach (var input in provider.Inputs) ValidateLifecycleProvider(input, visited);
            }

            static void ValidateImplementation(string path, string entry)
            {
                if (string.IsNullOrWhiteSpace(path) || path.Any(c => c == '"' || char.IsControl(c)) ||
                    !Regex.IsMatch(entry, @"\A[A-Za-z_][A-Za-z0-9_]*\z"))
                {
                    throw new InvalidOperationException($"Unsafe HLSL implementation: {path}/{entry}");
                }
            }

            internal FluxParticleGeneratedPass GeneratePass(FluxParticleGpuPass pass, string name)
            {
                Start();
                foreach (var read in pass.Reads) AddPageRead(read.PageId, read.Version);
                var outputPage = program.StateLayout.Pages.Single(p => p.Id == pass.OutputPageId);
                var previousPage = Page(pass.OutputPageId, FluxParticlePageVersion.StepPrevious);
                var lifecycleCode = lifecycle == null ? "    bool alive = true;\n" :
                    ExpandTemplate("StepLifecycle.hlsl.template", new Dictionary<string, string>
                    {
                        ["Age"] = State(lifecycle.AgeValueId, FluxParticlePageVersion.StepPrevious),
                        ["Lifetime"] = SuppliedOrState(lifecycle.LifetimeValueId)
                    });
                EmitOperations(pass.SpawnOperations, false);
                EmitPageWrites(outputPage, pass.SpawnOperations);
                var spawnCode = body.ToString();
                body.Clear();
                EmitOperations(pass.UpdateOperations, false);
                EmitPageWrites(outputPage, pass.UpdateOperations);
                var updateCode = body.ToString();
                var expiration = "";
                if (lifecycle != null && outputPage.Bindings.Any(b => b.ValueId == lifecycle.AgeValueId))
                {
                    var binding = outputPage.Bindings.Single(b => b.ValueId == lifecycle.AgeValueId);
                    expiration = ExpandTemplate("Expiration.hlsl.template", new Dictionary<string, string> { ["Channel"] = Swizzle(binding) });
                }
                var step = ExpandTemplate("Step.hlsl.template", new Dictionary<string, string>
                {
                    ["PreviousPage"] = previousPage,
                    ["Lifecycle"] = lifecycleCode,
                    ["Spawn"] = spawnCode,
                    ["Update"] = updateCode,
                    ["Expiration"] = expiration
                });
                var header = Declarations(pass.SpawnOperations.Concat(pass.UpdateOperations));
                var source = Wrap(name + "/Step" + pass.Index, header + step, "FluxFX_Step(i.uv)");
                return new FluxParticleGeneratedPass(pass, source, pageReads.Values.OrderBy(r => r.PageId).ThenBy(r => r.Version));
            }

            internal string GenerateClear(FluxParticleStatePage page, string name)
            {
                var channels = new[] { "0.0", "0.0", "0.0", "0.0" };
                if (lifecycle != null)
                {
                    var age = page.Bindings.FirstOrDefault(b => b.ValueId == lifecycle.AgeValueId);
                    if (age != null) channels[age.ChannelOffset] = "-1.0";
                }
                return Wrap(name + "/Clear" + page.Id, "", "float4(" + string.Join(", ", channels) + ")");
            }

            internal string GenerateAvailability(string name)
            {
                Start();
                var age = lifecycle == null ? "-1.0" : State(lifecycle.AgeValueId, FluxParticlePageVersion.Updated);
                var lifetime = lifecycle == null ? "0.0" : SuppliedOrState(lifecycle.LifetimeValueId, true);
                var code = Declarations(Array.Empty<FluxParticleOperationDefinition>()) + @"
float4 _FluxStateSize;
float4 _FluxAllocationSize;
float4 FluxFX_Availability(float2 inputUV)
{
    uint2 pixel = (uint2)floor(inputUV * _FluxAllocationSize.xy);
    uint index = pixel.y * (uint)_FluxAllocationSize.x + pixel.x;
    if (index >= (uint)_FluxStateSize.z) return 0;
    float2 uv = (float2(index % (uint)_FluxStateSize.x, index / (uint)_FluxStateSize.x) + 0.5) / _FluxStateSize.xy;
    float age = " + age + @";
    [branch] if (age < 0.0) return float4(1, 0, 0, 0);
    return age < " + lifetime + @" ? 0 : float4(1, 0, 0, 0);
}
";
                return Wrap(name + "/Availability", code, "FluxFX_Availability(i.uv)")
                    .Replace("    SubShader", "    Properties {\n" + GenerateParameterProperties(program) + "\n    }\n    SubShader");
            }

            internal string GenerateRender()
            {
                Start();
                var operations = program.Operations.Where(op => op.Phase == FluxParticlePhase.Render).ToArray();
                var outputs = operations.SelectMany(op => op.Writes).Select(w => w.ValueId).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray();
                var fields = new StringBuilder();
                foreach (var id in outputs) fields.AppendLine($"    {Type(id)} {Field(id)};");
                var lifecycleCode = lifecycle == null ? "    alive = true;\n" :
                    ExpandTemplate("RenderLifecycle.hlsl.template", new Dictionary<string, string>
                    {
                        ["Age"] = State(lifecycle.AgeValueId, FluxParticlePageVersion.Updated),
                        ["Lifetime"] = SuppliedOrState(lifecycle.LifetimeValueId, true)
                    });
                EmitOperations(operations, true);
                var writes = new StringBuilder();
                foreach (var id in outputs)
                {
                    var writer = operations.Last(op => op.Writes.Any(w => w.ValueId == id));
                    writes.AppendLine($"    result.{Field(id)} = {Local(writer.Id)}.{Field(id)};");
                }
                var render = ExpandTemplate("Render.hlsl.template", new Dictionary<string, string>
                {
                    ["Fields"] = fields.ToString(),
                    ["Lifecycle"] = lifecycleCode,
                    ["Operations"] = body.ToString(),
                    ["Writes"] = writes.ToString()
                });
                return Declarations(operations) + render;
            }

            void EmitPageWrites(FluxParticleStatePage page, IReadOnlyList<FluxParticleOperationDefinition> operations)
            {
                foreach (var binding in page.Bindings)
                {
                    var writer = operations.LastOrDefault(op => op.Writes.Any(w => w.ValueId == binding.ValueId));
                    if (writer == null) continue;
                    body.AppendLine($"        result.{Swizzle(binding)} = (float{Suffix(binding.ComponentCount)}){Local(writer.Id)}.{Field(binding.ValueId)};");
                }
            }

            void Start()
            {
                pageReads = new Dictionary<string, FluxParticlePageRead>(StringComparer.Ordinal);
                localOperations = new HashSet<string>(StringComparer.Ordinal);
                usedProviders = new HashSet<string>(StringComparer.Ordinal);
                body = new StringBuilder();
            }

            void EmitOperations(IEnumerable<FluxParticleOperationDefinition> operations, bool render)
            {
                localOperations.Clear();
                foreach (var projected in operations)
                {
                    // Keep the full original Context even when Spawn writes were projected to one Page.
                    var op = originals[projected.Id];
                    var local = Local(op.Id);
                    body.AppendLine($"        {FluxParticleHlslNames.Context(op.Id)} {local} = ({FluxParticleHlslNames.Context(op.Id)})0;");
                    foreach (var read in op.Reads)
                    {
                        string expression;
                        switch (read.Version)
                        {
                            case FluxParticleReadVersion.Provided:
                                expression = SuppliedOrState(read.ValueId, render);
                                break;
                            case FluxParticleReadVersion.AfterOperation:
                                expression = localOperations.Contains(read.ProducerOperationId)
                                    ? Local(read.ProducerOperationId) + "." + Field(read.ValueId)
                                    : State(read.ValueId, FluxParticlePageVersion.Updated);
                                break;
                            default:
                                expression = State(read.ValueId, render ? FluxParticlePageVersion.Updated : FluxParticlePageVersion.StepPrevious);
                                break;
                        }
                        body.AppendLine($"        {local}.{Field(read.ValueId)} = {expression};");
                    }
                    foreach (var id in op.Parameters)
                    {
                        body.AppendLine($"        {local}.{FluxParticleHlslNames.Parameter(id)} = {FluxParticleHlslNames.ParameterProperty(id)};");
                    }
                    if (op.EnabledParameterId != null)
                    {
                        body.AppendLine($"        if ({FluxParticleHlslNames.ParameterProperty(op.EnabledParameterId)} > 0.5)");
                        body.AppendLine("        {").AppendLine($"            {op.EntryPoint}({local});").AppendLine("        }");
                    }
                    else
                    {
                        body.AppendLine($"        {op.EntryPoint}({local});");
                    }
                    localOperations.Add(op.Id);
                }
            }

            string SuppliedOrState(string id, bool render = false)
            {
                if (!program.Providers.TryGetValue(id, out var provider))
                {
                    return State(id, render ? FluxParticlePageVersion.Updated : FluxParticlePageVersion.StepPrevious);
                }
                usedProviders.Add(id);
                var property = FluxParticleHlslNames.ProviderProperty(id);
                if (provider.Kind == FluxParticleProviderKind.LookupTable)
                {
                    var input = SuppliedOrState(provider.Inputs[0], render);
                    var count = provider.LookupSampleCount;
                    if (provider.LookupPositions.Count != 0)
                    {
                        var terms = provider.LookupPositions.Take(count - 1).Select(position =>
                            $"((saturate({input}) > {position.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}) ? 1.0 : 0.0)");
                        var index = string.Join(" + ", terms);
                        return $"tex2Dlod({property}, float4((({index}) + 0.5) / {count}.0, 0.5, 0.0, 0.0)).{(provider.LookupVectorSamples.Count != 0 ? "rgba" : "r")}";
                    }
                    return $"tex2Dlod({property}, float4((saturate({input}) * {count - 1}.0 + 0.5) / {count}.0, 0.5, 0.0, 0.0)).{(provider.LookupVectorSamples.Count != 0 ? "rgba" : "r")}";
                }
                if (provider.Kind == FluxParticleProviderKind.Uniform) return property;
                if (provider.Kind == FluxParticleProviderKind.SpawnInput)
                {
                    var value = program.Values[id];
                    return $"({Type(id)})tex2D({property}, uv).{"rgba".Substring(0, value.ComponentCount)}";
                }
                var arguments = provider.Inputs.Select(input => SuppliedOrState(input, render))
                    .Concat(provider.Parameters.Select(FluxParticleHlslNames.ParameterProperty));
                return provider.EntryPoint + "(" + string.Join(", ", arguments) + ")";
            }

            string State(string id, FluxParticlePageVersion version)
            {
                if (!program.StateLayout.TryGetBinding(id, out var binding)) throw new InvalidOperationException($"Unbound Shader Value: {id}");
                AddPageRead(binding.PageId, version);
                return $"({Type(id)})tex2D({Page(binding.PageId, version)}, uv).{Swizzle(binding)}";
            }

            void AddPageRead(int pageId, FluxParticlePageVersion version)
            {
                var key = Page(pageId, version);
                if (!pageReads.ContainsKey(key)) pageReads.Add(key, new FluxParticlePageRead(pageId, version));
            }

            string Declarations(IEnumerable<FluxParticleOperationDefinition> operations)
            {
                var result = new StringBuilder("sampler2D _FluxSpawnMask;\n");
                foreach (var read in pageReads.Values.OrderBy(r => r.PageId).ThenBy(r => r.Version))
                {
                    result.AppendLine($"sampler2D {Page(read.PageId, read.Version)};");
                }
                foreach (var p in program.Parameters.Values.OrderBy(p => p.Id, StringComparer.Ordinal))
                {
                    result.AppendLine($"{HlslType(p.Type)} {FluxParticleHlslNames.ParameterProperty(p.Id)};");
                }
                foreach (var p in program.Providers.Values.Where(p => (p.Kind == FluxParticleProviderKind.Uniform || p.Kind == FluxParticleProviderKind.SpawnInput)).OrderBy(p => p.ValueId, StringComparer.Ordinal))
                {
                    result.AppendLine($"{(p.Kind == FluxParticleProviderKind.SpawnInput ? "sampler2D" : Type(p.ValueId))} {FluxParticleHlslNames.ProviderProperty(p.ValueId)};");
                }
                foreach (var provider in program.Providers.Values.Where(provider =>
                    provider.Kind == FluxParticleProviderKind.LookupTable && usedProviders.Contains(provider.ValueId))
                    .OrderBy(provider => provider.ValueId, StringComparer.Ordinal))
                {
                    result.AppendLine($"sampler2D {FluxParticleHlslNames.ProviderProperty(provider.ValueId)};");
                }
                var unique = operations.Select(op => originals[op.Id]).Distinct().ToArray();
                foreach (var op in unique)
                {
                    result.AppendLine($"struct {FluxParticleHlslNames.Context(op.Id)}").AppendLine("{");
                    var ids = op.Reads.Select(r => r.ValueId).Concat(op.Writes.Select(w => w.ValueId)).Distinct().OrderBy(id => id, StringComparer.Ordinal);
                    foreach (var id in ids) result.AppendLine($"    {Type(id)} {Field(id)};");
                    foreach (var id in op.Parameters.Distinct().OrderBy(id => id, StringComparer.Ordinal))
                    {
                        result.AppendLine($"    {HlslType(program.Parameters[id].Type)} {FluxParticleHlslNames.Parameter(id)};");
                    }
                    if (op.Reads.Count == 0 && op.Writes.Count == 0 && op.Parameters.Count == 0) result.AppendLine("    float unused;");
                    result.AppendLine("};");
                }
                // OperationのHLSLがContext型を参照するため、型定義を先に出力する。
                foreach (var path in program.Providers.Values.Where(p => p.Kind == FluxParticleProviderKind.Derived && usedProviders.Contains(p.ValueId)).Select(p => p.HlslPath)
                    .Concat(unique.Select(op => op.HlslPath)).Distinct().OrderBy(p => p, StringComparer.Ordinal))
                {
                    result.AppendLine($"#include \"{path.Replace('\\', '/')}\"");
                }
                return result.ToString();
            }

            string Type(string id) => HlslType(program.Values[id].Type);
            static string Field(string id) => FluxParticleHlslNames.Value(id);
            static string Local(string id) => FluxParticleHlslNames.OperationLocal(id);
            static string Page(int id, FluxParticlePageVersion version) => FluxParticleHlslNames.PageProperty(id, version);
            static string Swizzle(FluxParticleStateBinding binding) => "rgba".Substring(binding.ChannelOffset, binding.ComponentCount);
            static string Suffix(int count) => count == 1 ? "" : count.ToString();

            static string HlslType(FluxParticleValueType type)
            {
                return type == FluxParticleValueType.UInt ? "uint" : "float" + Suffix((int)type);
            }

            static string Wrap(string name, string code, string expression)
            {
                return ExpandTemplate("Shader.shader.template", new Dictionary<string, string>
                {
                    ["ShaderName"] = name,
                    ["Code"] = code,
                    ["Expression"] = expression
                });
            }

        }

        /// <summary>
        /// Parameterの型に対応するShaderLab Property宣言を生成します。
        /// </summary>
        /// <param name="program">Particleのコンパイル結果</param>
        /// <returns>FloatまたはVectorのProperty宣言</returns>
        public static string GenerateParameterProperties(FluxParticleCompiledProgram program)
        {
            return string.Join("\n", program.Parameters.Values.OrderBy(parameter => parameter.Id, StringComparer.Ordinal)
                .Select(parameter =>
                {
                    var property = FluxParticleHlslNames.ParameterProperty(parameter.Id);
                    var scalar = parameter.Type == FluxParticleValueType.Float;
                    var type = scalar ? "Float" : "Vector";
                    var initial = scalar ? "0" : "(0, 0, 0, 0)";
                    return $@"[HideInInspector] {property} (""{property}"", {type}) = {initial}";
                }));
        }
        internal static string ExpandTemplate(string name, IReadOnlyDictionary<string, string> bindings)
        {
            var relativePath = "Editor/Compilation/Templates/" + name;
            var assetPath = "Packages/com.tplab.flux.fx/" + relativePath;
            var package = PackageInfo.FindForAssetPath(assetPath);
            if (package == null) throw new InvalidOperationException($"Shader template package not found: {assetPath}");
            var path = Path.Combine(package.resolvedPath, relativePath);
            var source = File.ReadAllText(path).Replace("\r\n", "\n");
            var used = new HashSet<string>(StringComparer.Ordinal);
            // A standalone token inherits its line's indentation. Inline tokens remain expressions.
            // 挿入したHLSLを再展開しないよう、元のTemplateだけを走査する。
            const string tokens = @"(?m)^(?<indent>[ \t]*)\{\{(?<block>[A-Za-z]+)\}\}[ \t]*(?:\n|\z)|\{\{(?<inline>[A-Za-z]+)\}\}";
            var expanded = Regex.Replace(source, tokens, match =>
            {
                var block = match.Groups["block"].Success;
                var key = match.Groups[block ? "block" : "inline"].Value;
                if (!bindings.TryGetValue(key, out var value))
                {
                    throw new InvalidOperationException($"Unbound template token: {name}/{key}");
                }
                used.Add(key);
                value = value.Replace("\r\n", "\n");
                if (block) return IndentBlock(value, match.Groups["indent"].Value);
                if (value.Contains("\n")) throw new InvalidOperationException($"Multiline value requires a standalone template token: {name}/{key}");
                return value;
            });
            foreach (var key in bindings.Keys)
            {
                if (!used.Contains(key)) throw new InvalidOperationException($"Missing template token: {name}/{key}");
            }
            return expanded;
        }

        static string IndentBlock(string source, string indentation)
        {
            if (string.IsNullOrWhiteSpace(source)) return "";
            var lines = source.TrimEnd('\n').Split('\n');
            var commonIndent = lines.Where(line => !string.IsNullOrWhiteSpace(line))
                .Min(line => line.TakeWhile(c => c == ' ' || c == '\t').Count());
            var result = new StringBuilder();
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    result.Append(indentation).Append(line.Substring(commonIndent));
                }
                result.Append('\n');
            }
            return result.ToString();
        }
    }
}
