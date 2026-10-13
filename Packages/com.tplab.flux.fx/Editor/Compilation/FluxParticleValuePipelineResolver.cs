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
        /// 指定したValueのWriterを直列化し、最終WriterのIDを返します。
        /// Writerがなければnullを返します。順序の意味は呼び出し側が定義します。
        /// </summary>
        /// <param name="definitions">Operationを登録済みの論理定義。</param>
        /// <param name="valueId">接続対象のValue ID。</param>
        /// <param name="getOrder">Operationごとの明示的な順序。</param>
        /// <returns>最終Writer ID。Writerがなければnull。</returns>
        public static string Resolve(FluxParticleDefinitionBuilder definitions, string valueId,
            Func<FluxParticleOperationDefinition, int?> getOrder)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (string.IsNullOrWhiteSpace(valueId)) throw new ArgumentException("Value ID is required.", nameof(valueId));
            if (getOrder == null) throw new ArgumentNullException(nameof(getOrder));

            var writers = definitions.Operations.Values.Where(operation => operation.Phase == FluxParticlePhase.Update &&
                operation.Writes.Any(write => write.ValueId == valueId)).ToArray();
            if (writers.Length == 0) return null;
            if (writers.Length > 1)
            {
                if (writers.Any(operation => !getOrder(operation).HasValue))
                {
                    throw new InvalidOperationException($"Writers of {valueId} require an explicit order.");
                }
                var duplicate = writers.GroupBy(operation => getOrder(operation).Value)
                    .FirstOrDefault(group => group.Count() > 1);
                if (duplicate != null)
                {
                    throw new InvalidOperationException($"Conflicting writer order for {valueId}: {duplicate.Key}.");
                }
                writers = writers.OrderBy(operation => getOrder(operation).Value).ToArray();
            }

            string previous = null;
            foreach (var writer in writers)
            {
                var reads = writer.Reads.Select(read => read.ValueId == valueId
                    ? new FluxParticleValueRead(valueId, previous == null ? FluxParticleReadVersion.StepPrevious :
                        FluxParticleReadVersion.AfterOperation, previous)
                    : read).ToArray();
                var dependencies = writer.Dependencies.ToList();
                if (previous != null && !dependencies.Contains(previous)) dependencies.Add(previous);
                definitions.ReplaceOperation(new FluxParticleOperationDefinition(writer.Id, writer.OwnerId,
                    writer.Phase, reads, writer.Writes, dependencies, writer.HlslPath, writer.EntryPoint,
                    writer.Parameters, writer.EnabledParameterId, writer.VelocityOrder));
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
                consumer.Parameters, consumer.EnabledParameterId, consumer.VelocityOrder);
        }
    }
}
