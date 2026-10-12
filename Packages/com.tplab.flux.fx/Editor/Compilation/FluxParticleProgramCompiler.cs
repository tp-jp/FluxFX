using System;
using System.Collections.Generic;
using System.Linq;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// ValueとOperationの宣言から、最小限のState LayoutとGPU Pass計画を生成します。
    /// この段階ではShader/Material生成と物理RT Allocationは行いません。
    /// </summary>
    public sealed class FluxParticleProgramCompiler
    {
        /// <summary>
        /// 登録済みのValue・Provider・Operationから検証済みProgramを生成します。
        /// </summary>
        /// <param name="definitions">
        /// CoreとModuleから収集した定義。
        /// </param>
        /// <returns>
        /// State LayoutとExecutionPlanを含む検証済みProgram。
        /// </returns>
        public FluxParticleCompiledProgram Compile(FluxParticleDefinitionBuilder definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            ValidateProviders(definitions);
            var operations = SortAndValidateOperations(definitions);
            var persistent = ResolvePersistentValues(definitions, operations);
            var plans = BuildPagePlans(persistent, operations);
            var layout = CreateLayout(plans, definitions.Values);
            var passes = SchedulePasses(plans, layout, operations, definitions);

            return new FluxParticleCompiledProgram(layout, new FluxParticleExecutionPlan(passes), operations);
        }

        static void ValidateProviders(FluxParticleDefinitionBuilder definitions)
        {
            var marks = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var provider in definitions.Providers.Values)
            {
                if (!definitions.Values.ContainsKey(provider.ValueId))
                {
                    throw new InvalidOperationException($"Provider has undefined Value: {provider.ValueId}");
                }
                foreach (var parameterId in provider.Parameters)
                {
                    if (!definitions.Parameters.ContainsKey(parameterId))
                    {
                        throw new InvalidOperationException($"Unknown Parameter: {parameterId} in {provider.ValueId}");
                    }
                }
                foreach (var valueId in provider.Inputs)
                {
                    if (!definitions.Values.ContainsKey(valueId))
                    {
                        throw new InvalidOperationException($"Unknown provider input: {valueId} in {provider.ValueId}");
                    }
                }
            }

            foreach (var valueId in definitions.Providers.Keys.OrderBy(id => id, StringComparer.Ordinal))
            {
                ValidateProviderChain(valueId, definitions.Providers, marks);
            }
        }

        static void ValidateProviderChain(
            string valueId,
            IReadOnlyDictionary<string, FluxParticleValueProvider> providers,
            Dictionary<string, int> marks)
        {
            if (!providers.TryGetValue(valueId, out var provider)) return;
            if (marks.TryGetValue(valueId, out var mark))
            {
                if (mark == 1) throw new InvalidOperationException($"Cyclic provider dependency: {valueId}");
                if (mark == 2) return;
            }

            marks[valueId] = 1;
            foreach (var input in provider.Inputs) ValidateProviderChain(input, providers, marks);
            marks[valueId] = 2;
        }

        static List<FluxParticleOperationDefinition> SortAndValidateOperations(FluxParticleDefinitionBuilder definitions)
        {
            var operations = definitions.Operations;
            var marks = new Dictionary<string, int>(StringComparer.Ordinal);
            var sorted = new List<FluxParticleOperationDefinition>();

            foreach (var operationId in operations.Keys.OrderBy(id => id, StringComparer.Ordinal))
            {
                VisitOperation(operationId, operations, marks, sorted);
            }

            var order = sorted.Select((operation, index) => new { operation.Id, index }).ToDictionary(x => x.Id, x => x.index, StringComparer.Ordinal);

            foreach (var operation in sorted)
            {
                if (operation.Phase != FluxParticlePhase.Render && operation.Writes.Count == 0)
                {
                    throw new InvalidOperationException($"Spawn/Update Operation has no State output: {operation.Id}");
                }
                foreach (var parameterId in operation.Parameters)
                {
                    if (!definitions.Parameters.TryGetValue(parameterId, out var parameter))
                    {
                        throw new InvalidOperationException($"Unknown Operation Parameter: {operation.Id}/{parameterId}");
                    }
                    if (parameter.Policy == FluxParticleParameterPolicy.SpawnLocked && operation.Phase != FluxParticlePhase.Spawn)
                    {
                        throw new InvalidOperationException($"SpawnLocked Operation Parameter requires materialization: {operation.Id}/{parameterId}");
                    }
                }
                if (operation.EnabledParameterId != null)
                {
                    if (operation.Phase != FluxParticlePhase.Update ||
                        !definitions.Parameters.TryGetValue(operation.EnabledParameterId, out var enabled) ||
                        enabled.Type != FluxParticleValueType.Float ||
                        operation.Writes.Any(write => write.Kind != FluxParticleWriteKind.Modify ||
                            !operation.Reads.Any(read => read.ValueId == write.ValueId)))
                    {
                        throw new InvalidOperationException($"Enabled Operation requires Update Modify with explicit fallback reads: {operation.Id}");
                    }
                }
                var written = new HashSet<string>(StringComparer.Ordinal);
                foreach (var write in operation.Writes)
                {
                    if (!written.Add(write.ValueId)) throw new InvalidOperationException($"Duplicate write: {operation.Id}/{write.ValueId}");
                    if (!definitions.Values.TryGetValue(write.ValueId, out var value))
                    {
                        throw new InvalidOperationException($"Unknown written Value: {write.ValueId} in {operation.Id}");
                    }
                    if (value.WritePolicy == FluxParticleWritePolicy.ReadOnly ||
                        (value.WritePolicy == FluxParticleWritePolicy.OwnerManaged && value.OwnerId != operation.OwnerId) ||
                        (value.WritePolicy == FluxParticleWritePolicy.SystemManaged && operation.OwnerId != "fluxfx.core"))
                    {
                        throw new InvalidOperationException($"Write is not permitted: {operation.Id}/{write.ValueId}");
                    }
                    if (operation.Phase == FluxParticlePhase.Spawn && write.Kind != FluxParticleWriteKind.Initialize)
                    {
                        throw new InvalidOperationException($"Spawn must initialize Value: {operation.Id}/{write.ValueId}");
                    }
                    if (operation.Phase != FluxParticlePhase.Spawn && write.Kind == FluxParticleWriteKind.Initialize)
                    {
                        throw new InvalidOperationException($"Initialize is only valid in Spawn: {operation.Id}/{write.ValueId}");
                    }
                }

                foreach (var read in operation.Reads)
                {
                    if (!definitions.Values.ContainsKey(read.ValueId))
                    {
                        throw new InvalidOperationException($"Unknown read Value: {read.ValueId} in {operation.Id}");
                    }
                    if (read.Version == FluxParticleReadVersion.Provided)
                    {
                        if (!definitions.Providers.TryGetValue(read.ValueId, out var provider))
                        {
                            throw new InvalidOperationException($"Missing provider: {read.ValueId} in {operation.Id}");
                        }
                        ValidateProviderUsage(read.ValueId, operation.Phase, definitions, new HashSet<string>(StringComparer.Ordinal));
                    }
                    else if (read.Version == FluxParticleReadVersion.AfterOperation)
                    {
                        if (!operations.TryGetValue(read.ProducerOperationId, out var producer) ||
                            producer.Phase != operation.Phase || order[producer.Id] >= order[operation.Id] ||
                            !producer.Writes.Any(write => write.ValueId == read.ValueId) ||
                            !DependsOn(operation.Id, producer.Id, operations, new HashSet<string>(StringComparer.Ordinal)))
                        {
                            throw new InvalidOperationException($"Invalid AfterOperation read: {operation.Id}/{read.ValueId}");
                        }
                    }
                    else if (read.Version == FluxParticleReadVersion.Committed && operation.Phase != FluxParticlePhase.Render)
                    {
                        throw new InvalidOperationException($"Committed read is only valid in Rendering: {operation.Id}");
                    }
                    else if (read.Version == FluxParticleReadVersion.StepPrevious && operation.Phase != FluxParticlePhase.Update)
                    {
                        throw new InvalidOperationException($"StepPrevious is only valid in Update: {operation.Id}");
                    }
                }
            }

            ValidateWriterOrdering(sorted, operations);
            return sorted;
        }

        static void VisitOperation(
            string id,
            IReadOnlyDictionary<string, FluxParticleOperationDefinition> operations,
            Dictionary<string, int> marks,
            List<FluxParticleOperationDefinition> result)
        {
            if (!operations.TryGetValue(id, out var operation)) throw new InvalidOperationException($"Unknown Operation dependency: {id}");
            if (marks.TryGetValue(id, out var mark))
            {
                if (mark == 1) throw new InvalidOperationException($"Cyclic Operation dependency: {id}");
                if (mark == 2) return;
            }

            marks[id] = 1;
            foreach (var dependency in operation.Dependencies.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (!operations.TryGetValue(dependency, out var predecessor) || predecessor.Phase != operation.Phase)
                {
                    throw new InvalidOperationException($"Dependency must target the same Phase: {operation.Id} -> {dependency}");
                }
                VisitOperation(dependency, operations, marks, result);
            }
            marks[id] = 2;
            result.Add(operation);
        }

        static bool DependsOn(
            string id,
            string target,
            IReadOnlyDictionary<string, FluxParticleOperationDefinition> operations,
            HashSet<string> visited)
        {
            if (!visited.Add(id)) return false;
            foreach (var dependency in operations[id].Dependencies)
            {
                if (dependency == target || DependsOn(dependency, target, operations, visited)) return true;
            }
            return false;
        }

        static void ValidateWriterOrdering(
            IReadOnlyList<FluxParticleOperationDefinition> operations,
            IReadOnlyDictionary<string, FluxParticleOperationDefinition> lookup)
        {
            foreach (var group in operations.SelectMany(operation => operation.Writes.Select(write => new { operation, write.ValueId }))
                         .GroupBy(x => new { x.ValueId, x.operation.Phase }))
            {
                var writers = group.Select(x => x.operation).ToArray();
                if (group.Key.Phase == FluxParticlePhase.Spawn && writers.Length != 1)
                {
                    throw new InvalidOperationException($"Persistent Value has multiple Spawn initializers: {group.Key.ValueId}");
                }
                for (var a = 0; a < writers.Length; a++)
                {
                    for (var b = a + 1; b < writers.Length; b++)
                    {
                        if (!DependsOn(writers[a].Id, writers[b].Id, lookup, new HashSet<string>(StringComparer.Ordinal)) &&
                            !DependsOn(writers[b].Id, writers[a].Id, lookup, new HashSet<string>(StringComparer.Ordinal)))
                        {
                            throw new InvalidOperationException($"Unordered writers: {group.Key.ValueId} ({writers[a].Id}, {writers[b].Id})");
                        }
                    }
                }
            }
        }

        static void ValidateProviderUsage(
            string valueId,
            FluxParticlePhase phase,
            FluxParticleDefinitionBuilder definitions,
            HashSet<string> visited)
        {
            if (!visited.Add(valueId)) return;
            if (!definitions.Providers.TryGetValue(valueId, out var provider))
            {
                // A Derived Provider may depend on a Persistent Value.
                if (!definitions.Operations.Values.Any(op => op.Writes.Any(write => write.ValueId == valueId)))
                {
                    throw new InvalidOperationException($"Unresolved provider input: {valueId}");
                }
                return;
            }
            if (provider.Kind == FluxParticleProviderKind.SpawnInput && phase != FluxParticlePhase.Spawn)
            {
                throw new InvalidOperationException($"Spawn-only provider used outside Spawn: {valueId}");
            }
            foreach (var parameterId in provider.Parameters)
            {
                if (definitions.Parameters[parameterId].Policy == FluxParticleParameterPolicy.SpawnLocked && phase != FluxParticlePhase.Spawn)
                {
                    throw new InvalidOperationException($"SpawnLocked parameter requires materialization: {parameterId}");
                }
            }
            foreach (var input in provider.Inputs) ValidateProviderUsage(input, phase, definitions, visited);
        }

        static List<FluxParticleValueDefinition> ResolvePersistentValues(
            FluxParticleDefinitionBuilder definitions,
            IReadOnlyList<FluxParticleOperationDefinition> operations)
        {
            var written = new HashSet<string>(operations.Where(op => op.Phase != FluxParticlePhase.Render)
                .SelectMany(op => op.Writes.Select(write => write.ValueId)), StringComparer.Ordinal);
            var initialized = new HashSet<string>(operations.Where(op => op.Phase == FluxParticlePhase.Spawn)
                .SelectMany(op => op.Writes.Select(write => write.ValueId)), StringComparer.Ordinal);

            foreach (var operation in operations.Where(op => op.Phase == FluxParticlePhase.Render))
            {
                foreach (var write in operation.Writes)
                {
                    if (written.Contains(write.ValueId))
                    {
                        throw new InvalidOperationException($"Render Operation cannot write Persistent State: {operation.Id}/{write.ValueId}");
                    }
                }
            }

            foreach (var valueId in written)
            {
                if (!initialized.Contains(valueId)) throw new InvalidOperationException($"Missing Spawn initializer: {valueId}");
                if (definitions.Providers.ContainsKey(valueId))
                {
                    throw new InvalidOperationException($"Value cannot have both State writer and Provider: {valueId}");
                }
            }

            foreach (var operation in operations)
            {
                foreach (var read in operation.Reads)
                {
                    if (read.Version != FluxParticleReadVersion.Provided && !written.Contains(read.ValueId))
                    {
                        var renderLocal = operation.Phase == FluxParticlePhase.Render && operations.Any(op =>
                            op.Phase == FluxParticlePhase.Render && op.Writes.Any(write => write.ValueId == read.ValueId));
                        if (!renderLocal) throw new InvalidOperationException($"Read has no Persistent Value: {operation.Id}/{read.ValueId}");
                    }
                }
            }

            return written.Select(id => definitions.Values[id]).OrderBy(value => value.Id, StringComparer.Ordinal).ToList();
        }

        sealed class PagePlan
        {
            public readonly HashSet<string> Values = new HashSet<string>(StringComparer.Ordinal);
            public int Width;
            public bool HasUpdate;
        }

        static List<PagePlan> BuildPagePlans(
            IReadOnlyList<FluxParticleValueDefinition> persistent,
            IReadOnlyList<FluxParticleOperationDefinition> operations)
        {
            // ARGBFloat represents all integers exactly up to 2^24. Unbounded UInt must not be packed silently.
            foreach (var value in persistent.Where(value => value.Type == FluxParticleValueType.UInt))
            {
                if (!value.MaximumUIntValue.HasValue || value.MaximumUIntValue.Value > 16777216u)
                {
                    throw new InvalidOperationException($"UInt State requires an exact float32 storage bound: {value.Id}");
                }
            }
            var types = persistent.ToDictionary(value => value.Id, value => value, StringComparer.Ordinal);
            var pages = new List<PagePlan>();

            // Each Update writer chain must remain on one Page in this first backend.
            // Different chains remain separate so their cross-Page dependencies can form GPU passes.
            foreach (var op in operations.Where(op => op.Phase == FluxParticlePhase.Update))
            {
                var writes = op.Writes.Select(write => write.ValueId).Distinct(StringComparer.Ordinal).ToArray();
                if (writes.Length == 0) continue;
                var affected = pages.Where(page => writes.Any(page.Values.Contains)).ToArray();
                var values = new HashSet<string>(writes, StringComparer.Ordinal);
                foreach (var page in affected) values.UnionWith(page.Values);
                var width = values.Sum(id => types[id].ComponentCount);
                if (width > 4) throw new InvalidOperationException($"Update operation cannot fit in one RGBA Page: {op.Id}");
                foreach (var page in affected) pages.Remove(page);
                var result = new PagePlan { HasUpdate = true, Width = width };
                result.Values.UnionWith(values);
                pages.Add(result);
            }

            // Spawn-only State can occupy unused channels in an Update Page without an extra GPU pass.
            foreach (var value in persistent.Where(value => pages.All(page => !page.Values.Contains(value.Id)))
                         .OrderByDescending(value => value.ComponentCount).ThenBy(value => value.Id, StringComparer.Ordinal))
            {
                var page = pages.Where(p => p.Width + value.ComponentCount <= 4)
                    .OrderByDescending(p => CountUpdateConsumers(value.Id, p, operations))
                    .ThenByDescending(p => p.HasUpdate)
                    .ThenBy(p => p.Width)
                    .FirstOrDefault();
                if (page == null)
                {
                    page = new PagePlan();
                    pages.Add(page);
                }
                page.Values.Add(value.Id);
                page.Width += value.ComponentCount;
            }

            return pages.OrderBy(p => p.Values.OrderBy(id => id, StringComparer.Ordinal).First(), StringComparer.Ordinal).ToList();
        }

        static int CountUpdateConsumers(string valueId, PagePlan page, IEnumerable<FluxParticleOperationDefinition> operations)
        {
            return operations.Count(op => op.Phase == FluxParticlePhase.Update &&
                op.Writes.Any(write => page.Values.Contains(write.ValueId)) &&
                op.Reads.Any(read => read.ValueId == valueId));
        }

        static FluxParticleStateLayout CreateLayout(
            IReadOnlyList<PagePlan> pages,
            IReadOnlyDictionary<string, FluxParticleValueDefinition> types)
        {
            var result = new List<FluxParticleStatePage>();
            for (var i = 0; i < pages.Count; i++)
            {
                var offset = 0;
                var bindings = new List<FluxParticleStateBinding>();
                foreach (var valueId in pages[i].Values.OrderByDescending(id => types[id].ComponentCount)
                             .ThenBy(id => id, StringComparer.Ordinal))
                {
                    var count = types[valueId].ComponentCount;
                    if (offset + count > 4) throw new InvalidOperationException($"Page overflow: {i}");
                    bindings.Add(new FluxParticleStateBinding(valueId, i, offset, count));
                    offset += count;
                }
                result.Add(new FluxParticleStatePage(i, bindings));
            }
            return new FluxParticleStateLayout(result);
        }

        static List<FluxParticleGpuPass> SchedulePasses(
            IReadOnlyList<PagePlan> pages,
            FluxParticleStateLayout layout,
            IReadOnlyList<FluxParticleOperationDefinition> operations,
            FluxParticleDefinitionBuilder definitions)
        {
            var lookup = operations.ToDictionary(op => op.Id, op => op, StringComparer.Ordinal);
            var writingPage = new Dictionary<string, int>(StringComparer.Ordinal);
            var groupByOperation = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var page in layout.Pages)
            {
                foreach (var binding in page.Bindings) writingPage.Add(binding.ValueId, page.Id);
            }
            // Update writers must target a single Page in this initial backend.
            // Spawn initialization is different: a single logical initializer may populate
            // several Pages, so its writes are projected into each Page's Spawn branch.
            foreach (var operation in operations.Where(op => op.Phase == FluxParticlePhase.Update && op.Writes.Count > 0))
            {
                var targets = operation.Writes.Select(write => writingPage[write.ValueId]).Distinct().ToArray();
                if (targets.Length != 1) throw new InvalidOperationException($"Update Operation writes multiple Pages: {operation.Id}");
                groupByOperation.Add(operation.Id, targets[0]);
            }

            var dependencies = new Dictionary<int, HashSet<int>>();
            foreach (var page in layout.Pages) dependencies.Add(page.Id, new HashSet<int>());
            foreach (var operation in operations.Where(op => op.Phase == FluxParticlePhase.Update && op.Writes.Count > 0))
            {
                var destination = groupByOperation[operation.Id];
                foreach (var dependency in operation.Dependencies)
                {
                    if (!groupByOperation.TryGetValue(dependency, out var source))
                    {
                        throw new InvalidOperationException($"Update dependency has no Page output: {dependency}");
                    }
                    if (source != destination) dependencies[destination].Add(source);
                }
                foreach (var read in operation.Reads.Where(read => read.Version == FluxParticleReadVersion.AfterOperation))
                {
                    var source = groupByOperation[read.ProducerOperationId];
                    if (source != destination) dependencies[destination].Add(source);
                }
            }

            // Cross-Page Spawn dependencies require explicit intermediate versions,
            // which Phase 1 does not provide. Reject rather than silently reorder.
            foreach (var operation in operations.Where(op => op.Phase == FluxParticlePhase.Spawn))
            {
                foreach (var dependency in operation.Dependencies)
                {
                    var source = operations.First(op => op.Id == dependency);
                    var sourcePages = source.Writes.Select(write => writingPage[write.ValueId]).Distinct();
                    var destinationPages = operation.Writes.Select(write => writingPage[write.ValueId]).Distinct();
                    if (sourcePages.Any(page => destinationPages.Any(target => target != page)))
                    {
                        throw new InvalidOperationException($"Cross-Page Spawn dependency is not supported: {operation.Id} -> {dependency}");
                    }
                }
            }

            var sortedPages = new List<int>();
            var marks = new Dictionary<int, int>();
            foreach (var page in pages.Select((value, id) => id)) VisitPage(page, dependencies, marks, sortedPages);

            var passes = new List<FluxParticleGpuPass>();
            foreach (var pageId in sortedPages)
            {
                var spawn = operations.Where(op => op.Phase == FluxParticlePhase.Spawn &&
                    op.Writes.Any(write => writingPage[write.ValueId] == pageId))
                    .Select(op => ProjectSpawnOperation(op, pageId, writingPage)).ToArray();
                var update = operations.Where(op => op.Phase == FluxParticlePhase.Update &&
                    op.Writes.Count > 0 && groupByOperation[op.Id] == pageId).ToArray();
                if (spawn.Length == 0 && update.Length == 0) continue;

                // A Blit must read its old Page from a different physical RT to carry forward unchanged channels/indices.
                var reads = new Dictionary<string, FluxParticlePageRead>(StringComparer.Ordinal);
                AddRead(reads, pageId, FluxParticlePageVersion.StepPrevious);
                foreach (var operation in spawn.Concat(update))
                {
                    foreach (var read in operation.Reads)
                    {
                        if (read.Version == FluxParticleReadVersion.Provided)
                        {
                            AddProviderReads(read.ValueId, operation.Phase, definitions, writingPage, reads,
                                new HashSet<string>(StringComparer.Ordinal));
                            continue;
                        }
                        if (!writingPage.TryGetValue(read.ValueId, out var sourcePage))
                        {
                            throw new InvalidOperationException($"Unbound GPU input: {operation.Id}/{read.ValueId}");
                        }
                        if (read.Version == FluxParticleReadVersion.AfterOperation)
                        {
                            if (sourcePage != pageId) AddRead(reads, sourcePage, FluxParticlePageVersion.Updated);
                        }
                        else
                        {
                            AddRead(reads, sourcePage, FluxParticlePageVersion.StepPrevious);
                        }
                    }
                }

                passes.Add(new FluxParticleGpuPass(
                    passes.Count,
                    pageId,
                    update.Length == 0 ? FluxParticlePassCondition.HasSpawn : FluxParticlePassCondition.Always,
                    spawn,
                    update,
                    reads.Values.OrderBy(read => read.PageId).ThenBy(read => read.Version)));
            }
            return passes;
        }

        /// <summary>
        /// A Spawn Operation may initialize Values stored on multiple Pages.
        /// Each physical Pass receives only the writes belonging to its destination Page,
        /// while retaining the logical Operation's reads and shader implementation.
        /// </summary>
        static FluxParticleOperationDefinition ProjectSpawnOperation(
            FluxParticleOperationDefinition operation,
            int pageId,
            IReadOnlyDictionary<string, int> writingPage)
        {
            var writes = operation.Writes.Where(write => writingPage[write.ValueId] == pageId).ToArray();
            if (writes.Length == operation.Writes.Count) return operation;

            return new FluxParticleOperationDefinition(
                operation.Id,
                operation.OwnerId,
                operation.Phase,
                operation.Reads,
                writes,
                operation.Dependencies,
                operation.HlslPath,
                operation.EntryPoint,
                operation.Parameters,
                operation.EnabledParameterId);
        }

        static void AddProviderReads(
            string valueId,
            FluxParticlePhase phase,
            FluxParticleDefinitionBuilder definitions,
            IReadOnlyDictionary<string, int> writingPage,
            Dictionary<string, FluxParticlePageRead> reads,
            HashSet<string> visited)
        {
            if (!visited.Add(valueId)) return;
            if (!definitions.Providers.TryGetValue(valueId, out var provider))
            {
                if (!writingPage.TryGetValue(valueId, out var pageId))
                {
                    throw new InvalidOperationException($"Provider requires unavailable State: {valueId}");
                }
                if (phase == FluxParticlePhase.Spawn)
                {
                    throw new InvalidOperationException($"Spawn Provider requires State before initialization: {valueId}");
                }
                AddRead(reads, pageId, FluxParticlePageVersion.StepPrevious);
                return;
            }
            foreach (var input in provider.Inputs)
            {
                AddProviderReads(input, phase, definitions, writingPage, reads, visited);
            }
        }

        static void AddRead(Dictionary<string, FluxParticlePageRead> reads, int pageId, FluxParticlePageVersion version)
        {
            var id = pageId + ":" + (int)version;
            if (!reads.ContainsKey(id)) reads.Add(id, new FluxParticlePageRead(pageId, version));
        }

        static void VisitPage(
            int pageId,
            Dictionary<int, HashSet<int>> dependencies,
            Dictionary<int, int> marks,
            List<int> sorted)
        {
            if (marks.TryGetValue(pageId, out var mark))
            {
                if (mark == 1) throw new InvalidOperationException($"Layout requires multiple updates of a Page in one Step: {pageId}");
                if (mark == 2) return;
            }
            marks[pageId] = 1;
            foreach (var dependency in dependencies[pageId].OrderBy(id => id)) VisitPage(dependency, dependencies, marks, sorted);
            marks[pageId] = 2;
            sorted.Add(pageId);
        }
    }
}
