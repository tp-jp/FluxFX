using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Inspector
{
    /// <summary>
    /// Editor内でParticleのGPU実行とプレビュー描画を管理します。
    /// </summary>
    public sealed class FluxParticleEditorPreview : IDisposable
    {
        readonly List<Object> objects = new List<Object>();
        readonly FluxParticleAuthoring authoring;
        readonly FluxParticleProgramGpuSession session;
        readonly Material material;
        readonly Material maskMaterial;
        readonly Material keyMaterial;
        readonly RenderTexture mask;
        readonly RenderTexture keys;
        readonly Mesh mesh;
        readonly Material countMaterial;
        readonly RenderTexture countTexture;
        int countGeneration;
        bool countPending;
        double lastCountTime;
        readonly BurstSettings[] bursts;
        readonly bool usesSimulationTime;
        double fraction;
        float delay;
        float cycleTime;
        int burstIndex;
        int cursor;
        int seed;
        bool completed;
        bool disposed;

        /// <summary>
        /// プレビューで使用するParticle数
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 再生開始からSimulationを進めた累計秒数
        /// </summary>
        public double PlaybackTime { get; private set; }

        /// <summary>
        /// 最新のGPU生存判定で得た粒子数。取得できない場合は-1
        /// </summary>
        public int AliveCount { get; private set; }

        /// <summary>
        /// 生成結果からプレビュー用のGPUリソースを構築します。
        /// </summary>
        /// <param name="build">Particleのビルド結果</param>
        /// <param name="authoring">Particleの設定</param>
        /// <param name="count">プレビューのParticle数</param>
        public FluxParticleEditorPreview(FluxParticleBuildResult build, FluxParticleAuthoring authoring, int count)
        {
            if (count < 1 || count > 4096) throw new ArgumentOutOfRangeException(nameof(count));
            this.authoring = authoring;
            Count = count;
            bursts = authoring.Emission.Bursts.OrderBy(burst => burst.Time).ToArray();
            try
            {
                var width = Mathf.CeilToInt(Mathf.Sqrt(count));
                var shaders = build.Shaders;
                usesSimulationTime = shaders.Compilation.Providers.ContainsKey("step.time");
                session = new FluxParticleProgramGpuSession(shaders,
                    shaders.Passes.Select(pass => CreateShader(pass.Source)).ToArray(),
                    shaders.ClearSources.Select(CreateShader).ToArray(), width, width);
                foreach (var parameter in shaders.Compilation.Parameters.Values)
                {
                    var value = build.InitialValues[parameter.Id];
                    if (parameter.Type == FluxParticleValueType.Float) session.SetFloat(parameter.Id, value.x);
                    else session.SetVector(parameter.Id, value);
                }
                material = Own(new Material(CreateShader(build.RendererSource)));
                var source = authoring.Render.Material;
                material.mainTexture = source == null ? null : source.mainTexture;
                material.mainTextureScale = source == null ? Vector2.one : source.mainTextureScale;
                material.mainTextureOffset = source == null ? Vector2.zero : source.mainTextureOffset;
                var blend = authoring.Render.BlendMode;
                material.SetFloat("_SrcBlend", blend == FluxParticleBlendMode.Opaque ? 1 : 5);
                material.SetFloat("_DstBlend", blend == FluxParticleBlendMode.Alpha ? 10 : blend == FluxParticleBlendMode.Additive ? 1 : 0);
                material.SetFloat("_ZWrite", blend == FluxParticleBlendMode.Opaque ? 1 : 0);
                material.renderQueue = blend == FluxParticleBlendMode.Opaque ? 2000 : 3000;
                maskMaterial = Own(new Material(AssetDatabase.LoadAssetAtPath<Shader>(
                    "Packages/com.tplab.flux.fx/Runtime/Shaders/FluxParticleSpawnMask.shader")));
                keyMaterial = Own(new Material(AssetDatabase.LoadAssetAtPath<Shader>(
                    "Packages/com.tplab.flux.fx/Runtime/Shaders/FluxParticleSpawnKey.shader")));
                mask = CreateTexture(width);
                keys = CreateTexture(width);
                var dimensions = new Vector4(width, width, count, 0);
                maskMaterial.SetVector("_FluxStateSize", dimensions);
                keyMaterial.SetVector("_FluxStateSize", dimensions);
                material.SetVector("_FluxStateSize", dimensions);
                session.SetSpawnInput("spawn.key", keys);
                mesh = CreateMesh(count);
                if (SystemInfo.supportsAsyncGPUReadback)
                {
                    var countSource = string.Join(Environment.NewLine, new[]
                    {
                        @"Shader ""Hidden/FluxFX/Preview/AliveCount"" { SubShader { Pass { ZTest Always Cull Off ZWrite Off Blend Off",
                        "HLSLPROGRAM",
                        "#pragma target 3.5",
                        "#pragma vertex vert_img",
                        "#pragma fragment frag",
                        @"#include ""UnityCG.cginc""",
                        shaders.RenderSource,
                        "float4 frag(v2f_img input) : SV_Target { bool alive; FluxFX_EvaluateRender(input.uv, alive);",
                        "return float4(alive ? 1 : 0, 0, 0, 0); }",
                        "ENDHLSL",
                        "} } Fallback Off }"
                    });
                    countMaterial = Own(new Material(CreateShader(countSource)));
                    countTexture = CreateTexture(width);
                }
                Restart();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// GPU状態とEmissionの再生位置を初期化します。
        /// </summary>
        public void Restart()
        {
            session.Clear();
            PlaybackTime = 0;
            countGeneration++;
            countPending = false;
            lastCountTime = double.NegativeInfinity;
            AliveCount = countMaterial == null ? -1 : 0;
            fraction = 0;
            delay = authoring.Playback.StartDelay;
            cycleTime = 0;
            burstIndex = 0;
            cursor = 0;
            seed = 0;
            completed = false;
        }

        /// <summary>
        /// EmissionとGPU Simulationを進めます。
        /// </summary>
        /// <param name="deltaTime">経過秒数</param>
        public void Step(float deltaTime)
        {
            var spawnCount = Emit(deltaTime);
            var visible = Math.Min(Count, spawnCount);
            var skipped = spawnCount - visible;
            var first = (int)((cursor + skipped) % Count);
            maskMaterial.SetFloat("_FluxSpawnFirst", first);
            maskMaterial.SetFloat("_FluxSpawnCount", visible);
            keyMaterial.SetFloat("_FluxSpawnFirst", first);
            keyMaterial.SetFloat("_FluxSpawnCount", visible);
            keyMaterial.SetFloat("_FluxSpawnSeedBase", (seed + skipped) % 1048576);
            var active = RenderTexture.active;
            try
            {
                if (visible > 0)
                {
                    Graphics.Blit(null, mask, maskMaterial);
                    Graphics.Blit(null, keys, keyMaterial);
                }
                session.SetUniformFloat("step.delta-time", deltaTime);
                if (usesSimulationTime)
                {
                    session.SetUniformFloat("step.time", (float)PlaybackTime);
                }
                session.Step(visible > 0 ? mask : null);
            }
            finally
            {
                RenderTexture.active = active;
            }
            cursor = (int)((cursor + spawnCount) % Count);
            seed = (int)((seed + spawnCount) % 1048576);
            PlaybackTime += deltaTime;
        }

        /// <summary>
        /// 現在のGPU状態を指定したカメラへ描画します。
        /// </summary>
        /// <param name="camera">描画先のSceneViewカメラ</param>
        /// <param name="matrix">Particle Systemのローカルからワールドへの変換</param>
        /// <param name="layer">描画対象のLayer</param>
        public void Draw(Camera camera, Matrix4x4 matrix, int layer)
        {
            session.BindRender(material);
            Graphics.DrawMesh(mesh, matrix, material, layer, camera, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
        }

        /// <summary>
        /// GPUの生存判定を非同期で取得し、粒子数を更新します。
        /// </summary>
        public void RequestAliveCount()
        {
            if (disposed || countMaterial == null || countPending ||
                EditorApplication.timeSinceStartup - lastCountTime < 0.2) return;
            session.BindRender(countMaterial);
            var active = RenderTexture.active;
            try
            {
                Graphics.Blit(null, countTexture, countMaterial);
            }
            finally
            {
                RenderTexture.active = active;
            }
            lastCountTime = EditorApplication.timeSinceStartup;
            countPending = true;
            var generation = countGeneration;
            AsyncGPUReadback.Request(countTexture, 0, TextureFormat.RGBAFloat, request =>
            {
                if (disposed || generation != countGeneration) return;
                countPending = false;
                if (request.hasError)
                {
                    AliveCount = -1;
                    return;
                }
                var data = request.GetData<Color>();
                var alive = 0;
                for (var i = 0; i < Count; i++)
                {
                    if (data[i].r > 0.5f) alive++;
                }
                AliveCount = alive;
                SceneView.RepaintAll();
            });
        }
        long Emit(float deltaTime)
        {
            if (completed) return 0;
            var remaining = deltaTime;
            var delayStep = Mathf.Min(delay, remaining);
            delay -= delayStep;
            remaining -= delayStep;
            long count = 0;
            while (remaining > 0)
            {
                var step = Mathf.Min(remaining, authoring.Playback.Duration - cycleTime);
                var previous = cycleTime;
                cycleTime += step;
                remaining -= step;
                fraction += step * (double)authoring.Emission.Rate;
                var rateCount = (long)Math.Min(int.MaxValue, Math.Floor(fraction));
                fraction -= rateCount;
                count += rateCount;
                while (burstIndex < bursts.Length && bursts[burstIndex].Time <= cycleTime)
                {
                    var burst = bursts[burstIndex++];
                    if (burst.Time >= previous) count += burst.Count;
                }
                if (cycleTime < authoring.Playback.Duration) break;
                if (!authoring.Playback.Loop)
                {
                    completed = true;
                    break;
                }
                cycleTime = 0;
                burstIndex = 0;
            }
            return count;
        }

        Shader CreateShader(string source)
        {
            var shader = Own(ShaderUtil.CreateShaderAsset(source, true));
            if (ShaderUtil.ShaderHasError(shader) || !shader.isSupported)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine,
                    ShaderUtil.GetShaderMessages(shader).Select(message => message.message)));
            }
            return shader;
        }

        RenderTexture CreateTexture(int width)
        {
            var texture = Own(new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            });
            if (!texture.Create()) throw new InvalidOperationException("Preview RenderTexture allocation failed.");
            return texture;
        }

        Mesh CreateMesh(int count)
        {
            var vertices = new Vector3[count * 4];
            var uv = new Vector2[count * 4];
            var indices = new Vector2[count * 4];
            var triangles = new int[count * 6];
            var corners = new[] { new Vector3(-0.05f, -0.05f, 0), new Vector3(0.05f, -0.05f, 0),
                new Vector3(0.05f, 0.05f, 0), new Vector3(-0.05f, 0.05f, 0) };
            var coordinates = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            var order = new[] { 0, 1, 2, 0, 2, 3 };
            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    vertices[i * 4 + j] = corners[j];
                    uv[i * 4 + j] = coordinates[j];
                    indices[i * 4 + j] = new Vector2(i, 0);
                }
                for (var j = 0; j < 6; j++) triangles[i * 6 + j] = i * 4 + order[j];
            }
            var result = Own(new Mesh { name = "FluxFX Preview Mesh" });
            result.vertices = vertices;
            result.uv = uv;
            result.uv2 = indices;
            result.triangles = triangles;
            result.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
            return result;
        }

        T Own<T>(T value) where T : Object
        {
            value.hideFlags = HideFlags.HideAndDontSave;
            objects.Add(value);
            return value;
        }

        /// <summary>
        /// プレビュー用のGPUリソースと描画環境を解放します。
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            session?.Dispose();
            for (var i = objects.Count - 1; i >= 0; i--)
            {
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            }
            objects.Clear();
        }
    }
}
