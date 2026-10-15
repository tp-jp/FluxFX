using System;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Contracts
{
    /// <summary>
    /// Parameterの型に対応するBuilderを作成します。
    /// </summary>
    public static class FluxParticleParameter
    {
        /// <summary>
        /// Float ParameterのBuilderを作成します。
        /// </summary>
        public static FluxParticleNumericParameterBuilder<float> Float(string id) => new FluxParticleNumericParameterBuilder<float>(id, FluxParticleParameterKind.Float);

        /// <summary>
        /// Vector2 ParameterのBuilderを作成します。
        /// </summary>
        public static FluxParticleNumericParameterBuilder<Vector2> Vector2(string id) => new FluxParticleNumericParameterBuilder<Vector2>(id, FluxParticleParameterKind.Vector2);

        /// <summary>
        /// Vector3 ParameterのBuilderを作成します。
        /// </summary>
        public static FluxParticleNumericParameterBuilder<Vector3> Vector3(string id) => new FluxParticleNumericParameterBuilder<Vector3>(id, FluxParticleParameterKind.Vector3);

        /// <summary>
        /// Vector4 ParameterのBuilderを作成します。
        /// </summary>
        public static FluxParticleNumericParameterBuilder<Vector4> Vector4(string id) => new FluxParticleNumericParameterBuilder<Vector4>(id, FluxParticleParameterKind.Vector4);

        /// <summary>
        /// Color ParameterのBuilderを作成します。
        /// </summary>
        public static FluxParticleNumericParameterBuilder<Color> Color(string id) => new FluxParticleNumericParameterBuilder<Color>(id, FluxParticleParameterKind.Color);

        /// <summary>
        /// Bool ParameterのBuilderを作成します。
        /// </summary>
        public static FluxParticleBooleanParameterBuilder Bool(string id) => new FluxParticleBooleanParameterBuilder(id);

        /// <summary>
        /// Enum ParameterのBuilderを作成します。
        /// </summary>
        public static FluxParticleEnumParameterBuilder<T> Enum<T>(string id) where T : struct, Enum => new FluxParticleEnumParameterBuilder<T>(id);
    }
}
