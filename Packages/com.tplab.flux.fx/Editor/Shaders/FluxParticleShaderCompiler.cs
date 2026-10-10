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
        const string CompilerVersion = "D5-A3";
        const string VelocityShaderPath = "Packages/com.tplab.flux.fx/Runtime/Shaders/ParticleVelocityUpdate.shader";
        const string ContextHlslPath = "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/FluxFXGpuContext.hlsl";
        const string SourceShaderName = "FluxFX/ParticleVelocityUpdate";

        const string PropertyMarker = "        _PositionTex (\"Position\", 2D) = \"black\" {}";
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
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var modules = new List<(FluxParticleModule module, IFluxParticleGpuModuleDefinition definition)>();

            foreach (var module in plan.GetModules(FluxParticleExecutionStage.VelocityUpdate))
            {
                if (_registry.TryGet(module.GetType(), out var definition))
                {
                    modules.Add((module, definition));
                }
            }

            modules.Sort((a, b) =>
            {
                var order = a.definition.Order.CompareTo(b.definition.Order);
                return order != 0 ? order : string.CompareOrdinal(a.definition.ModuleId, b.definition.ModuleId);
            });

            var source = Normalize(File.ReadAllText(VelocityShaderPath));
            var contextHlsl = Normalize(File.ReadAllText(ContextHlslPath));
            var includes = new StringBuilder();
            var dependencies = new StringBuilder();
            var operations = new List<(int order, string id, string code)>();
            var collector = new FluxParticleParameterCollector();

            dependencies.Append(ContextHlslPath).Append('\n');
            dependencies.Append(contextHlsl).Append('\n');

            foreach (var entry in modules)
            {
                var definition = entry.definition;
                var path = definition.HlslPath.Replace('\\', '/');

                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"GPU Module HLSL was not found: {path}", path);
                }

                var hlsl = Normalize(File.ReadAllText(path));

                if (hlsl.Contains("#include"))
                {
                    throw new InvalidOperationException($"Nested HLSL includes are not supported: {path}");
                }

                definition.CollectParameters(entry.module, collector);

                includes.AppendLine($"            #include \"{path}\"");

                dependencies.Append(definition.ModuleId).Append('\n');
                dependencies.Append(definition.EntryPoint).Append('\n');
                dependencies.Append(definition.Order).Append('\n');
                dependencies.Append(path).Append('\n');
                dependencies.Append(hlsl).Append('\n');

                operations.Add((
                    definition.Order,
                    definition.ModuleId,
                    $"                {definition.EntryPoint}(ctx);"));
            }

            var properties = new StringBuilder();
            var uniforms = new StringBuilder();

            foreach (var parameter in collector.Parameters.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                switch (parameter.Type)
                {
                    case FluxParticleParameterType.Float:
                        properties.AppendLine($"        {parameter.Name} (\"{parameter.Name}\", Float) = 0");
                        uniforms.AppendLine($"            float {parameter.Name};");
                        break;

                    case FluxParticleParameterType.Vector:
                        properties.AppendLine($"        {parameter.Name} (\"{parameter.Name}\", Vector) = (0,0,0,0)");
                        uniforms.AppendLine($"            float4 {parameter.Name};");
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported GPU parameter type: {parameter.Type}");
                }
            }

            operations.Add((200, "legacy.force", "                ctx.velocity += _Force.xyz * ctx.deltaTime;"));
            operations.Add((300, "legacy.noise", "                float3 noise = EvaluateNoise(ctx.position);\n                ctx.velocity += noise * _NoiseStrength * ctx.deltaTime;"));
            operations.Add((400, "legacy.vortex", "                float3 vortex = EvaluateVortex(ctx.position);\n                ctx.velocity += vortex * ctx.deltaTime;"));
            operations.Add((600, "legacy.limit", "                ctx.velocity = ApplyVelocityLimit(ctx.velocity);"));

            // 未登録ModuleのLegacy処理を維持する。
            if (!modules.Any(x => x.definition.ModuleId == "tplab.fluxfx.gravity"))
            {
                operations.Add((100, "legacy.gravity", "                ctx.velocity += _Gravity.xyz * ctx.deltaTime;"));
            }

            if (!modules.Any(x => x.definition.ModuleId == "tplab.fluxfx.drag"))
            {
                operations.Add((500, "legacy.drag", "                ctx.velocity *= exp(-_Drag * ctx.deltaTime);"));
            }

            operations.Sort((a, b) =>
            {
                var order = a.order.CompareTo(b.order);
                return order != 0 ? order : string.CompareOrdinal(a.id, b.id);
            });

            var function = new StringBuilder();
            function.AppendLine("            void FluxFX_ApplyVelocityModules(inout FluxFXGpuContext ctx)");
            function.AppendLine("            {");

            foreach (var operation in operations)
            {
                function.AppendLine(operation.code);
            }

            function.AppendLine("            }");
            function.AppendLine();

            var contextInclude = $"            #include \"{ContextHlslPath}\"\n";

            source = ReplaceOnce(source, PropertyMarker, PropertyMarker + "\n" + properties);
            source = ReplaceOnce(source, ParameterMarker, ParameterMarker + "\n" + uniforms + contextInclude + includes);
            source = ReplaceOnce(source, FunctionMarker, function + FunctionMarker);

            var velocityBlock = new StringBuilder();
            velocityBlock.AppendLine("                FluxFXGpuContext ctx;");
            velocityBlock.AppendLine("                ctx.position = position.xyz;");
            velocityBlock.AppendLine("                ctx.velocity = velocity.xyz;");
            velocityBlock.AppendLine("                ctx.age = position.w;");
            velocityBlock.AppendLine("                ctx.lifetime = velocity.w;");
            velocityBlock.AppendLine("                ctx.deltaTime = _DeltaTime;");
            velocityBlock.AppendLine("                ctx.simulationTime = _FluxFXSimulationTime;");
            velocityBlock.AppendLine();
            velocityBlock.AppendLine("                FluxFX_ApplyVelocityModules(ctx);");
            velocityBlock.Append("                velocity.xyz = ctx.velocity;");

            source = ReplaceVelocityBlock(source, velocityBlock.ToString());

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
            {
                throw new InvalidOperationException("Velocity block start was not found.");
            }

            var end = source.IndexOf(VelocityBlockEnd, start, StringComparison.Ordinal);
            if (end < 0)
            {
                throw new InvalidOperationException("Velocity block end was not found.");
            }

            end += VelocityBlockEnd.Length;

            return source.Substring(0, start) + replacement + source.Substring(end);
        }

        static string ReplaceOnce(string source, string before, string after)
        {
            var index = source.IndexOf(before, StringComparison.Ordinal);
            if (index < 0)
            {
                throw new InvalidOperationException($"Shader template marker was not found: {before}");
            }

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