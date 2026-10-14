using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts;
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

        FluxParticleBuildResult(FluxParticleShaderProgram program, IReadOnlyDictionary<string, Vector4> initialValues)
        {
            Program = program;
            InitialValues = initialValues;
            RendererSource = FluxParticleBillboardShaderGenerator.Generate(program, initialValues);
        }

        /// <summary>
        /// Authoring設定からGPU実行と描画に必要な結果を構築します。
        /// </summary>
        /// <param name="authoring">Particleの設定。</param>
        /// <returns>コンパイルとShader生成の結果。</returns>
        public static FluxParticleBuildResult Create(FluxParticleAuthoring authoring)
        {
            FluxParticleAuthoringValidator.Validate(authoring);
            var definitions = FluxParticleAuthoringDefinitionBuilder.Create(authoring, out var initialValues);
            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            var program = new FluxParticleProgramShaderGenerator().Generate(definitions, "Hidden/FluxFX/Program", FluxParticleCoreDefinitions.CreateLifecycle());
            return new FluxParticleBuildResult(program, initialValues);
        }
    }
}
