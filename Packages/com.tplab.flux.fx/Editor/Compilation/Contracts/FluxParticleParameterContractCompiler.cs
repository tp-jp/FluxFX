using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon;
using UnityEngine;
using ContractPolicy = TpLab.Flux.FX.Editor.Contracts.FluxParticleParameterPolicy;
using CompiledPolicy = TpLab.Flux.FX.Editor.Compilation.Definitions.FluxParticleParameterPolicy;

namespace TpLab.Flux.FX.Editor.Compilation.Contracts
{
    /// <summary>
    /// Parameter契約をShader用定義と格納値へ変換します。
    /// </summary>
    public static class FluxParticleParameterContractCompiler
    {
        /// <summary>
        /// 公開型と変更PolicyをShader用定義へ変換します。
        /// </summary>
        public static FluxParticleParameterDefinition Compile(FluxParticleParameterContract contract)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));
            var type = StorageType(contract.Kind);
            if (contract.EnumConstraint != null)
            {
                foreach (var value in contract.EnumConstraint.AllowedValues)
                {
                    EncodeEnum(contract.Id, value);
                }
            }
            return new FluxParticleParameterDefinition(contract, type, Policy(contract.Policy));
        }

        /// <summary>
        /// 型付きの値を契約で検証し、Shaderへ渡す値へ変換します。
        /// </summary>
        public static Vector4 Encode<T>(FluxParticleParameterContract<T> contract, T value) where T : struct
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));
            if (!contract.IsValid(value)) throw new InvalidOperationException($"Parameter '{contract.Id}': value violates its contract.");
            switch (contract.Kind)
            {
                case FluxParticleParameterKind.Float:
                    return new Vector4((float)(object)value, 0, 0, 0);
                case FluxParticleParameterKind.Vector2:
                    var vector2 = (Vector2)(object)value;
                    return new Vector4(vector2.x, vector2.y, 0, 0);
                case FluxParticleParameterKind.Vector3:
                    var vector3 = (Vector3)(object)value;
                    return new Vector4(vector3.x, vector3.y, vector3.z, 0);
                case FluxParticleParameterKind.Vector4:
                    return (Vector4)(object)value;
                case FluxParticleParameterKind.Color:
                    return (Color)(object)value;
                case FluxParticleParameterKind.Bool:
                    return new Vector4((bool)(object)value ? 1 : 0, 0, 0, 0);
                case FluxParticleParameterKind.Enum:
                    return new Vector4(EncodeEnum(contract.Id, value), 0, 0, 0);
                default:
                    throw new InvalidOperationException($"Parameter '{contract.Id}': unsupported kind {contract.Kind}.");
            }
        }

        static FluxParticleValueType StorageType(FluxParticleParameterKind kind)
        {
            switch (kind)
            {
                case FluxParticleParameterKind.Float:
                case FluxParticleParameterKind.Bool:
                case FluxParticleParameterKind.Enum:
                    return FluxParticleValueType.Float;
                case FluxParticleParameterKind.Vector2:
                    return FluxParticleValueType.Float2;
                case FluxParticleParameterKind.Vector3:
                    return FluxParticleValueType.Float3;
                case FluxParticleParameterKind.Vector4:
                case FluxParticleParameterKind.Color:
                    return FluxParticleValueType.Float4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        static CompiledPolicy Policy(ContractPolicy policy)
        {
            switch (policy)
            {
                case ContractPolicy.Static: return CompiledPolicy.Static;
                case ContractPolicy.Live: return CompiledPolicy.Live;
                case ContractPolicy.SpawnLocked: return CompiledPolicy.SpawnLocked;
                default: throw new ArgumentOutOfRangeException(nameof(policy));
            }
        }

        static float EncodeEnum(string id, object value)
        {
            var number = Convert.ToDecimal(value);
            var encoded = (float)number;
            try
            {
                var decoded = number < 0 ? (decimal)checked((long)encoded) : checked((ulong)encoded);
                if (decoded == number) return encoded;
            }
            catch (OverflowException)
            {
                // Floatへの丸めで元の整数型の範囲を超えた値は格納できません。
            }
            throw new InvalidOperationException($"Parameter '{id}': Enum value '{value}' cannot be represented exactly as a Float.");
        }
    }
}
