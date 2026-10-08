using TpLab.Flux.FX.Udon.Parameters;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleRenderer : UdonSharpBehaviour
    {
        const int MaxUInt16VertexCount = 65535;

        const float BlendZero = 0;
        const float BlendOne = 1;
        const float BlendSrcAlpha = 5;
        const float BlendOneMinusSrcAlpha = 10;

        [SerializeField]
        MeshFilter meshFilter;

        [SerializeField]
        MeshRenderer meshRenderer;

        [HideInInspector]
        [SerializeField]
        FluxParticleRenderMode renderMode;

        [HideInInspector]
        [SerializeField]
        Mesh sourceMesh;

        [HideInInspector]
        [SerializeField]
        Material particleMaterial;

        [HideInInspector]
        [SerializeField]
        FluxParticleBlendMode blendMode;

        [HideInInspector]
        [SerializeField]
        Texture2D colorOverLifetimeLut;

        [HideInInspector]
        [SerializeField]
        Texture2D sizeOverLifetimeLut;

        [HideInInspector]
        [SerializeField]
        Texture2D rotationOverLifetimeLut;

        Material _material;

        float _colorOverLifetimeMode;
        float _sizeOverLifetimeMode;
        float _rotationOverLifetimeMode;
        float _textureSheetAnimationEnabled;
        float _textureSheetTilesX = 1;
        float _textureSheetTilesY = 1;
        float _textureSheetCycles = 1;
        float _simulationSpace;
        object _colorOverLifetime;
        object _sizeOverLifetime;
        object _rotationOverLifetime;

        FluxParticleColorOverLifetimeParameters ColorOverLifetime => (FluxParticleColorOverLifetimeParameters)_colorOverLifetime;

        FluxParticleSizeOverLifetimeParameters SizeOverLifetime => (FluxParticleSizeOverLifetimeParameters)_sizeOverLifetime;

        FluxParticleRotationOverLifetimeParameters RotationOverLifetime => (FluxParticleRotationOverLifetimeParameters)_rotationOverLifetime;

        internal void Initialize(int capacity)
        {
            if (renderMode == FluxParticleRenderMode.Mesh && sourceMesh != null)
            {
                meshFilter.mesh = CreateParticleMesh(sourceMesh, capacity);
            }
            else
            {
                meshFilter.mesh = CreateParticleMesh(capacity);
            }

            if (particleMaterial != null)
            {
                meshRenderer.sharedMaterial = particleMaterial;
            }

            _material = meshRenderer.material;

            ApplyRenderParameters();
        }

        internal void SetSimulationSpace(int simulationSpace)
        {
            _simulationSpace = simulationSpace;
        }

        internal void SetRenderParameters(
            DataDictionary parameters,
            FluxParticleColorOverLifetimeParameters colorOverLifetime,
            FluxParticleSizeOverLifetimeParameters sizeOverLifetime,
            FluxParticleRotationOverLifetimeParameters rotationOverLifetime)
        {
            _colorOverLifetime = colorOverLifetime;
            _sizeOverLifetime = sizeOverLifetime;
            _rotationOverLifetime = rotationOverLifetime;

            if (parameters.TryGetValue("colorOverLifetime", out var colorToken))
            {
                var color = colorToken.DataDictionary;

                _colorOverLifetimeMode = 0;

                if (color.TryGetValue("mode", out var modeToken))
                {
                    _colorOverLifetimeMode = (float)modeToken.Double;
                }
            }
            else
            {
                _colorOverLifetimeMode = 0;
            }

            if (parameters.TryGetValue("sizeOverLifetime", out var sizeToken))
            {
                var size = sizeToken.DataDictionary;

                _sizeOverLifetimeMode = 0;

                if (size.TryGetValue("mode", out var modeToken))
                {
                    _sizeOverLifetimeMode = (float)modeToken.Double;
                }
            }
            else
            {
                _sizeOverLifetimeMode = 0;
            }

            if (parameters.TryGetValue("rotationOverLifetime", out var rotationToken))
            {
                var rotation = rotationToken.DataDictionary;

                _rotationOverLifetimeMode = 0;

                if (rotation.TryGetValue("mode", out var modeToken))
                {
                    _rotationOverLifetimeMode = (float)modeToken.Double;
                }
            }
            else
            {
                _rotationOverLifetimeMode = 0;
            }

            if (parameters.TryGetValue("textureSheetAnimation", out var textureSheetToken))
            {
                var textureSheet = textureSheetToken.DataDictionary;

                _textureSheetAnimationEnabled = 1;
                _textureSheetTilesX = (float)textureSheet["tilesX"].Double;
                _textureSheetTilesY = (float)textureSheet["tilesY"].Double;
                _textureSheetCycles = (float)textureSheet["cycles"].Double;
            }
            else
            {
                _textureSheetAnimationEnabled = 0;
                _textureSheetTilesX = 1;
                _textureSheetTilesY = 1;
                _textureSheetCycles = 1;
            }
        }

        internal void SetState(FluxParticleState state)
        {
            SetBuffer("_PositionTex", state.CurrentPosition);
            SetBuffer("_VelocityTex", state.CurrentVelocity);
            SetBuffer("_VisualTex", state.CurrentVisual);
            SetBuffer("_RotationTex", state.CurrentRotation);

            var texture = state.CurrentPosition.Texture;
            var endColor = ColorOverLifetime.GetEndColor();

            _material.SetFloat("_FluxSourceCount", state.CurrentPosition.Count);
            _material.SetFloat("_FluxSourceWidth", texture.width);
            _material.SetFloat("_FluxSourceHeight", texture.height);
            _material.SetVector("_EndColor", endColor);
            _material.SetFloat("_EndSize", SizeOverLifetime.GetEndSize());
            _material.SetVector("_EndRotation", RotationOverLifetime.GetEndRotation());
            _material.SetFloat("_ColorOverLifetimeEnabled", ColorOverLifetime.GetEnabled() ? 1.0f : 0.0f);
            _material.SetFloat("_SizeOverLifetimeEnabled", SizeOverLifetime.GetEnabled() ? 1.0f : 0.0f);
            _material.SetFloat("_RotationOverLifetimeEnabled", RotationOverLifetime.GetEnabled() ? 1.0f : 0.0f);
        }

        void ApplyRenderParameters()
        {
            var mode = (int)renderMode;

            _material.SetFloat("_ColorOverLifetimeMode", _colorOverLifetimeMode);
            _material.SetFloat("_SizeOverLifetimeMode", _sizeOverLifetimeMode);
            _material.SetFloat("_RotationOverLifetimeMode", _rotationOverLifetimeMode);
            _material.SetFloat("_TextureSheetAnimationEnabled", _textureSheetAnimationEnabled);
            _material.SetFloat("_TextureSheetTilesX", _textureSheetTilesX);
            _material.SetFloat("_TextureSheetTilesY", _textureSheetTilesY);
            _material.SetFloat("_TextureSheetCycles", _textureSheetCycles);
            _material.SetFloat("_SimulationSpace", _simulationSpace);
            _material.SetFloat("_RenderMode", mode);
            _material.SetTexture("_ColorOverLifetimeLut", colorOverLifetimeLut);
            _material.SetTexture("_SizeOverLifetimeLut", sizeOverLifetimeLut);
            _material.SetTexture("_RotationOverLifetimeLut", rotationOverLifetimeLut);

            ApplyBlendMode();
        }

        void ApplyBlendMode()
        {
            if (blendMode == FluxParticleBlendMode.Alpha)
            {
                _material.SetFloat("_SrcBlend", BlendSrcAlpha);
                _material.SetFloat("_DstBlend", BlendOneMinusSrcAlpha);
                _material.SetFloat("_ZWrite", 0);
                _material.renderQueue = (int)RenderQueue.Transparent;
                return;
            }

            if (blendMode == FluxParticleBlendMode.Additive)
            {
                _material.SetFloat("_SrcBlend", BlendSrcAlpha);
                _material.SetFloat("_DstBlend", BlendOne);
                _material.SetFloat("_ZWrite", 0);
                _material.renderQueue = (int)RenderQueue.Transparent;
                return;
            }

            _material.SetFloat("_SrcBlend", BlendOne);
            _material.SetFloat("_DstBlend", BlendZero);
            _material.SetFloat("_ZWrite", 1);
            _material.renderQueue = (int)RenderQueue.Geometry;
        }

        void SetBuffer(string name, FluxBuffer buffer)
        {
            _material.SetTexture(name, buffer.Texture);
        }

        Mesh CreateParticleMesh(int capacity)
        {
            var vertexCount = capacity * 4;
            var vertices = new Vector3[vertexCount];
            var uv = new Vector2[vertexCount];
            var uv2 = new Vector2[vertexCount];
            var triangles = new int[capacity * 6];

            for (var i = 0; i < capacity; i++)
            {
                var vertexIndex = i * 4;
                var triangleIndex = i * 6;

                vertices[vertexIndex] = new Vector3(-0.05f, -0.05f, 0);
                vertices[vertexIndex + 1] = new Vector3(0.05f, -0.05f, 0);
                vertices[vertexIndex + 2] = new Vector3(0.05f, 0.05f, 0);
                vertices[vertexIndex + 3] = new Vector3(-0.05f, 0.05f, 0);

                uv[vertexIndex] = new Vector2(0, 0);
                uv[vertexIndex + 1] = new Vector2(1, 0);
                uv[vertexIndex + 2] = new Vector2(1, 1);
                uv[vertexIndex + 3] = new Vector2(0, 1);

                var particleIndex = new Vector2(i, 0);

                uv2[vertexIndex] = particleIndex;
                uv2[vertexIndex + 1] = particleIndex;
                uv2[vertexIndex + 2] = particleIndex;
                uv2[vertexIndex + 3] = particleIndex;

                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 1;
                triangles[triangleIndex + 2] = vertexIndex + 2;
                triangles[triangleIndex + 3] = vertexIndex;
                triangles[triangleIndex + 4] = vertexIndex + 2;
                triangles[triangleIndex + 5] = vertexIndex + 3;
            }

            var mesh = new Mesh();
            mesh.name = "Flux Particle Mesh";

            if (vertexCount > MaxUInt16VertexCount)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.uv2 = uv2;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

            return mesh;
        }

        Mesh CreateParticleMesh(Mesh source, int capacity)
        {
            var sourceVertices = source.vertices;
            var sourceNormals = source.normals;
            var sourceTangents = source.tangents;
            var sourceUV = source.uv;
            var sourceTriangles = source.triangles;

            var sourceVertexCount = sourceVertices.Length;
            var sourceTriangleCount = sourceTriangles.Length;
            var vertexCount = sourceVertexCount * capacity;

            var hasNormals = sourceNormals.Length == sourceVertexCount;
            var hasTangents = sourceTangents.Length == sourceVertexCount;
            var hasUV = sourceUV.Length == sourceVertexCount;

            var vertices = new Vector3[vertexCount];
            var normals = hasNormals ? new Vector3[vertexCount] : null;
            var tangents = hasTangents ? new Vector4[vertexCount] : null;
            var uv = hasUV ? new Vector2[vertexCount] : null;
            var uv2 = new Vector2[vertexCount];
            var triangles = new int[sourceTriangleCount * capacity];

            for (var particleIndex = 0; particleIndex < capacity; particleIndex++)
            {
                var vertexOffset = particleIndex * sourceVertexCount;
                var triangleOffset = particleIndex * sourceTriangleCount;
                var particleData = new Vector2(particleIndex, 0);

                for (var i = 0; i < sourceVertexCount; i++)
                {
                    var vertexIndex = vertexOffset + i;

                    vertices[vertexIndex] = sourceVertices[i];
                    uv2[vertexIndex] = particleData;

                    if (hasNormals)
                    {
                        normals[vertexIndex] = sourceNormals[i];
                    }

                    if (hasTangents)
                    {
                        tangents[vertexIndex] = sourceTangents[i];
                    }

                    if (hasUV)
                    {
                        uv[vertexIndex] = sourceUV[i];
                    }
                }

                for (var i = 0; i < sourceTriangleCount; i++)
                {
                    triangles[triangleOffset + i] = vertexOffset + sourceTriangles[i];
                }
            }

            var mesh = new Mesh();
            mesh.name = "Flux Particle Mesh";

            if (vertexCount > MaxUInt16VertexCount)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.vertices = vertices;

            if (hasNormals)
            {
                mesh.normals = normals;
            }

            if (hasTangents)
            {
                mesh.tangents = tangents;
            }

            if (hasUV)
            {
                mesh.uv = uv;
            }

            mesh.uv2 = uv2;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

            return mesh;
        }
    }
}