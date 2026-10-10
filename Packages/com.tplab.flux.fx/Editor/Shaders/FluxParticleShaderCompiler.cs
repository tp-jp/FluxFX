using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Shaders
{
    public sealed class FluxParticleShaderCompiler
    {
        const string VelocityTemplatePath = "Packages/com.tplab.flux.fx/Runtime/Shaders/Templates/ParticleVelocityUpdate.shader.template";

        readonly FluxParticleGpuModuleRegistry _registry;

        public FluxParticleShaderCompiler(FluxParticleGpuModuleRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public FluxParticleShaderDraft CompileVelocity(FluxParticleCompilePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var modules = new List<(FluxParticleModule module, IFluxParticleGpuModuleDefinition definition)>();

            foreach (var module in plan.GetModules(FluxParticleExecutionStage.VelocityUpdate))
            {
                if (!_registry.TryGet(module.GetType(), out var definition)) continue;

                modules.Add((module, definition));
            }

            modules.Sort((a, b) =>
            {
                var order = a.definition.Order.CompareTo(b.definition.Order);
                return order != 0 ? order : string.CompareOrdinal(a.definition.ModuleId, b.definition.ModuleId);
            });

            var properties = new StringBuilder();
            var uniforms = new StringBuilder();
            var includes = new StringBuilder();
            var operations = new List<(int order, string id, string code)>();
            var collector = new FluxParticleParameterCollector();

            foreach (var entry in modules)
            {
                var definition = entry.definition;
                var path = definition.HlslPath.Replace('\\', '/');

                definition.CollectParameters(entry.module, collector);

                includes.AppendLine($"#include \"{path}\"");
                operations.Add((definition.Order, definition.ModuleId, $"{definition.EntryPoint}(ctx);"));
            }

            foreach (var parameter in collector.Parameters.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                switch (parameter.Type)
                {
                    case FluxParticleParameterType.Float:
                        properties.AppendLine($"{parameter.Name} (\"{parameter.Name}\", Float) = 0");
                        uniforms.AppendLine($"float {parameter.Name};");
                        break;

                    case FluxParticleParameterType.Vector:
                        properties.AppendLine($"{parameter.Name} (\"{parameter.Name}\", Vector) = (0,0,0,0)");
                        uniforms.AppendLine($"float4 {parameter.Name};");
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported GPU parameter type: {parameter.Type}");
                }
            }

            // Legacy処理は残りのModule移行が完了するまで維持する。
            operations.Add((300, "legacy.noise", "FluxFX_LegacyNoise(ctx);"));
            operations.Add((400, "legacy.vortex", "FluxFX_LegacyVortex(ctx);"));

            operations.Sort((a, b) =>
            {
                var order = a.order.CompareTo(b.order);
                return order != 0 ? order : string.CompareOrdinal(a.id, b.id);
            });

            var calls = new StringBuilder();

            foreach (var operation in operations)
            {
                calls.AppendLine(operation.code);
            }

            var source = Normalize(File.ReadAllText(VelocityTemplatePath));
            source = ReplaceBlockSlot(source, "{{MODULE_PROPERTIES}}", properties.ToString().TrimEnd());
            source = ReplaceBlockSlot(source, "{{MODULE_UNIFORMS}}", uniforms.ToString().TrimEnd());
            source = ReplaceBlockSlot(source, "{{MODULE_INCLUDES}}", includes.ToString().TrimEnd());
            source = ReplaceBlockSlot(source, "{{MODULE_CALLS}}", calls.ToString().TrimEnd());

            return new FluxParticleShaderDraft(FluxParticleExecutionStage.VelocityUpdate, source);
        }

        static string ReplaceBlockSlot(string source, string slot, string value)
        {
            var index = FindSlot(source, slot);
            var lineStart = source.LastIndexOf('\n', index);
            lineStart = lineStart < 0 ? 0 : lineStart + 1;

            var indent = source.Substring(lineStart, index - lineStart);
            var lineEnd = source.IndexOf('\n', index + slot.Length);
            lineEnd = lineEnd < 0 ? source.Length : lineEnd;

            if (indent.Trim().Length != 0 || source.Substring(index + slot.Length, lineEnd - index - slot.Length).Trim().Length != 0)
            {
                throw new InvalidOperationException($"Shader template block slot must be the only content on its line: {slot}");
            }

            var normalizedValue = Normalize(value);
            var indentedValue = normalizedValue.Length == 0 ? "" : indent + normalizedValue.Replace("\n", "\n" + indent);

            return source.Substring(0, lineStart) + indentedValue + source.Substring(index + slot.Length);
        }

        static int FindSlot(string source, string slot)
        {
            var index = source.IndexOf(slot, StringComparison.Ordinal);

            if (index < 0 || source.IndexOf(slot, index + slot.Length, StringComparison.Ordinal) >= 0)
            {
                throw new InvalidOperationException($"Shader template slot must appear exactly once: {slot}");
            }

            return index;
        }

        static string Normalize(string value)
        {
            return value.Replace("\r\n", "\n").Replace('\r', '\n');
        }
    }
}