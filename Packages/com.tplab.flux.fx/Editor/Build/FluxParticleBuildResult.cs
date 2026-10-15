using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Compilation.ShaderGeneration;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// Particleのコンパイル結果と生成Shaderを保持します。
    /// </summary>
    public sealed class FluxParticleBuildResult
    {
        /// <summary>
        /// GPU PassとState Layoutを含む生成結果
        /// </summary>
        public FluxParticleShaderProgram Shaders { get; }

        /// <summary>
        /// Parameter IDごとの初期値
        /// </summary>
        public IReadOnlyDictionary<string, Vector4> InitialValues { get; }

        /// <summary>
        /// Particle描画用のShaderLabソース
        /// </summary>
        public string RendererSource { get; }

        public FluxParticleBuildResult(FluxParticleShaderProgram program, IReadOnlyDictionary<string, Vector4> initialValues, string rendererSource)
        {
            Shaders = program;
            InitialValues = initialValues;
            RendererSource = rendererSource;
        }

    }
}
