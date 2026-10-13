using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// 新Programの生成Assetと配列形式Manifestを構築し、Udon Runtimeへ設定します。
    /// 固定Stage Registryを介さず、生成結果のPageとPass順を保持します。
    /// </summary>
    public static class FluxParticleProgramBuild
    {
        const string Root = "Assets/FluxFX/Generated/Programs";

        /// <summary>
        /// SceneFlowのBuild/Play前処理から新Backendを構成します。
        /// </summary>
        /// <param name="authoring">既存のAuthoring設定。</param>
        /// <param name="system">接続先のParticle System。</param>
        public static void Apply(FluxParticleAuthoring authoring, FluxParticleSystem system)
        {
            if (system.ParticleCount < 1 || system.ParticleCount > 1048576)
            {
                throw new InvalidOperationException("Particle count must be in [1, 1048576].");
            }
            var renderer = authoring.GetComponentInChildren<FluxParticleRenderer>(true);
            var emitter = authoring.GetComponentInChildren<FluxParticleEmitter>(true);
            if (renderer == null || emitter == null)
            {
                throw new InvalidOperationException("Program Backend requires the existing Renderer and Emitter.");
            }
            var compiled = new FluxParticleAuthoringProgram(authoring);
            var runtime = authoring.gameObject.GetUdonSharpComponent<FluxParticleExecutor>();
            if (runtime == null)
            {
                runtime = authoring.gameObject.AddUdonSharpComponent<FluxParticleExecutor>();
            }
            Configure(runtime, compiled, authoring, Root, system.ParticleCount);
            system.SetProgramVariable("particleExecutor", runtime);
            system.SetProgramVariable("particleRenderer", renderer);
            system.SetProgramVariable("particleEmitter", emitter);
            var bursts = new JArray();
            foreach (var burst in authoring.Emission.Bursts.OrderBy(burst => burst.Time))
            {
                if (burst == null || burst.Time < 0 || burst.Count < 1)
                {
                    throw new InvalidOperationException("Invalid emission burst.");
                }
                bursts.Add(new JObject { ["time"] = burst.Time, ["count"] = burst.Count });
            }
            var json = new JObject
            {
                ["playback"] = new JObject
                {
                    ["startDelay"] = authoring.Playback.StartDelay,
                    ["duration"] = authoring.Playback.Duration,
                    ["loop"] = authoring.Playback.Loop
                },
                ["emission"] = new JObject { ["rate"] = authoring.Emission.Rate, ["bursts"] = bursts }
            };
            system.SetProgramVariable("compiledParameters", json.ToString(Formatting.None));
            EditorUtility.SetDirty(runtime);
            EditorUtility.SetDirty(system);
            AssetDatabase.SaveAssets();
            Logger.Log($"Program Backend assigned: {authoring.name} ({compiled.Program.Passes.Count} Passes)", authoring);
        }

        /// <summary>
        /// 旧Backendに戻したとき、以前生成したFluxリソース用コンポーネントを削除します。
        /// </summary>
        /// <param name="authoring">対象のAuthoring。</param>
        public static void Disable(FluxParticleAuthoring authoring)
        {
            var generated = authoring.transform.Find("__FluxFXGenerated");
            if (generated != null) DestroyGeneratedRoot(generated);
        }

        /// <summary>
        /// Compiler出力をUdonで扱える一次元配列へ変換し、Runtimeへ設定します。
        /// </summary>
        /// <param name="runtime">設定対象。Initialize前に呼び出します。</param>
        /// <param name="compiled">検証済みAuthoring Program。</param>
        /// <param name="authoring">描画MaterialとBlend設定。</param>
        /// <param name="outputRoot">生成Assetの保存先。Assets配下を指定します。</param>
        public static void Configure(FluxParticleExecutor runtime, FluxParticleAuthoringProgram compiled, FluxParticleAuthoring authoring, string outputRoot = Root, int bufferCapacity = 4)
        {
            if (!outputRoot.StartsWith("Assets/", StringComparison.Ordinal) || outputRoot.Contains(".."))
            {
                throw new ArgumentException("Output must be under Assets.", nameof(outputRoot));
            }
            var program = compiled.Program;
            var generated = CreateGeneratedRoot(runtime);
            var steps = program.Passes.Select((pass, index) =>
                CreateKernel(generated, $"Step {index}", SaveShader(pass.Source, outputRoot))).ToArray();
            var clears = program.ClearSources.Select((source, index) =>
                CreateKernel(generated, $"Clear {index}", SaveShader(source, outputRoot))).ToArray();
            var maskShader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.tplab.flux.fx/Runtime/Shaders/FluxParticleSpawnMask.shader");
            var keyShader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.tplab.flux.fx/Runtime/Shaders/FluxParticleSpawnKey.shader");
            if (maskShader == null || keyShader == null || ShaderUtil.ShaderHasError(maskShader) || ShaderUtil.ShaderHasError(keyShader))
            {
                throw new InvalidOperationException("SpawnInput generation Shaders were not imported correctly.");
            }
            var maskKernel = CreateKernel(generated, "Spawn Mask", maskShader);
            var keyKernel = CreateKernel(generated, "Spawn Key", keyShader);
            var buffersA = program.Program.StateLayout.Pages.Select(page => CreateBuffer(generated, $"Page {page.Id} A", bufferCapacity)).ToArray();
            var buffersB = program.Program.StateLayout.Pages.Select(page => CreateBuffer(generated, $"Page {page.Id} B", bufferCapacity)).ToArray();
            var maskBuffer = CreateBuffer(generated, "Spawn Mask Buffer", bufferCapacity);
            var keyBuffer = CreateBuffer(generated, "Spawn Key Buffer", bufferCapacity);
            var renderCode = compiled.GenerateRenderer();
            var shader = SaveShader(renderCode, outputRoot);
            var renderSource = authoring.Render.Material;
            var texture = renderSource == null ? null : renderSource.mainTexture;
            var scale = renderSource == null ? Vector2.one : renderSource.mainTextureScale;
            var offset = renderSource == null ? Vector2.zero : renderSource.mainTextureOffset;
            var signature = EditorJsonUtility.ToJson(authoring) + (renderSource == null ? "" : EditorJsonUtility.ToJson(renderSource));
            // A runtime instance must not share a mutable render Material with another Particle System.
            // GlobalObjectId is stable for saved scene/prefab objects; temporary test objects use InstanceID.
            var globalId = GlobalObjectId.GetGlobalObjectIdSlow(runtime.gameObject);
            var identity = (int)globalId.identifierType == 0 ? "Transient:" + runtime.GetInstanceID() : globalId.ToString();
            var rendererPath = outputRoot + "/Render-" + Hash128.Compute(renderCode + signature + identity) + ".mat";
            var render = AssetDatabase.LoadAssetAtPath<Material>(rendererPath);
            if (render == null)
            {
                render = new Material(shader);
                render.mainTexture = texture;
                render.mainTextureScale = scale;
                render.mainTextureOffset = offset;
                var blend = authoring.Render.BlendMode;
                render.SetFloat("_SrcBlend", blend == FluxParticleBlendMode.Opaque ? 1 : 5);
                render.SetFloat("_DstBlend", blend == FluxParticleBlendMode.Alpha ? 10 : blend == FluxParticleBlendMode.Additive ? 1 : 0);
                render.SetFloat("_ZWrite", blend == FluxParticleBlendMode.Opaque ? 1 : 0);
                render.renderQueue = blend == FluxParticleBlendMode.Opaque ? 2000 : 3000;
                AssetDatabase.CreateAsset(render, rendererPath);
            }
            var offsets = new List<int> { 0 };
            var pages = new List<int>();
            var versions = new List<bool>();
            var properties = new List<string>();
            foreach (var pass in program.Passes)
            {
                foreach (var read in pass.Reads)
                {
                    pages.Add(read.PageId);
                    versions.Add(read.Version == FluxParticlePageVersion.Updated);
                    properties.Add(FluxParticleHlslNames.PageProperty(read.PageId, read.Version));
                }
                offsets.Add(pages.Count);
            }
            runtime.SetProgramVariable("stepKernels", steps);
            runtime.SetProgramVariable("clearKernels", clears);
            runtime.SetProgramVariable("spawnMaskKernel", maskKernel);
            runtime.SetProgramVariable("spawnKeyKernel", keyKernel);
            runtime.SetProgramVariable("pageBuffersA", buffersA);
            runtime.SetProgramVariable("pageBuffersB", buffersB);
            runtime.SetProgramVariable("spawnMaskBuffer", maskBuffer);
            runtime.SetProgramVariable("spawnKeyBuffer", keyBuffer);
            runtime.SetProgramVariable("renderMaterial", render);
            runtime.SetProgramVariable("outputPages", program.Passes.Select(pass => pass.Plan.OutputPageId).ToArray());
            runtime.SetProgramVariable("spawnOnly", program.Passes.Select(pass => pass.Plan.Condition == FluxParticlePassCondition.HasSpawn).ToArray());
            runtime.SetProgramVariable("readOffsets", offsets.ToArray());
            runtime.SetProgramVariable("readPages", pages.ToArray());
            runtime.SetProgramVariable("readUpdated", versions.ToArray());
            runtime.SetProgramVariable("readProperties", properties.ToArray());
            runtime.SetProgramVariable("renderPageProperties", program.Program.StateLayout.Pages.Select(page => FluxParticleHlslNames.PageProperty(page.Id, FluxParticlePageVersion.Updated)).ToArray());
            runtime.SetProgramVariable("parameterIds", program.Parameters.Select(parameter => parameter.Id).ToArray());
            runtime.SetProgramVariable("parameterProperties", program.Parameters.Select(parameter => FluxParticleHlslNames.ParameterProperty(parameter.Id)).ToArray());
            runtime.SetProgramVariable("parameterTypes", program.Parameters.Select(parameter => (int)parameter.Type).ToArray());
            runtime.SetProgramVariable("staticParameters", program.Parameters.Select(parameter => parameter.Policy == FluxParticleParameterPolicy.Static).ToArray());
            runtime.SetProgramVariable("parameterValues", program.Parameters.Select(parameter => compiled.InitialValues[parameter.Id]).ToArray());
            runtime.SetProgramVariable("deltaTimeProperty", FluxParticleHlslNames.ProviderProperty("step.delta-time"));
            runtime.SetProgramVariable("spawnKeyProperty", FluxParticleHlslNames.ProviderProperty("spawn.key"));
        }

        static Transform CreateGeneratedRoot(FluxParticleExecutor executor)
        {
            var existing = executor.transform.Find("__FluxFXGenerated");
            if (existing != null) DestroyGeneratedRoot(existing);
            var root = new GameObject("__FluxFXGenerated");
            root.transform.SetParent(executor.transform, false);
            return root.transform;
        }

        static void DestroyGeneratedRoot(Transform generated)
        {
            foreach (var kernel in generated.GetComponentsInChildren<FluxKernel>(true))
            {
                var material = kernel.GetProgramVariable("material") as Material;
                if (material != null && !AssetDatabase.Contains(material)) Object.DestroyImmediate(material);
            }
            Object.DestroyImmediate(generated.gameObject);
        }

        static FluxKernel CreateKernel(Transform parent, string name, Shader shader)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var kernel = go.AddUdonSharpComponent<FluxKernel>();
            kernel.SetProgramVariable("shader", shader);
            return kernel;
        }

        static FluxBuffer CreateBuffer(Transform parent, string name, int count)
        {
            if (count < 1 || count > 1048576)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var buffer = go.AddUdonSharpComponent<FluxBuffer>();
            buffer.SetProgramVariable("initialCount", count);
            return buffer;
        }

        static Shader SaveShader(string source, string outputRoot)
        {
            Directory.CreateDirectory(outputRoot);
            var path = outputRoot + "/" + Hash128.Compute(source) + ".shader";
            if (!File.Exists(path))
            {
                File.WriteAllText(path, source);
            }
            else if (File.ReadAllText(path) != source)
            {
                throw new InvalidOperationException($"Generated Shader was modified: {path}");
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null || ShaderUtil.ShaderHasError(shader))
            {
                throw new InvalidOperationException($"Program Shader failed to compile: {path}");
            }
            return shader;
        }

    }
}
