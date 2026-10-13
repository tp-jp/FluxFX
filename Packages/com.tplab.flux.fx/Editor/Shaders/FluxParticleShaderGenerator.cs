using System;
using System.IO;
using System.Text;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Shaders
{
    public sealed class FluxParticleShaderGenerator
    {
        public FluxParticleShaderDraft Generate(
            FluxParticleGpuStageCompilation stage,
            FluxParticleGpuStageDescriptor descriptor)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

            if (stage.Stage != descriptor.Stage)
            {
                throw new ArgumentException(
                    $"Expected {descriptor.Stage} stage, but received {stage.Stage}.",
                    nameof(stage));
            }

            var properties = new StringBuilder();
            var uniforms = new StringBuilder();
            var includes = new StringBuilder();
            var calls = new StringBuilder();

            foreach (var parameter in stage.Parameters)
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

            foreach (var module in stage.Modules)
            {
                includes.AppendLine($"#include \"{module.HlslPath}\"");

                calls.AppendLine($"if ({module.EnabledParameter.Name} > 0.5)");
                calls.AppendLine("{");
                calls.AppendLine($"    {module.EntryPoint}(ctx);");
                calls.AppendLine("}");
            }

            var source = Normalize(File.ReadAllText(descriptor.ShaderTemplatePath));

            source = ReplaceBlockSlot(source, "{{MODULE_PROPERTIES}}", properties.ToString().TrimEnd());
            source = ReplaceBlockSlot(source, "{{MODULE_UNIFORMS}}", uniforms.ToString().TrimEnd());
            source = ReplaceBlockSlot(source, "{{MODULE_INCLUDES}}", includes.ToString().TrimEnd());
            source = ReplaceBlockSlot(source, "{{MODULE_CALLS}}", calls.ToString().TrimEnd());

            return new FluxParticleShaderDraft(stage.Stage, source);
        }

        public FluxParticleShaderDraft Generate(
            FluxParticleGpuCompilation compilation,
            FluxParticleExecutionStage stage)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));

            var stageCompilation = compilation.GetStage(stage);

            if (stageCompilation == null)
            {
                throw new InvalidOperationException($"GPU compilation stage was not found: {stage}.");
            }

            var descriptor = FluxParticleGpuStageRegistry.GetRequired(stage);

            return Generate(stageCompilation, descriptor);
        }

        public FluxParticleShaderDraft GenerateVelocity(FluxParticleGpuCompilation compilation)
        {
            return Generate(compilation, FluxParticleExecutionStage.VelocityUpdate);
        }

        public FluxParticleShaderDraft GenerateVelocity(FluxParticleGpuStageCompilation stage)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));

            var descriptor = FluxParticleGpuStageRegistry.GetRequired(FluxParticleExecutionStage.VelocityUpdate);

            return Generate(stage, descriptor);
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