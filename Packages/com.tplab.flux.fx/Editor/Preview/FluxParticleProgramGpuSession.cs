using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Compilation.Compiler;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Compilation.ShaderGeneration;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Preview
{
    /// <summary>
    /// 生成ProgramをUnity Editorで検証するGPU Backendです。
    /// 1Pageにつき2RTを確保し、StepPreviousを保持したまま全Pass実行後にCommitします。
    /// </summary>
    public sealed class FluxParticleProgramGpuSession : IDisposable
    {
        readonly FluxParticleShaderProgram program;
        readonly List<Texture2D> lookupTextures = new List<Texture2D>();
        readonly Material[] steps;
        readonly Material[] clears;
        readonly RenderTexture[] current;
        readonly RenderTexture[] next;
        readonly HashSet<string> assigned = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, Action<Material>> bindings = new Dictionary<string, Action<Material>>(StringComparer.Ordinal);
        bool started;
        bool disposed;

        /// <summary>
        /// 確定済みPage Texture
        /// </summary>
        /// <remarks>Stepをまたいで参照をキャッシュしないでください。</remarks>
        public IReadOnlyList<RenderTexture> CurrentPages => Array.AsReadOnly(current);

        /// <summary>
        /// 生成したStep/Clear Shaderから専用MaterialとState RTを作り、全Bufferを初期化します。
        /// </summary>
        /// <param name="program">実行対象の確定Program</param>
        /// <param name="stepShaders">Pass順のImport済みShader</param>
        /// <param name="clearShaders">Page順のImport済みShader</param>
        /// <param name="width">State Textureの幅</param>
        /// <param name="height">State Textureの高さ</param>
        public FluxParticleProgramGpuSession(FluxParticleShaderProgram program, IReadOnlyList<Shader> stepShaders,
            IReadOnlyList<Shader> clearShaders, int width, int height)
        {
            if (program == null) throw new ArgumentNullException(nameof(program));
            if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (stepShaders == null || stepShaders.Count != program.Passes.Count || clearShaders == null || clearShaders.Count != program.ClearSources.Count)
            {
                throw new ArgumentException("Shader counts must match the generated Program.");
            }
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
            {
                throw new NotSupportedException("ARGBFloat State textures are required.");
            }
            this.program = program;
            steps = new Material[stepShaders.Count];
            clears = new Material[clearShaders.Count];
            current = new RenderTexture[clearShaders.Count];
            next = new RenderTexture[clearShaders.Count];
            try
            {
                for (var i = 0; i < steps.Length; i++) steps[i] = CreateMaterial(stepShaders[i]);
                for (var i = 0; i < clears.Length; i++)
                {
                    clears[i] = CreateMaterial(clearShaders[i]);
                    current[i] = CreateTexture(width, height);
                    next[i] = CreateTexture(width, height);
                }
                foreach (var provider in program.Compilation.Providers.Values.Where(provider => provider.Kind == FluxParticleProviderKind.LookupTable))
                {
                    var texture = FluxParticleLookupTexture.Create(provider);
                    lookupTextures.Add(texture);
                    bindings.Add("lookup:" + provider.ValueId, material =>
                        material.SetTexture(FluxParticleHlslNames.ProviderProperty(provider.ValueId), texture));
                }
                Clear();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// 両Bufferを既知の値へ戻します。Static Parameterの変更制限は解除しません。
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            var active = RenderTexture.active;
            try
            {
                for (var i = 0; i < clears.Length; i++)
                {
                    Graphics.Blit(null, current[i], clears[i]);
                    Graphics.Blit(null, next[i], clears[i]);
                }
            }
            finally
            {
                RenderTexture.active = active;
            }
        }

        /// <summary>
        /// Float Parameterを設定します。Staticは最初のStep以降変更できません。
        /// </summary>
        /// <param name="id">Parameter ID</param>
        /// <param name="value">設定値</param>
        public void SetFloat(string id, float value)
        {
            var parameter = RequireParameter(id, FluxParticleValueType.Float);
            Apply("p:" + id, m => m.SetFloat(FluxParticleHlslNames.ParameterProperty(parameter.Id), value));
            assigned.Add("p:" + id);
        }

        /// <summary>
        /// Float2/3/4 Parameterを設定します。
        /// </summary>
        /// <param name="id">Parameter ID</param>
        /// <param name="value">設定値</param>
        public void SetVector(string id, Vector4 value)
        {
            var parameter = RequireParameter(id);
            if (parameter.Type == FluxParticleValueType.Float || parameter.Type == FluxParticleValueType.UInt)
            {
                throw new ArgumentException($"Vector Parameter required: {id}");
            }
            Apply("p:" + id, m => m.SetVector(FluxParticleHlslNames.ParameterProperty(id), value));
            assigned.Add("p:" + id);
        }

        /// <summary>
        /// UInt Parameterを設定します。
        /// </summary>
        /// <param name="id">Parameter ID</param>
        /// <param name="value">設定値</param>
        public void SetUInt(string id, uint value)
        {
            RequireParameter(id, FluxParticleValueType.UInt);
            Apply("p:" + id, m => m.SetInteger(FluxParticleHlslNames.ParameterProperty(id), unchecked((int)value)));
            assigned.Add("p:" + id);
        }

        /// <summary>
        /// Float Uniform Providerを設定します。
        /// </summary>
        /// <param name="valueId">Uniform ProviderのValue ID</param>
        /// <param name="value">設定値</param>
        public void SetUniformFloat(string valueId, float value)
        {
            RequireProvider(valueId, FluxParticleProviderKind.Uniform, FluxParticleValueType.Float);
            Apply("v:" + valueId, m => m.SetFloat(FluxParticleHlslNames.ProviderProperty(valueId), value));
            assigned.Add("v:" + valueId);
        }

        /// <summary>
        /// Float2/3/4 Uniform Providerを設定します。
        /// </summary>
        /// <param name="valueId">Uniform ProviderのValue ID</param>
        /// <param name="value">設定値</param>
        public void SetUniformVector(string valueId, Vector4 value)
        {
            var type = RequireProvider(valueId, FluxParticleProviderKind.Uniform);
            if (type == FluxParticleValueType.Float || type == FluxParticleValueType.UInt) throw new ArgumentException("Vector Provider required.");
            Apply("v:" + valueId, m => m.SetVector(FluxParticleHlslNames.ProviderProperty(valueId), value));
            assigned.Add("v:" + valueId);
        }

        /// <summary>
        /// UInt Uniform Providerを設定します。
        /// </summary>
        /// <param name="valueId">Uniform ProviderのValue ID</param>
        /// <param name="value">設定値</param>
        public void SetUniformUInt(string valueId, uint value)
        {
            RequireProvider(valueId, FluxParticleProviderKind.Uniform, FluxParticleValueType.UInt);
            Apply("v:" + valueId, m => m.SetInteger(FluxParticleHlslNames.ProviderProperty(valueId), unchecked((int)value)));
            assigned.Add("v:" + valueId);
        }

        /// <summary>
        /// SpawnInputをRGBA Textureで供給します。UIntは整数精度を保証できるfloat値で格納します。
        /// </summary>
        /// <param name="valueId">SpawnInput ProviderのValue ID</param>
        /// <param name="texture">State RTと同じ寸法でPoint/Clampの線形Texture</param>
        public void SetSpawnInput(string valueId, Texture texture)
        {
            RequireProvider(valueId, FluxParticleProviderKind.SpawnInput);
            ValidateInputTexture(texture);
            Apply("v:" + valueId, m => m.SetTexture(FluxParticleHlslNames.ProviderProperty(valueId), texture));
            assigned.Add("v:" + valueId);
        }

        /// <summary>
        /// 確定したPassを順に実行し、書き込んだPageだけをCommitします。
        /// </summary>
        /// <param name="spawnMask">Rが0.5より大きいTexelへの生成要求（Lifecycle使用時は空きTexelのみ、nullなら生成なし）</param>
        public void Step(Texture spawnMask = null)
        {
            ThrowIfDisposed();
            if (spawnMask != null) ValidateInputTexture(spawnMask);
            foreach (var parameter in program.Compilation.Parameters.Values)
            {
                if (!assigned.Contains("p:" + parameter.Id)) throw new InvalidOperationException($"Parameter is not assigned: {parameter.Id}");
            }
            foreach (var provider in program.ExternalProviders)
            {
                if (provider.Kind == FluxParticleProviderKind.SpawnInput && spawnMask == null) continue;
                if (!assigned.Contains("v:" + provider.ValueId)) throw new InvalidOperationException($"Provider is not assigned: {provider.ValueId}");
            }
            started = true;
            var updated = new bool[current.Length];
            var active = RenderTexture.active;
            try
            {
                for (var i = 0; i < program.Passes.Count; i++)
                {
                    var pass = program.Passes[i];
                    if (pass.Plan.Condition == FluxParticlePassCondition.HasSpawn && spawnMask == null) continue;
                    var material = steps[i];
                    material.SetTexture("_FluxSpawnMask", spawnMask == null ? Texture2D.blackTexture : spawnMask);
                    foreach (var read in pass.Reads)
                    {
                        var texture = read.Version == FluxParticlePageVersion.Updated && updated[read.PageId] ? next[read.PageId] : current[read.PageId];
                        material.SetTexture(FluxParticleHlslNames.PageProperty(read.PageId, read.Version), texture);
                    }
                    Graphics.Blit(null, next[pass.Plan.OutputPageId], material);
                    updated[pass.Plan.OutputPageId] = true;
                }
                for (var page = 0; page < current.Length; page++)
                {
                    if (!updated[page]) continue;
                    var previous = current[page];
                    current[page] = next[page];
                    next[page] = previous;
                }
            }
            finally
            {
                RenderTexture.active = active;
            }
        }

        /// <summary>
        /// RenderSourceを組み込んだMaterialへParameterと最新の確定Pageを供給します。
        /// </summary>
        /// <param name="material">呼び出し元が所有する描画用Material</param>
        public void BindRender(Material material)
        {
            ThrowIfDisposed();
            if (material == null) throw new ArgumentNullException(nameof(material));
            foreach (var binding in bindings.Values) binding(material);
            foreach (var page in program.Compilation.StateLayout.Pages)
            {
                material.SetTexture(FluxParticleHlslNames.PageProperty(page.Id, FluxParticlePageVersion.Updated), current[page.Id]);
            }
        }

        /// <summary>
        /// State RTとSessionが所有するMaterialを破棄します。呼び出し元のShaderは破棄しません。
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var texture in lookupTextures)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            foreach (var material in steps.Concat(clears))
            {
                if (material != null) UnityEngine.Object.DestroyImmediate(material);
            }
            foreach (var texture in current.Concat(next))
            {
                if (texture == null) continue;
                texture.Release();
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        FluxParticleParameterDefinition RequireParameter(string id, FluxParticleValueType? expectedType = null)
        {
            ThrowIfDisposed();
            if (!program.Compilation.Parameters.TryGetValue(id, out var parameter)) throw new ArgumentException($"Unknown Parameter: {id}");
            if (expectedType.HasValue && parameter.Type != expectedType.Value) throw new ArgumentException($"Parameter type mismatch: {id}");
            if (started && parameter.Policy == FluxParticleParameterPolicy.Static) throw new InvalidOperationException($"Static Parameter is locked: {id}");
            return parameter;
        }

        FluxParticleValueType RequireProvider(string id, FluxParticleProviderKind kind, FluxParticleValueType? expectedType = null)
        {
            ThrowIfDisposed();
            if (!program.ExternalProviders.Any(p => p.ValueId == id && p.Kind == kind)) throw new ArgumentException($"Unknown Provider: {id}");
            var type = program.Compilation.Values[id].Type;
            if (expectedType.HasValue && type != expectedType.Value) throw new ArgumentException($"Provider type mismatch: {id}");
            return type;
        }

        void ValidateInputTexture(Texture texture)
        {
            if (texture == null || current.Length == 0 || texture.width != current[0].width || texture.height != current[0].height ||
                texture.filterMode != FilterMode.Point || texture.wrapMode != TextureWrapMode.Clamp)
            {
                throw new ArgumentException("Input Texture must match State dimensions and use Point/Clamp.");
            }
        }

        void Apply(string key, Action<Material> action)
        {
            bindings[key] = action;
            foreach (var material in steps) action(material);
        }

        void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(FluxParticleProgramGpuSession));
        }

        static Material CreateMaterial(Shader shader)
        {
            if (shader == null || !shader.isSupported) throw new ArgumentException("A supported imported Shader is required.");
            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        static RenderTexture CreateTexture(int width, int height)
        {
            var texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                useMipMap = false,
                autoGenerateMips = false
            };
            if (texture.Create()) return texture;
            UnityEngine.Object.DestroyImmediate(texture);
            throw new InvalidOperationException("State Texture allocation failed.");
        }
    }
}
