using System;
using System.Collections.Generic;
using System.Linq;

namespace TpLab.Flux.FX.Editor.Compilation.Definitions
{
    /// <summary>
    /// 対象Operationが存在する場合だけ有効になる順序制約です。
    /// </summary>
    public sealed class FluxParticleOptionalOperationDependency
    {
        /// <summary>
        /// 後続OperationのID
        /// </summary>
        public string OperationId { get; }

        /// <summary>
        /// 存在する場合に先行するOperationのID
        /// </summary>
        public string AfterOperationId { get; }

        public FluxParticleOptionalOperationDependency(string operationId, string afterOperationId)
        {
            if (string.IsNullOrWhiteSpace(operationId)) throw new ArgumentException("Operation ID is required.", nameof(operationId));
            if (string.IsNullOrWhiteSpace(afterOperationId)) throw new ArgumentException("Predecessor ID is required.", nameof(afterOperationId));
            if (operationId == afterOperationId) throw new ArgumentException("An Operation cannot depend on itself.", nameof(afterOperationId));
            OperationId = operationId;
            AfterOperationId = afterOperationId;
        }
    }

    /// <summary>
    /// 必須依存を維持しながら、存在するOperation間の任意順序制約を解決します。
    /// </summary>
    public static class FluxParticleOptionalDependencyResolver
    {
        /// <summary>
        /// 指定ValueのWriterの順序を返します。入力のOperation定義は変更しません。
        /// </summary>
        public static IReadOnlyList<string> OrderWriters(FluxParticleDefinitionBuilder definitions, string valueId,
            IEnumerable<FluxParticleOptionalOperationDependency> optionalDependencies, FluxParticlePhase phase = FluxParticlePhase.Update)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (string.IsNullOrWhiteSpace(valueId)) throw new ArgumentException("Value ID is required.", nameof(valueId));
            if (optionalDependencies == null) throw new ArgumentNullException(nameof(optionalDependencies));

            var writers = definitions.Operations.Values.Where(operation => operation.Phase == phase &&
                operation.Writes.Any(write => write.ValueId == valueId)).ToDictionary(operation => operation.Id, StringComparer.Ordinal);
            var successors = writers.Keys.ToDictionary(id => id, id => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
            foreach (var operation in writers.Values)
            {
                foreach (var id in operation.Dependencies)
                {
                    if (!definitions.Operations.TryGetValue(id, out var predecessor))
                    {
                        throw new InvalidOperationException($"Unknown dependency: {operation.Id} -> {id}.");
                    }
                    if (predecessor.Phase != operation.Phase)
                    {
                        throw new InvalidOperationException($"Cross-phase dependency: {operation.Id} -> {id}.");
                    }
                    if (writers.ContainsKey(id)) successors[id].Add(operation.Id);
                }
                foreach (var read in operation.Reads.Where(read => read.Version == FluxParticleReadVersion.AfterOperation))
                {
                    if (!definitions.Operations.TryGetValue(read.ProducerOperationId, out var producer))
                    {
                        throw new InvalidOperationException($"Unknown producer: {operation.Id} -> {read.ProducerOperationId}.");
                    }
                    if (producer.Phase != operation.Phase)
                    {
                        throw new InvalidOperationException($"Cross-phase producer: {operation.Id} -> {producer.Id}.");
                    }
                    if (writers.ContainsKey(producer.Id)) successors[producer.Id].Add(operation.Id);
                }
            }
            foreach (var constraint in optionalDependencies)
            {
                if (constraint == null) throw new ArgumentException("Null optional dependency.", nameof(optionalDependencies));
                if (writers.ContainsKey(constraint.OperationId) && writers.ContainsKey(constraint.AfterOperationId))
                {
                    successors[constraint.AfterOperationId].Add(constraint.OperationId);
                }
            }

            var indegree = writers.Keys.ToDictionary(id => id, id => 0, StringComparer.Ordinal);
            foreach (var edges in successors.Values)
            {
                foreach (var id in edges) indegree[id]++;
            }
            var order = new List<string>();
            while (indegree.Count != 0)
            {
                var ready = indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key).ToArray();
                if (ready.Length == 0) throw new InvalidOperationException($"Cyclic writer dependencies for {valueId}.");
                if (ready.Length > 1) throw new InvalidOperationException($"Ambiguous writer dependencies for {valueId}: {string.Join(", ", ready)}.");
                var id = ready[0];
                order.Add(id);
                indegree.Remove(id);
                foreach (var next in successors[id]) indegree[next]--;
            }
            return order.AsReadOnly();
        }
    }
}
