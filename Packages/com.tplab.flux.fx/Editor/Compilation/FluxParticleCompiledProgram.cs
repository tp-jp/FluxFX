using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// 初期Backendで対応するState Pageの保存形式です。
    /// </summary>
    public enum FluxParticleStatePageFormat
    {
        /// <summary>
        /// RGBA各Channelを32bit浮動小数点で保持する形式です。
        /// </summary>
        ArgbFloat
    }

    /// <summary>
    /// RT上の論理的な1ページです。RTインスタンス数はExecutionPlanが管理します。
    /// </summary>
    public sealed class FluxParticleStatePage
    {
        /// <summary>
        /// 論理State Pageの識別番号です。物理RTの番号ではありません。
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// PageのRT保存形式です。
        /// </summary>
        public FluxParticleStatePageFormat Format { get; }

        /// <summary>
        /// このPageに配置されたPersistent ValueとRGBA Channelの対応一覧です。
        /// </summary>
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
        /// <summary>
        /// 配置された論理ValueのIDです。
        /// </summary>
        public string ValueId { get; }

        /// <summary>
        /// 格納先の論理Page IDです。
        /// </summary>
        public int PageId { get; }

        /// <summary>
        /// 格納を開始するRGBA ChannelのOffsetです（R=0、G=1、B=2、A=3）。
        /// </summary>
        public int ChannelOffset { get; }

        /// <summary>
        /// 連続して使用するChannel数です。
        /// </summary>
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

        /// <summary>
        /// Persistent Stateを保持する論理Pageの一覧です。
        /// </summary>
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
        /// <param name="valueId">検索対象の論理Value ID。</param>
        /// <param name="binding">見つかったState Binding。存在しない場合はnull。</param>
        /// <returns>Persistent Stateとして配置されていればtrue。</returns>
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
        /// <summary>
        /// 毎Simulation Step実行します。
        /// </summary>
        Always,

        /// <summary>
        /// Spawn発生時のみ実行します。
        /// </summary>
        HasSpawn
    }

    /// <summary>
    /// GPU Passが参照するState Pageとそのタイミングです。
    /// </summary>
    public enum FluxParticlePageVersion
    {
        /// <summary>
        /// Step開始時の確定状態です。
        /// </summary>
        StepPrevious,

        /// <summary>
        /// 先行Passが更新した状態です。
        /// </summary>
        Updated
    }

    /// <summary>
    /// GPU Passの入力ページへの参照です。
    /// </summary>
    public sealed class FluxParticlePageRead
    {
        /// <summary>
        /// 入力となる論理PageのIDです。
        /// </summary>
        public int PageId { get; }

        /// <summary>
        /// 読み取り対象となるPageのバージョンです。
        /// </summary>
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
        /// <summary>
        /// ExecutionPlan内のPass実行順序です。
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// このPassの書き込み先Page IDです。
        /// </summary>
        public int OutputPageId { get; }

        /// <summary>
        /// Passの実行条件です。
        /// </summary>
        public FluxParticlePassCondition Condition { get; }

        /// <summary>
        /// Spawn対象のParticleに適用するOperationです。
        /// </summary>
        public IReadOnlyList<FluxParticleOperationDefinition> SpawnOperations { get; }

        /// <summary>
        /// 既存の生存Particleに適用するOperationです。
        /// </summary>
        public IReadOnlyList<FluxParticleOperationDefinition> UpdateOperations { get; }

        /// <summary>
        /// 入力として参照するPageとバージョンの一覧です。
        /// </summary>
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
        /// <summary>
        /// 依存関係を解決したGPU Passを実行順に保持します。
        /// </summary>
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
        /// <summary>
        /// Persistent Stateの物理配置を表します。
        /// </summary>
        public FluxParticleStateLayout StateLayout { get; }

        /// <summary>
        /// コンパイル時点で確定したValue定義です。
        /// </summary>
        public IReadOnlyDictionary<string, FluxParticleValueDefinition> Values { get; }

        /// <summary>
        /// コンパイル時点で確定したProvider定義です。
        /// </summary>
        public IReadOnlyDictionary<string, FluxParticleValueProvider> Providers { get; }

        /// <summary>
        /// コンパイル時点で確定したParameter定義です。
        /// </summary>
        public IReadOnlyDictionary<string, FluxParticleParameterDefinition> Parameters { get; }

        /// <summary>
        /// GPU Passとその入力・出力の実行計画です。
        /// </summary>
        public FluxParticleExecutionPlan ExecutionPlan { get; }

        /// <summary>
        /// 依存関係の解決を終えたOperation定義の一覧です。
        /// </summary>
        public IReadOnlyList<FluxParticleOperationDefinition> Operations { get; }

        internal FluxParticleCompiledProgram(
            FluxParticleStateLayout stateLayout,
            FluxParticleExecutionPlan executionPlan,
            IEnumerable<FluxParticleOperationDefinition> operations,
            FluxParticleDefinitionBuilder definitions)
        {
            Values = new ReadOnlyDictionary<string, FluxParticleValueDefinition>(
                definitions.Values.Values.ToDictionary(value => value.Id, StringComparer.Ordinal));
            Providers = new ReadOnlyDictionary<string, FluxParticleValueProvider>(
                definitions.Providers.Values.ToDictionary(provider => provider.ValueId, StringComparer.Ordinal));
            Parameters = new ReadOnlyDictionary<string, FluxParticleParameterDefinition>(
                definitions.Parameters.Values.ToDictionary(parameter => parameter.Id, StringComparer.Ordinal));
            StateLayout = stateLayout;
            ExecutionPlan = executionPlan;
            Operations = new List<FluxParticleOperationDefinition>(operations).AsReadOnly();
        }
    }
}
