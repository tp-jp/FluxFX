using System.Collections.Generic;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// Particleのコンパイル結果と生成Shaderを保持します。
    /// </summary>
    public sealed class FluxParticleBuildResult
    {
        /// <summary>
        /// GPU PassとState Layoutを含む生成結果です。
        /// </summary>
        public FluxParticleShaderProgram Program { get; }

        /// <summary>
        /// Parameter IDごとの初期値です。
        /// </summary>
        public IReadOnlyDictionary<string, Vector4> InitialValues { get; }

        /// <summary>
        /// Billboard描画に使用するShaderLabソースです。
        /// </summary>
        public string RendererSource { get; }

        public FluxParticleBuildResult(FluxParticleShaderProgram program, IReadOnlyDictionary<string, Vector4> initialValues)
        {
            Program = program;
            InitialValues = initialValues;
            RendererSource = FluxParticleBillboardShaderGenerator.Generate(program, initialValues);
        }

    }
}
