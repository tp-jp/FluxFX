using System;
using System.Collections.Generic;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// 初期Backendで対応するState Pageの保存形式です。
    /// </summary>
    public enum FluxParticleStatePageFormat
    {
        ArgbFloat
    }

    /// <summary>
    /// RT上の論理的な1ページです。RTインスタンス数はExecutionPlanが管理します。
    /// </summary>
    public sealed class FluxParticleStatePage
    {
        public int Id { get; }
        public FluxParticleStatePageFormat Format { get; }
        public IReadOnlyList<FluxParticleStateBinding> Bindings { get; }

        internal FluxParticleStatePage(int id, IEnumerable<FluxParticleStateBinding> bindings)
        {
            Id = id;
            Format = FluxParticleStatePageFormat.ArgbFloat;
            Bindings = new List<FluxParticleStateBinding>(bindings).AsReadOnly();
        }
    }

    /// <summary>
    /// Persistent Valueの物理ページとRGBAチャンネルの配置結果です。
    /// </summary>
    public sealed class FluxParticleStateBinding
    {
        public string ValueId { get; }
        public int PageId { get; }
        public int ChannelOffset { get; }
        public int ComponentCount { get; }

        internal FluxParticleStateBinding(string valueId, int pageId, int channelOffset, int componentCount)
        {
            ValueId = valueId;
            PageId = pageId;
            ChannelOffset = channelOffset;
            ComponentCount = componentCount;
        }
    }

    /// <summary>
    /// フレーム間保持が必要なValueだけを格納したRTレイアウトです。
    /// </summary>
    public sealed class FluxParticleStateLayout
    {
        readonly Dictionary<string, FluxParticleStateBinding> _bindings;

        public IReadOnlyList<FluxParticleStatePage> Pages { get; }

        internal FluxParticleStateLayout(IEnumerable<FluxParticleStatePage> pages)
        {
            Pages = new List<FluxParticleStatePage>(pages).AsReadOnly();
            _bindings = new Dictionary<string, FluxParticleStateBinding>(StringComparer.Ordinal);

            foreach (var page in Pages)
            {
                foreach (var binding in page.Bindings)
                {
                    _bindings.Add(binding.ValueId, binding);
                }
            }
        }

        /// <summary>
        /// 指定したValueがPersistent Stateとして配置されているか調べます。
        /// </summary>
        public bool TryGetBinding(string valueId, out FluxParticleStateBinding binding)
        {
            return _bindings.TryGetValue(valueId, out binding);
        }
    }

    /// <summary>
    /// GPU Passの実行条件です。
    /// </summary>
    public enum FluxParticlePassCondition
    {
        Always,
        HasSpawn
    }

    /// <summary>
    /// GPU Passが参照するState Pageとそのタイミングです。
    /// </summary>
    public enum FluxParticlePageVersion
    {
        StepPrevious,
        Updated
    }

    /// <summary>
    /// GPU Passの入力ページへの参照です。
    /// </summary>
    public sealed class FluxParticlePageRead
    {
        public int PageId { get; }
        public FluxParticlePageVersion Version { get; }

        internal FluxParticlePageRead(int pageId, FluxParticlePageVersion version)
        {
            PageId = pageId;
            Version = version;
        }
    }

    /// <summary>
    /// 同じState Pageを更新するOperationと入力Snapshotの計画です。
    /// </summary>
    public sealed class FluxParticleGpuPass
    {
        public int Index { get; }
        public int OutputPageId { get; }
        public FluxParticlePassCondition Condition { get; }
        public IReadOnlyList<FluxParticleOperationDefinition> SpawnOperations { get; }
        public IReadOnlyList<FluxParticleOperationDefinition> UpdateOperations { get; }
        public IReadOnlyList<FluxParticlePageRead> Reads { get; }

        internal FluxParticleGpuPass(
            int index,
            int outputPageId,
            FluxParticlePassCondition condition,
            IEnumerable<FluxParticleOperationDefinition> spawnOperations,
            IEnumerable<FluxParticleOperationDefinition> updateOperations,
            IEnumerable<FluxParticlePageRead> reads)
        {
            Index = index;
            OutputPageId = outputPageId;
            Condition = condition;
            SpawnOperations = new List<FluxParticleOperationDefinition>(spawnOperations).AsReadOnly();
            UpdateOperations = new List<FluxParticleOperationDefinition>(updateOperations).AsReadOnly();
            Reads = new List<FluxParticlePageRead>(reads).AsReadOnly();
        }
    }

    /// <summary>
    /// 物理Buffer割り当て前の単一Update/Spawn Step用GPU Pass計画です。
    /// </summary>
    public sealed class FluxParticleExecutionPlan
    {
        public IReadOnlyList<FluxParticleGpuPass> Passes { get; }

        internal FluxParticleExecutionPlan(IEnumerable<FluxParticleGpuPass> passes)
        {
            Passes = new List<FluxParticleGpuPass>(passes).AsReadOnly();
        }
    }

    /// <summary>
    /// Logical Compilationと初期Physical Planningの検証済み結果です。
    /// Shader生成・Runtime適用はまだこの型には含めません。
    /// </summary>
    public sealed class FluxParticleCompiledProgram
    {
        public FluxParticleStateLayout StateLayout { get; }
        public FluxParticleExecutionPlan ExecutionPlan { get; }
        public IReadOnlyList<FluxParticleOperationDefinition> Operations { get; }

        internal FluxParticleCompiledProgram(
            FluxParticleStateLayout stateLayout,
            FluxParticleExecutionPlan executionPlan,
            IEnumerable<FluxParticleOperationDefinition> operations)
        {
            StateLayout = stateLayout;
            ExecutionPlan = executionPlan;
            Operations = new List<FluxParticleOperationDefinition>(operations).AsReadOnly();
        }
    }
}
