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
        float _endBillboardRotation;
        float _rotationOverLifetimeEnabled;
        float _rotationOverLifetimeMode;
        float _simulationSpace;
        object _startRotation;
        object _colorOverLifetime;
        object _sizeOverLifetime;

        FluxParticleStartRotationParameters StartRotation => (FluxParticleStartRotationParameters)_startRotation;

        FluxParticleColorOverLifetimeParameters ColorOverLifetime => (FluxParticleColorOverLifetimeParameters)_colorOverLifetime;

        FluxParticleSizeOverLifetimeParameters SizeOverLifetime => (FluxParticleSizeOverLifetimeParameters)_sizeOverLifetime;

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
            FluxParticleStartRotationParameters startRotation,
            FluxParticleColorOverLifetimeParameters colorOverLifetime,
            FluxParticleSizeOverLifetimeParameters sizeOverLifetime)
        {
            _startRotation = startRotation;
            _colorOverLifetime = colorOverLifetime;
            _sizeOverLifetime = sizeOverLifetime;

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

                _endBillboardRotation = (float)rotation["endRotation"].Double;
                _rotationOverLifetimeEnabled = 1;
                _rotationOverLifetimeMode = 0;

                if (rotation.TryGetValue("mode", out var modeToken))
                {
                    _rotationOverLifetimeMode = (float)modeToken.Double;
                }
            }
            else
            {
                _endBillboardRotation = 0;
                _rotationOverLifetimeEnabled = 0;
                _rotationOverLifetimeMode = 0;
            }
        }

        internal void SetState(FluxParticleState state)
        {
            SetBuffer("_PositionTex", state.CurrentPosition);
            SetBuffer("_VelocityTex", state.CurrentVelocity);
            SetBuffer("_VisualTex", state.CurrentVisual);

            var texture = state.CurrentPosition.Texture;
            var endColor = ColorOverLifetime.GetEndColor();

            _material.SetFloat("_FluxSourceCount", state.CurrentPosition.Count);
            _material.SetFloat("_FluxSourceWidth", texture.width);
            _material.SetFloat("_FluxSourceHeight", texture.height);
            _material.SetVector("_StartRotation", StartRotation.GetRotation());
            _material.SetVector("_EndColor", endColor);
            _material.SetFloat("_EndSize", SizeOverLifetime.GetEndSize());
            _material.SetFloat("_ColorOverLifetimeEnabled", ColorOverLifetime.GetEnabled() ? 1.0f : 0.0f);
            _material.SetFloat("_SizeOverLifetimeEnabled", SizeOverLifetime.GetEnabled() ? 1.0f : 0.0f);
        }

        void ApplyRenderParameters()
        {
            var mode = (int)renderMode;

            _material.SetFloat("_ColorOverLifetimeMode", _colorOverLifetimeMode);
            _material.SetFloat("_SizeOverLifetimeMode", _sizeOverLifetimeMode);
            _material.SetFloat("_EndBillboardRotation", _endBillboardRotation);
            _material.SetFloat("_RotationOverLifetimeEnabled", _rotationOverLifetimeEnabled);
            _material.SetFloat("_RotationOverLifetimeMode", _rotationOverLifetimeMode);
            _material.SetFloat("_SimulationSpace", _simulationSpace);
            _material.SetFloat("_RenderMode", mode);
            _material.SetTexture("_ColorOverLifetimeLut", colorOverLifetimeLut);
            _material.SetTexture("_SizeOverLifetimeLut", sizeOverLifetimeLut);
            _material.SetTexture("_RotationOverLifetimeLut", rotationOverLifetimeLut);
        }

        void SetBuffer(string name, FluxBuffer buffer)
        {
            _material.SetTexture(name, buffer.Texture);
        }

        Mesh CreateParticleMesh(int capacity)
        {
            var vertexCount = capacity * 4;
            var vertices = new Vector3[vertexCount];
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