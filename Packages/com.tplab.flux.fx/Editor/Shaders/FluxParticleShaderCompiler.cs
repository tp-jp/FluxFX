using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Shaders
{
    public sealed class FluxParticleShaderCompiler
    {
        const string CompilerVersion = "D3-B-1";
        const string VelocityShaderPath = "Packages/com.tplab.flux.fx/Runtime/Shaders/ParticleVelocityUpdate.shader";
        const string SourceShaderName = "FluxFX/ParticleVelocityUpdate";

        const string ParameterMarker = "            float _SpawnCount;";
        const string FunctionMarker = "            float4 frag(v2f_img i) : SV_Target";

        const string VelocityBlockStart = "                velocity.xyz += _Gravity.xyz * _DeltaTime;";
        const string VelocityBlockEnd = "                velocity.xyz = ApplyVelocityLimit(velocity.xyz);";

        readonly FluxParticleGpuModuleRegistry _registry;

        public FluxParticleShaderCompiler(FluxParticleGpuModuleRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public FluxParticleShaderCompilation CompileVelocity(FluxParticleCompilePlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            var definitions = new List<FluxParticleGpuModuleDefinition>();

            foreach (var module in plan.GetModules(FluxParticleExecutionStage.VelocityUpdate))
            {
                if (_registry.TryGet(module.GetType(), out var definition))
                    definitions.Add(definition);
            }

            definitions.Sort((a, b) =>
            {
                var order = a.Order.CompareTo(b.Order);
                return order != 0 ? order : string.CompareOrdinal(a.ModuleId, b.ModuleId);
            });

            var source = Normalize(File.ReadAllText(VelocityShaderPath));
            var includes = new StringBuilder();
            var dependencies = new StringBuilder();
            var operations = new List<(int order, string id, string code)>();

            foreach (var definition in definitions)
            {
                var path = definition.HlslPath.Replace('\\', '/');

                if (!File.Exists(path))
                    throw new FileNotFoundException($"GPU Module HLSL was not found: {path}", path);

                var hlsl = Normalize(File.ReadAllText(path));

                if (hlsl.Contains("#include"))
                    throw new InvalidOperationException($"Nested HLSL includes are not supported in D3-B: {path}");

                includes.AppendLine($"            #include \"{path}\"");

                dependencies.Append(definition.ModuleId).Append('\n');
                dependencies.Append(definition.EntryPoint).Append('\n');
                dependencies.Append(definition.Order).Append('\n');
                dependencies.Append(path).Append('\n');
                dependencies.Append(hlsl).Append('\n');

                operations.Add((
                    definition.Order,
                    definition.ModuleId,
                    $"                {definition.EntryPoint}(velocity, deltaTime);"));
            }

            operations.Add((200, "legacy.force", "                velocity += _Force.xyz * deltaTime;"));
            operations.Add((300, "legacy.noise", "                float3 noise = EvaluateNoise(position);\n                velocity += noise * _NoiseStrength * deltaTime;"));
            operations.Add((400, "legacy.vortex", "                float3 vortex = EvaluateVortex(position);\n                velocity += vortex * deltaTime;"));
            operations.Add((600, "legacy.limit", "                velocity = ApplyVelocityLimit(velocity);"));

            // D3-Bの移行対象外Moduleは既存Shader処理を維持する。
            if (!definitions.Any(x => x.ModuleId == "tplab.fluxfx.gravity"))
                operations.Add((100, "legacy.gravity", "                velocity += _Gravity.xyz * deltaTime;"));

            if (!definitions.Any(x => x.ModuleId == "tplab.fluxfx.drag"))
                operations.Add((500, "legacy.drag", "                velocity *= exp(-_Drag * deltaTime);"));

            operations.Sort((a, b) =>
            {
                var order = a.order.CompareTo(b.order);
                return order != 0 ? order : string.CompareOrdinal(a.id, b.id);
            });

            var function = new StringBuilder();
            function.AppendLine("            void FluxFX_ApplyVelocityModules(inout float3 velocity, float3 position, float deltaTime)");
            function.AppendLine("            {");

            foreach (var operation in operations)
                function.AppendLine(operation.code);

            function.AppendLine("            }");
            function.AppendLine();

            source = ReplaceOnce(source, ParameterMarker, ParameterMarker + "\n" + includes);
            source = ReplaceOnce(source, FunctionMarker, function + FunctionMarker);
            source = ReplaceVelocityBlock(source, "                FluxFX_ApplyVelocityModules(velocity.xyz, position.xyz, _DeltaTime);");

            var hashInput = CompilerVersion + "\n" + source + "\n" + dependencies;
            var cacheKey = ComputeHash(hashInput);
            var shaderName = $"Hidden/FluxFX/Generated/Velocity_{cacheKey}";

            source = ReplaceOnce(source, $"Shader \"{SourceShaderName}\"", $"Shader \"{shaderName}\"");

            return new FluxParticleShaderCompilation(
                FluxParticleExecutionStage.VelocityUpdate,
                shaderName,
                source,
                cacheKey);
        }

        static string ReplaceVelocityBlock(string source, string replacement)
        {
            var start = source.IndexOf(VelocityBlockStart, StringComparison.Ordinal);
            if (start < 0)
                throw new InvalidOperationException("Velocity block start was not found.");

            var end = source.IndexOf(VelocityBlockEnd, start, StringComparison.Ordinal);
            if (end < 0)
                throw new InvalidOperationException("Velocity block end was not found.");

            end += VelocityBlockEnd.Length;

            return source.Substring(0, start) + replacement + source.Substring(end);
        }

        static string ReplaceOnce(string source, string before, string after)
        {
            var index = source.IndexOf(before, StringComparison.Ordinal);
            if (index < 0)
                throw new InvalidOperationException($"Shader template marker was not found: {before}");

            return source.Substring(0, index) + after + source.Substring(index + before.Length);
        }

        static string Normalize(string value)
        {
            return value.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        static string ComputeHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
