using System;
using System.Collections.Generic;
using System.Linq;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// Update Phaseで同じValueを更新するOperationを登録順に依存せず接続します。
    /// 明示的な順序がない複数Writerは曖昧として拒否します。
    /// </summary>
    public static class FluxParticleValuePipelineResolver
    {
        /// <summary>
        /// Optional Dependencyを考慮したWriterの順序を確定し、入力と先行Operationを接続します。
        /// </summary>
        public static string Resolve(FluxParticleDefinitionBuilder definitions, string valueId,
            IReadOnlyList<FluxParticleOptionalOperationDependency> optionalDependencies)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var order = FluxParticleOptionalDependencyResolver.OrderWriters(definitions, valueId, optionalDependencies);
            string previous = null;
            foreach (var id in order)
            {
                var writer = definitions.Operations[id];
                var reads = writer.Reads.Select(read => read.ValueId == valueId
                    ? new FluxParticleValueRead(valueId, previous == null ? FluxParticleReadVersion.StepPrevious :
                        FluxParticleReadVersion.AfterOperation, previous)
                    : read).ToArray();
                var dependencies = writer.Dependencies.ToList();
                if (previous != null && !dependencies.Contains(previous)) dependencies.Add(previous);
                definitions.ReplaceOperation(new FluxParticleOperationDefinition(writer.Id, writer.OwnerId,
                    writer.Phase, reads, writer.Writes, dependencies, writer.HlslPath, writer.EntryPoint,
                    writer.Parameters, writer.EnabledParameterId));
                previous = writer.Id;
            }
            return previous;
        }

        /// <summary>
        /// Update Consumerの対象Value読み取りを最終Writerに接続します。
        /// </summary>
        public static FluxParticleOperationDefinition Connect(FluxParticleOperationDefinition consumer,
            string valueId, string finalWriterId)
        {
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));
            if (string.IsNullOrWhiteSpace(valueId)) throw new ArgumentException("Value ID is required.", nameof(valueId));
            if (consumer.Phase != FluxParticlePhase.Update)
            {
                throw new InvalidOperationException("Pipeline consumers must be Update Operations.");
            }
            if (!consumer.Reads.Any(read => read.ValueId == valueId))
            {
                throw new InvalidOperationException($"Operation {consumer.Id} does not read {valueId}.");
            }
            var reads = consumer.Reads.Select(read => read.ValueId == valueId
                ? new FluxParticleValueRead(valueId, finalWriterId == null ? FluxParticleReadVersion.StepPrevious :
                    FluxParticleReadVersion.AfterOperation, finalWriterId)
                : read).ToArray();
            var dependencies = consumer.Dependencies.ToList();
            if (finalWriterId != null && !dependencies.Contains(finalWriterId)) dependencies.Add(finalWriterId);
            return new FluxParticleOperationDefinition(consumer.Id, consumer.OwnerId, consumer.Phase, reads,
                consumer.Writes, dependencies, consumer.HlslPath, consumer.EntryPoint,
                consumer.Parameters, consumer.EnabledParameterId);
        }
    }
}
