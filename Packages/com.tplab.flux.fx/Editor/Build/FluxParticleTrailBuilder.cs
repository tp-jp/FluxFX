using System;
using System.Linq;
using System.Globalization;
using TpLab.Flux.FX.Editor.Compilation.ShaderGeneration;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// RuntimeとPreviewで共有する軌跡の記録・描画リソースを構成します。
    /// </summary>
    internal static class FluxParticleTrailBuilder
    {
        internal static (FluxKernel Kernel, FluxBuffer[] Buffers, Material[] Materials, Mesh Mesh, MeshRenderer[] Renderers, int Capacity) Create(
            Transform parent, FluxParticleBuildResult build, FluxParticleAuthoring authoring, int count,
            FluxParticleGenerationSession session, string owner, Action<Object> own = null)
        {
            var module = authoring.Modules.OfType<FluxParticleTrailsModule>().SingleOrDefault();
            if (module == null) return (null, null, null, null, null, 0);
            var tracked = Math.Min(count, module.MaxTrails);
            var maximum = FluxParticleCapacity.GetMaximumCount(SystemInfo.maxTextureSize, SystemInfo.supports32bitsIndexBuffer,
                4, 6, SystemInfo.maxGraphicsBufferSize, 28);
            if (tracked < 1 || tracked > maximum) throw new InvalidOperationException("Trail capacity exceeds Texture or Mesh limits.");
            var preview = session == null;
            var source = FluxParticleTrailShaderGenerator.Generate(build.Shaders);
            var shader = preview ? ShaderUtil.CreateShaderAsset(source, true) : FluxParticleProgramBuild.SaveShader(source, session, owner);
            if (preview) own(shader);
            if (ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Trail capture Shader failed to compile.");
            var renderShader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.tplab.flux.fx/Runtime/Shaders/FluxParticleTrail.shader");
            if (renderShader == null || ShaderUtil.ShaderHasError(renderShader)) throw new InvalidOperationException("Trail render Shader failed to compile.");
            var group = new GameObject("Trails");
            group.transform.SetParent(parent, false);
            var buffers = new FluxBuffer[module.PointsPerTrail + 2];
            for (var i = 0; i < buffers.Length; i++)
            {
                buffers[i] = FluxParticleSpawnAllocationBuilder.CreateBuffer(group.transform, "History " + i, tracked, preview);
            }
            var kernel = FluxParticleSpawnAllocationBuilder.CreateKernel(group.transform, "Capture", shader, preview);
            if (preview)
            {
                var kernelMaterial = new Material(shader);
                own(kernelMaterial);
                kernel.SetProgramVariable("material", kernelMaterial);
            }
            var materials = new Material[module.PointsPerTrail - 1];
            var renderers = new MeshRenderer[preview ? 0 : materials.Length];
            var bounds = authoring.Render.CullingBounds;
            Mesh mesh;
            if (preview)
            {
                mesh = CreateMesh(tracked, bounds);
                own(mesh);
            }
            else
            {
                var values = new[] { bounds.center.x, bounds.center.y, bounds.center.z, bounds.size.x, bounds.size.y, bounds.size.z };
                var signature = "trail-segments-v2:" + tracked + ":" + string.Join(",", values.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));
                var meshPath = session.MeshRoot + "/Trail-" + Hash128.Compute(signature) + ".asset";
                FluxParticleGenerationSession.EnsureFolder(session.MeshRoot);
                mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (mesh == null)
                {
                    mesh = CreateMesh(tracked, bounds);
                    AssetDatabase.CreateAsset(mesh, meshPath);
                }
                session.Record(meshPath, "Mesh", owner);
            }
            for (var segment = 0; segment < materials.Length; segment++)
            {
                string path = null;
                Material material = null;
                if (!preview)
                {
                    FluxParticleGenerationSession.EnsureFolder(session.MaterialRoot);
                    path = session.MaterialRoot + "/Trail-" + Hash128.Compute(owner) + "-" + segment + ".mat";
                    material = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
                if (material == null)
                {
                    material = new Material(renderShader);
                    if (preview) own(material);
                    else AssetDatabase.CreateAsset(material, path);
                }
                material.shader = renderShader;
                materials[segment] = material;
                if (!preview) session.Record(path, "Material", owner);
                if (!preview)
                {
                    var go = new GameObject("Segment " + segment);
                    go.layer = authoring.gameObject.layer;
                    go.transform.SetParent(group.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderers[segment] = renderer;
                    renderer.enabled = false;
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    EditorUtility.SetDirty(material);
                }
            }
            return (kernel, buffers, materials, mesh, renderers, tracked);
        }

        static Mesh CreateMesh(int count, Bounds bounds)
        {
            var vertices = new Vector3[count * 4];
            var uv = new Vector2[vertices.Length];
            var indices = new Vector2[vertices.Length];
            var triangles = new int[count * 6];
            var corners = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            var order = new[] { 0, 1, 2, 0, 2, 3 };
            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    uv[i * 4 + j] = corners[j];
                    indices[i * 4 + j] = new Vector2(i, 0);
                }
                for (var j = 0; j < 6; j++)
                {
                    triangles[i * 6 + j] = i * 4 + order[j];
                }
            }
            return new Mesh { name = "FluxFX Trail Segments", indexFormat = count * 4 > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16, vertices = vertices, uv = uv, uv2 = indices, triangles = triangles, bounds = bounds };
        }
    }
}
