using System;
using System.Collections.Generic;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// コンパイル時に扱う論理値の型です。UIntは物理Textureへの格納形式を意味しません。
    /// </summary>
    public enum FluxParticleValueType
    {
        /// <summary>
        /// 1成分の浮動小数点です。
        /// </summary>
        Float = 1,

        /// <summary>
        /// 2成分の浮動小数点です。
        /// </summary>
        Float2 = 2,

        /// <summary>
        /// 3成分の浮動小数点です。
        /// </summary>
        Float3 = 3,

        /// <summary>
        /// 4成分の浮動小数点です。
        /// </summary>
        Float4 = 4,

        /// <summary>
        /// 符号なし整数です。RTへの格納形式とは独立します。
        /// </summary>
        UInt = 5
    }

    /// <summary>
    /// 論理値への書き込みを許可する対象です。
    /// </summary>
    public enum FluxParticleWritePolicy
    {
        /// <summary>
        /// Core側Operationのみが書き込み可能です。
        /// </summary>
        SystemManaged,

        /// <summary>
        /// 複数のOperationで更新可能です。
        /// </summary>
        SharedMutable,

        /// <summary>
        /// Value所有者だけが更新できます。
        /// </summary>
        OwnerManaged,

        /// <summary>
        /// Operationによる書き込みは禁止です。
        /// </summary>
        ReadOnly
    }

    /// <summary>
    /// Operationの実行対象となる粒子の状態です。
    /// </summary>
    public enum FluxParticlePhase
    {
        /// <summary>
        /// 新規Spawn粒子に適用します。
        /// </summary>
        Spawn,

        /// <summary>
        /// 既存の生存粒子に適用します。
        /// </summary>
        Update,

        /// <summary>
        /// 描画評価に適用します。
        /// </summary>
        Render
    }

    /// <summary>
    /// Valueの書き込み方法です。
    /// </summary>
    public enum FluxParticleWriteKind
    {
        /// <summary>
        /// Spawn時に初期化します。
        /// </summary>
        Initialize,

        /// <summary>
        /// 既存値を置換します。
        /// </summary>
        Replace,

        /// <summary>
        /// 現在値に基づいて更新します。
        /// </summary>
        Modify
    }

    /// <summary>
    /// Valueの参照時点です。
    /// </summary>
    public enum FluxParticleReadVersion
    {
        /// <summary>
        /// Step開始時の値を参照します。
        /// </summary>
        StepPrevious,

        /// <summary>
        /// 指定したOperationの結果を参照します。
        /// </summary>
        AfterOperation,

        /// <summary>
        /// Simulation後の確定値を参照します。
        /// </summary>
        Committed,

        /// <summary>
        /// Value Providerが供給する値を参照します。
        /// </summary>
        Provided
    }

    /// <summary>
    /// Parameterを実行中に変更できるかどうかを表します。
    /// </summary>
    public enum FluxParticleParameterPolicy
    {
        /// <summary>
        /// 実行中変更しないParameterです。
        /// </summary>
        Static,

        /// <summary>
        /// 変更後の値が既存粒子にも反映されます。
        /// </summary>
        Live,

        /// <summary>
        /// Spawn時に確定した評価結果を維持します。
        /// </summary>
        SpawnLocked
    }

    /// <summary>
    /// 非永続Valueを供給する方法です。
    /// </summary>
    public enum FluxParticleProviderKind
    {
        /// <summary>
        /// 共有Shader Property等から供給します。
        /// </summary>
        Uniform,

        /// <summary>
        /// Spawn Contextから供給します。
        /// </summary>
        SpawnInput,

        /// <summary>
        /// 入力Value・Parameterから計算します。
        /// </summary>
        Derived,

        /// <summary>
        /// Render Phaseで固定LUTを線形補間して取得します。
        /// </summary>
        LookupTable
    }

    /// <summary>
    /// 論理値の識別子、型、書き込み権限を定義します。
    /// </summary>
    public sealed class FluxParticleValueDefinition
    {
        /// <summary>
        /// 論理ValueのIDです。物理RTやChannelには依存しません。
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// RT形式と独立したValueの論理型です。
        /// </summary>
        public FluxParticleValueType Type { get; }

        /// <summary>
        /// Valueへの書き込みを許可する対象の制約です。
        /// </summary>
        public FluxParticleWritePolicy WritePolicy { get; }

        /// <summary>
        /// Valueを所有するCoreまたはModuleのIDです。
        /// </summary>
        public string OwnerId { get; }

        /// <summary>
        /// UIntの保証された最大値です。float Channelの整数精度の検証に使用します。
        /// </summary>
        public uint? MaximumUIntValue { get; }

        /// <summary>
        /// 必要なChannel成分数です。UIntは1成分として扱います。
        /// </summary>
        public int ComponentCount => Type == FluxParticleValueType.UInt ? 1 : (int)Type;

        /// <summary>
        /// 論理Valueの型・所有者・書き込み契約を設定します。
        /// </summary>
        /// <param name="id">識別子。</param>
        /// <param name="type">論理型。</param>
        /// <param name="writePolicy">書き込み権限。</param>
        /// <param name="ownerId">定義の所有者ID。</param>
        /// <param name="maximumUIntValue">UIntの最大値。</param>
        public FluxParticleValueDefinition(
            string id,
            FluxParticleValueType type,
            FluxParticleWritePolicy writePolicy,
            string ownerId,
            uint? maximumUIntValue = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Value ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner ID is required.", nameof(ownerId));
            if (!Enum.IsDefined(typeof(FluxParticleValueType), type)) throw new ArgumentOutOfRangeException(nameof(type));
            if (!Enum.IsDefined(typeof(FluxParticleWritePolicy), writePolicy)) throw new ArgumentOutOfRangeException(nameof(writePolicy));
            if (type != FluxParticleValueType.UInt && maximumUIntValue.HasValue)
            {
                throw new ArgumentException("Only UInt Values may specify MaximumUIntValue.", nameof(maximumUIntValue));
            }

            Id = id;
            Type = type;
            WritePolicy = writePolicy;
            OwnerId = ownerId;
            MaximumUIntValue = maximumUIntValue;
        }
    }

    /// <summary>
    /// Shaderへ渡すModule Parameterと変更契約です。
    /// </summary>
    public sealed class FluxParticleParameterDefinition
    {
        /// <summary>
        /// Parameterの一意なIDです。
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Parameterの論理型です。
        /// </summary>
        public FluxParticleValueType Type { get; }

        /// <summary>
        /// 実行中の値変更とSpawn時固定の契約です。
        /// </summary>
        public FluxParticleParameterPolicy Policy { get; }

        /// <summary>
        /// Shader Parameterとその変更契約を設定します。
        /// </summary>
        /// <param name="id">識別子。</param>
        /// <param name="type">論理型。</param>
        /// <param name="policy">Parameter変更契約。</param>
        public FluxParticleParameterDefinition(string id, FluxParticleValueType type, FluxParticleParameterPolicy policy)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Parameter ID is required.", nameof(id));
            if (!Enum.IsDefined(typeof(FluxParticleValueType), type)) throw new ArgumentOutOfRangeException(nameof(type));
            if (!Enum.IsDefined(typeof(FluxParticleParameterPolicy), policy)) throw new ArgumentOutOfRangeException(nameof(policy));

            Id = id;
            Type = type;
            Policy = policy;
        }
    }

    /// <summary>
    /// 論理ValueをRT以外から取得するための宣言です。
    /// HLSL実装を持つ場合はここだけに記録します。
    /// </summary>
    public sealed class FluxParticleValueProvider
    {
        /// <summary>
        /// このProviderが供給する論理ValueのIDです。
        /// </summary>
        public string ValueId { get; }

        /// <summary>
        /// Valueの供給方法
        /// </summary>
        public FluxParticleProviderKind Kind { get; }

        /// <summary>
        /// 評価で参照するValueのID一覧
        /// </summary>
        public IReadOnlyList<string> Inputs { get; }

        /// <summary>
        /// 評価に使うShader ParameterのID一覧です。
        /// </summary>
        public IReadOnlyList<string> Parameters { get; }

        /// <summary>
        /// Derived評価に使うHLSLファイルのパスです。
        /// </summary>
        public string HlslPath { get; }

        /// <summary>
        /// Derived評価に使うHLSL関数名です。
        /// </summary>
        public string EntryPoint { get; }

        /// <summary>
        /// 0から1の区間に等間隔で配置した固定LUTのサンプル
        /// </summary>
        public IReadOnlyList<float> LookupSamples { get; }

        /// <summary>
        /// Valueの供給方法と評価実装を設定します。
        /// </summary>
        /// <param name="valueId">論理ValueのID</param>
        /// <param name="kind">供給方式または書き込み方式</param>
        /// <param name="inputs">入力ValueのID一覧</param>
        /// <param name="parameters">使用ParameterのID一覧</param>
        /// <param name="hlslPath">HLSLファイルのパス</param>
        /// <param name="entryPoint">HLSL関数名</param>
        /// <param name="lookupSamples">0から1の区間に等間隔で配置した固定LUTのサンプル</param>
        public FluxParticleValueProvider(
            string valueId,
            FluxParticleProviderKind kind,
            IEnumerable<string> inputs = null,
            IEnumerable<string> parameters = null,
            string hlslPath = null,
            string entryPoint = null,
            IEnumerable<float> lookupSamples = null)
        {
            if (string.IsNullOrWhiteSpace(valueId)) throw new ArgumentException("Value ID is required.", nameof(valueId));
            if (!Enum.IsDefined(typeof(FluxParticleProviderKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == FluxParticleProviderKind.Derived && (string.IsNullOrWhiteSpace(hlslPath) || string.IsNullOrWhiteSpace(entryPoint)))
            {
                throw new ArgumentException("Derived providers require an HLSL implementation.");
            }

            ValueId = valueId;
            Kind = kind;
            Inputs = new List<string>(inputs ?? Array.Empty<string>()).AsReadOnly();
            Parameters = new List<string>(parameters ?? Array.Empty<string>()).AsReadOnly();
            HlslPath = hlslPath;
            EntryPoint = entryPoint;
            LookupSamples = new List<float>(lookupSamples ?? Array.Empty<float>()).AsReadOnly();
            if (kind == FluxParticleProviderKind.LookupTable)
            {
                if (Inputs.Count != 1 || Parameters.Count != 0 || LookupSamples.Count < 2 || LookupSamples.Count > 4096)
                {
                    throw new ArgumentException("Lookup providers require one Float input and 2 to 4096 samples.");
                }
                foreach (var sample in LookupSamples)
                {
                    if (float.IsNaN(sample) || float.IsInfinity(sample))
                    {
                        throw new ArgumentException("Lookup samples must be finite.");
                    }
                }
            }
            else if (LookupSamples.Count != 0)
            {
                throw new ArgumentException("Only LookupTable providers may declare lookup samples.");
            }
        }
    }

    /// <summary>
    /// Operationが読み取るValueと必要な状態バージョンです。
    /// </summary>
    public sealed class FluxParticleValueRead
    {
        /// <summary>
        /// 読み取り対象の論理Value IDです。
        /// </summary>
        public string ValueId { get; }

        /// <summary>
        /// 必要なValueの参照時点または供給方法です。
        /// </summary>
        public FluxParticleReadVersion Version { get; }

        /// <summary>
        /// AfterOperationの場合の参照元Operation IDです。
        /// </summary>
        public string ProducerOperationId { get; }

        /// <summary>
        /// Operationが参照するValueとバージョンを設定します。
        /// </summary>
        /// <param name="valueId">論理ValueのID。</param>
        /// <param name="version">参照バージョン。</param>
        /// <param name="producerOperationId">参照元OperationのID。</param>
        public FluxParticleValueRead(string valueId, FluxParticleReadVersion version, string producerOperationId = null)
        {
            if (string.IsNullOrWhiteSpace(valueId)) throw new ArgumentException("Value ID is required.", nameof(valueId));
            if (!Enum.IsDefined(typeof(FluxParticleReadVersion), version)) throw new ArgumentOutOfRangeException(nameof(version));
            if (version == FluxParticleReadVersion.AfterOperation && string.IsNullOrWhiteSpace(producerOperationId))
            {
                throw new ArgumentException("AfterOperation requires a producer ID.", nameof(producerOperationId));
            }
            if (version != FluxParticleReadVersion.AfterOperation && producerOperationId != null)
            {
                throw new ArgumentException("Producer ID is only valid for AfterOperation.", nameof(producerOperationId));
            }

            ValueId = valueId;
            Version = version;
            ProducerOperationId = producerOperationId;
        }
    }

    /// <summary>
    /// Operationが書き換える論理Valueとその方法です。
    /// </summary>
    public sealed class FluxParticleValueWrite
    {
        /// <summary>
        /// 書き込み対象のValue IDです。
        /// </summary>
        public string ValueId { get; }

        /// <summary>
        /// 初期化・置換・更新の方式です。
        /// </summary>
        public FluxParticleWriteKind Kind { get; }

        /// <summary>
        /// Operationの書き込みValueと方式を設定します。
        /// </summary>
        /// <param name="valueId">論理ValueのID。</param>
        /// <param name="kind">供給方式または書き込み方式。</param>
        public FluxParticleValueWrite(string valueId, FluxParticleWriteKind kind)
        {
            if (string.IsNullOrWhiteSpace(valueId)) throw new ArgumentException("Value ID is required.", nameof(valueId));
            if (!Enum.IsDefined(typeof(FluxParticleWriteKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            ValueId = valueId;
            Kind = kind;
        }
    }

    /// <summary>
    /// Operation単位の宣言です。物理RTとGPU Passは指定しません。
    /// </summary>
    public sealed class FluxParticleOperationDefinition
    {
        /// <summary>
        /// Operationの一意なIDです。
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Operationを提供するCoreまたはModuleのIDです。
        /// </summary>
        public string OwnerId { get; }

        /// <summary>
        /// Spawn・Update・Renderの実行区分です。
        /// </summary>
        public FluxParticlePhase Phase { get; }

        /// <summary>
        /// 参照するValueとその時点の契約です。
        /// </summary>
        public IReadOnlyList<FluxParticleValueRead> Reads { get; }

        /// <summary>
        /// 更新するValueと書き込み種別の一覧です。
        /// </summary>
        public IReadOnlyList<FluxParticleValueWrite> Writes { get; }

        /// <summary>
        /// 同じPhaseで先行する必要があるOperation IDの一覧です。
        /// </summary>
        public IReadOnlyList<string> Dependencies { get; }

        /// <summary>
        /// OperationのHLSL実装ファイルのパスです。
        /// </summary>
        public string HlslPath { get; }

        /// <summary>
        /// HLSLの呼び出し対象関数名です。
        /// </summary>
        public string EntryPoint { get; }

        /// <summary>
        /// OperationのContextへ供給するParameter IDです。HLSLから直接Uniformを参照する必要はありません。
        /// </summary>
        public IReadOnlyList<string> Parameters { get; }

        /// <summary>
        /// 0なら実行を省略するFloat Parameter IDです。nullなら常に実行します。
        /// 無効時は明示的に読み取った更新前の値を引き継ぎます。
        /// </summary>
        public string EnabledParameterId { get; }

        /// <summary>
        /// Operationの入出力・依存関係・HLSL実装を設定します。
        /// </summary>
        /// <param name="id">識別子。</param>
        /// <param name="ownerId">定義の所有者ID。</param>
        /// <param name="phase">Operation実行Phase。</param>
        /// <param name="reads">Read契約。</param>
        /// <param name="writes">Write契約。</param>
        /// <param name="dependencies">先行OperationのID一覧。</param>
        /// <param name="hlslPath">HLSLファイルのパス。</param>
        /// <param name="entryPoint">HLSL関数名。</param>
        /// <param name="parameters">Contextに供給するParameter ID一覧。</param>
        /// <param name="enabledParameterId">実行条件となるFloat Parameter ID。</param>
        public FluxParticleOperationDefinition(
            string id,
            string ownerId,
            FluxParticlePhase phase,
            IEnumerable<FluxParticleValueRead> reads,
            IEnumerable<FluxParticleValueWrite> writes,
            IEnumerable<string> dependencies,
            string hlslPath,
            string entryPoint,
            IEnumerable<string> parameters = null,
            string enabledParameterId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Operation ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner ID is required.", nameof(ownerId));
            if (!Enum.IsDefined(typeof(FluxParticlePhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            if (string.IsNullOrWhiteSpace(hlslPath)) throw new ArgumentException("HLSL path is required.", nameof(hlslPath));
            if (string.IsNullOrWhiteSpace(entryPoint)) throw new ArgumentException("HLSL entry point is required.", nameof(entryPoint));

            Id = id;
            OwnerId = ownerId;
            Phase = phase;
            Reads = new List<FluxParticleValueRead>(reads ?? Array.Empty<FluxParticleValueRead>()).AsReadOnly();
            Writes = new List<FluxParticleValueWrite>(writes ?? Array.Empty<FluxParticleValueWrite>()).AsReadOnly();
            Dependencies = new List<string>(dependencies ?? Array.Empty<string>()).AsReadOnly();
            HlslPath = hlslPath.Replace('\\', '/');
            EntryPoint = entryPoint;
            Parameters = new List<string>(parameters ?? Array.Empty<string>()).AsReadOnly();
            EnabledParameterId = enabledParameterId;
        }
    }
}
