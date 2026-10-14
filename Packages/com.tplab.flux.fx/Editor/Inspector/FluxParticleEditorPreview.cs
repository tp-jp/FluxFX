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
        readonly Material[] allocationMaterials;
        readonly RenderTexture[] allocationLevels;
        readonly RenderTexture mask;
        readonly RenderTexture keys;
        readonly Mesh mesh;
        int countGeneration;
        bool countPending;
        double lastCountTime;
        readonly float[] burstTimes;
        readonly int[] burstCounts;
        double[] emissionState;
        readonly bool usesSimulationTime;
        int cursor;
        int seed;
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
        /// 最新のGPU生存判定で得た粒子数
        /// </summary>
        /// <remarks>取得できない場合は-1</remarks>
        public int AliveCount { get; private set; }

        /// <summary>
        /// 生成結果からプレビュー用のGPUリソースを構築します。
        /// </summary>
        /// <param name="build">Particleのビルド結果</param>
        /// <param name="authoring">Particleの設定</param>
        /// <param name="count">プレビューのParticle数</param>
        public FluxParticleEditorPreview(FluxParticleBuildResult build, FluxParticleAuthoring authoring, int count)
        {
            var sourceMesh = authoring.Render.Mode == FluxParticleRenderMode.Mesh ? authoring.Render.Mesh : null;
            var maximum = FluxParticleCapacityValidator.GetMaximumCount(sourceMesh);
            if (count < 1 || count > maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Particle count must be in [1, {maximum}] for this GPU and render Mesh.");
            }
            this.authoring = authoring;
            Count = count;
            var bursts = authoring.Emission.Bursts.OrderBy(burst => burst.Time).ToArray();
            burstTimes = bursts.Select(burst => burst.Time).ToArray();
            burstCounts = bursts.Select(burst => burst.Count).ToArray();
            emissionState = FluxParticleEmissionScheduler.Create(authoring.Playback.StartDelay);
            try
            {
                var width = FluxParticleCapacity.GetTextureWidth(count);
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
                allocationMaterials = new[]
                {
                    Own(new Material(CreateShader(shaders.AvailabilitySource))),
                    Own(new Material(AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.tplab.flux.fx/Runtime/Shaders/FluxParticleFreeReduction.shader"))),
                    Own(new Material(AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.tplab.flux.fx/Runtime/Shaders/FluxParticleFreeRanks.shader"))),
                    Own(new Material(AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.tplab.flux.fx/Runtime/Shaders/FluxParticleAllocatedSpawnKey.shader")))
                };
                allocationLevels = FluxParticleSpawnAllocator.CreateTextures(count);
                if (allocationLevels == null) throw new InvalidOperationException("Spawn allocation RenderTexture allocation failed.");
                foreach (var texture in allocationLevels) Own(texture);
                mask = CreateTexture(width);
                keys = CreateTexture(width);
                FluxParticleSpawnAllocator.Configure(allocationMaterials, allocationLevels, mask, count);
                var dimensions = new Vector4(width, width, count, 0);
                material.SetVector("_FluxStateSize", dimensions);
                session.SetSpawnInput("spawn.key", keys);
                mesh = CreateMesh(count);
                Restart();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// GPUリソースを再生成し、有効なStateと再生位置を引き継ぎます。
        /// </summary>
        /// <param name="build">Particleのビルド結果</param>
        /// <returns>再生成したPreview</returns>
        public FluxParticleEditorPreview Recreate(FluxParticleBuildResult build)
        {
            var replacement = new FluxParticleEditorPreview(build, authoring, Count);
            try
            {
                var pages = session.CurrentPages;
                var destinations = replacement.session.CurrentPages;
                if (pages.Any(page => page == null || !page.IsCreated())) return replacement;
                var active = RenderTexture.active;
                try
                {
                    for (var i = 0; i < pages.Count; i++)
                    {
                        Graphics.Blit(pages[i], destinations[i]);
                    }
                }
                finally
                {
                    RenderTexture.active = active;
                }
                replacement.PlaybackTime = PlaybackTime;
                replacement.emissionState = (double[])emissionState.Clone();
                replacement.cursor = cursor;
                replacement.seed = seed;
                replacement.AliveCount = AliveCount;
                return replacement;
            }
            catch
            {
                replacement.Dispose();
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
            AliveCount = SystemInfo.supportsAsyncGPUReadback ? 0 : -1;
            FluxParticleEmissionScheduler.Reset(emissionState, authoring.Playback.StartDelay);
            cursor = 0;
            seed = 0;
        }
        /// <summary>
        /// EmissionとGPU Simulationを進めます。
        /// </summary>
        /// <param name="deltaTime">経過秒数</param>
        public void Step(float deltaTime)
        {
            var spawnCount = FluxParticleEmissionScheduler.Emit(emissionState, deltaTime, authoring.Emission.Rate,
                authoring.Playback.Duration, authoring.Playback.Loop, burstTimes, burstCounts);
            var visible = Math.Min(Count, spawnCount);
            var skipped = spawnCount - visible;
            var first = (int)((cursor + skipped) % Count);
            var active = RenderTexture.active;
            try
            {
                if (authoring.SimulationSpace == FluxParticleSimulationSpace.World)
                {
                    var matrix = authoring.transform.localToWorldMatrix;
                    for (var row = 0; row < 3; row++)
                    {
                        session.SetUniformVector("emitter.row" + row, matrix.GetRow(row));
                    }
                }
                session.SetUniformFloat("step.delta-time", deltaTime);
                if (usesSimulationTime)
                {
                    session.SetUniformFloat("step.time", (float)PlaybackTime);
                }
                if (visible > 0)
                {
                    session.BindRender(allocationMaterials[0]);
                    FluxParticleSpawnAllocator.Execute(allocationMaterials, allocationLevels, mask, keys, Count,
                        first, (int)visible, (int)((seed + skipped) % 1048576));
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
            if (disposed || !SystemInfo.supportsAsyncGPUReadback || countPending ||
                EditorApplication.timeSinceStartup - lastCountTime < 0.2) return;
            session.BindRender(allocationMaterials[0]);
            var active = RenderTexture.active;
            try
            {
                FluxParticleSpawnAllocator.BuildCounts(allocationMaterials, allocationLevels, Count);
            }
            finally
            {
                RenderTexture.active = active;
            }
            lastCountTime = EditorApplication.timeSinceStartup;
            countPending = true;
            var generation = countGeneration;
            AsyncGPUReadback.Request(allocationLevels[allocationLevels.Length - 1], 0, TextureFormat.RGBAFloat, request =>
            {
                if (disposed || generation != countGeneration) return;
                countPending = false;
                if (request.hasError)
                {
                    AliveCount = -1;
                    return;
                }
                var data = request.GetData<Color>();
                AliveCount = Count - Mathf.RoundToInt(data[0].r);
                SceneView.RepaintAll();
            });
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
            if (authoring.Render.Mode == FluxParticleRenderMode.Mesh) return CreateMesh(authoring.Render.Mesh, count);
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
            if (vertices.Length > 65535)
            {
                result.indexFormat = IndexFormat.UInt32;
            }
            result.vertices = vertices;
            result.uv = uv;
            result.uv2 = indices;
            result.triangles = triangles;
            result.bounds = authoring.Render.CullingBounds;
            return result;
        }

        Mesh CreateMesh(Mesh source, int count)
        {
            var sourceVertices = source.vertices;
            var sourceUv = source.uv;
            var sourceTriangles = source.triangles;
            var vertices = new Vector3[checked(sourceVertices.Length * count)];
            var uv = new Vector2[vertices.Length];
            var indices = new Vector2[vertices.Length];
            var triangles = new int[checked(sourceTriangles.Length * count)];
            for (var particle = 0; particle < count; particle++)
            {
                var offset = particle * sourceVertices.Length;
                Array.Copy(sourceVertices, 0, vertices, offset, sourceVertices.Length);
                if (sourceUv.Length == sourceVertices.Length)
                {
                    Array.Copy(sourceUv, 0, uv, offset, sourceUv.Length);
                }
                for (var vertex = 0; vertex < sourceVertices.Length; vertex++)
                {
                    indices[offset + vertex] = new Vector2(particle, 0);
                }
                for (var index = 0; index < sourceTriangles.Length; index++)
                {
                    triangles[particle * sourceTriangles.Length + index] = offset + sourceTriangles[index];
                }
            }
            var result = Own(new Mesh { name = "FluxFX Preview Mesh" });
            if (vertices.Length > 65535)
            {
                result.indexFormat = IndexFormat.UInt32;
            }
            result.vertices = vertices;
            result.uv = uv;
            result.uv2 = indices;
            result.triangles = triangles;
            result.bounds = authoring.Render.CullingBounds;
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
