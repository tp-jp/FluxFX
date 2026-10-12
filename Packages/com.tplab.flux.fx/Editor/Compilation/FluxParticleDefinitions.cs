using System;
using System.Collections.Generic;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// コンパイル時に扱う論理値の型です。UIntは物理Textureへの格納形式を意味しません。
    /// </summary>
    public enum FluxParticleValueType
    {
        Float = 1,
        Float2 = 2,
        Float3 = 3,
        Float4 = 4,
        UInt = 5
    }

    /// <summary>
    /// 論理値への書き込みを許可する対象です。
    /// </summary>
    public enum FluxParticleWritePolicy
    {
        SystemManaged,
        SharedMutable,
        OwnerManaged,
        ReadOnly
    }

    /// <summary>
    /// Operationの実行対象となる粒子の状態です。
    /// </summary>
    public enum FluxParticlePhase
    {
        Spawn,
        Update,
        Render
    }

    /// <summary>
    /// Valueの書き込み方法です。
    /// </summary>
    public enum FluxParticleWriteKind
    {
        Initialize,
        Replace,
        Modify
    }

    /// <summary>
    /// Valueの参照時点です。
    /// </summary>
    public enum FluxParticleReadVersion
    {
        StepPrevious,
        AfterOperation,
        Committed,
        Provided
    }

    /// <summary>
    /// Parameterを実行中に変更できるかどうかを表します。
    /// </summary>
    public enum FluxParticleParameterPolicy
    {
        Static,
        Live,
        SpawnLocked
    }

    /// <summary>
    /// 非永続Valueを供給する方法です。
    /// </summary>
    public enum FluxParticleProviderKind
    {
        Uniform,
        SpawnInput,
        Derived
    }

    /// <summary>
    /// 論理値の識別子、型、書き込み権限を定義します。
    /// </summary>
    public sealed class FluxParticleValueDefinition
    {
        public string Id { get; }
        public FluxParticleValueType Type { get; }
        public FluxParticleWritePolicy WritePolicy { get; }
        public string OwnerId { get; }
        public uint? MaximumUIntValue { get; }

        public int ComponentCount => Type == FluxParticleValueType.UInt ? 1 : (int)Type;

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
        public string Id { get; }
        public FluxParticleValueType Type { get; }
        public FluxParticleParameterPolicy Policy { get; }

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
        public string ValueId { get; }
        public FluxParticleProviderKind Kind { get; }
        public IReadOnlyList<string> Inputs { get; }
        public IReadOnlyList<string> Parameters { get; }
        public string HlslPath { get; }
        public string EntryPoint { get; }

        public FluxParticleValueProvider(
            string valueId,
            FluxParticleProviderKind kind,
            IEnumerable<string> inputs = null,
            IEnumerable<string> parameters = null,
            string hlslPath = null,
            string entryPoint = null)
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
        }
    }

    /// <summary>
    /// Operationが読み取るValueと必要な状態バージョンです。
    /// </summary>
    public sealed class FluxParticleValueRead
    {
        public string ValueId { get; }
        public FluxParticleReadVersion Version { get; }
        public string ProducerOperationId { get; }

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
        public string ValueId { get; }
        public FluxParticleWriteKind Kind { get; }

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
        public string Id { get; }
        public string OwnerId { get; }
        public FluxParticlePhase Phase { get; }
        public IReadOnlyList<FluxParticleValueRead> Reads { get; }
        public IReadOnlyList<FluxParticleValueWrite> Writes { get; }
        public IReadOnlyList<string> Dependencies { get; }
        public string HlslPath { get; }
        public string EntryPoint { get; }

        public FluxParticleOperationDefinition(
            string id,
            string ownerId,
            FluxParticlePhase phase,
            IEnumerable<FluxParticleValueRead> reads,
            IEnumerable<FluxParticleValueWrite> writes,
            IEnumerable<string> dependencies,
            string hlslPath,
            string entryPoint)
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
        }
    }
}
