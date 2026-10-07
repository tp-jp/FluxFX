using TpLab.Flux.FX.Udon;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Preview
{
    public sealed partial class FluxParticlePreview
    {
        void CreatePreviewObject()
        {
            _previewObject = new GameObject("FluxFX Preview");
            _previewObject.hideFlags = HideFlags.HideAndDontSave;

            _meshFilter = _previewObject.AddComponent<MeshFilter>();
            _meshRenderer = _previewObject.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = _renderMaterial;

            RebuildPreviewMesh();
            UpdatePreviewTransform();
            ApplyRenderState();
        }

        void UpdatePreviewMesh()
        {
            var renderMode = _authoring.Render.Mode;
            var sourceMesh = _authoring.Render.Mesh;

            if (renderMode == _currentRenderMode && sourceMesh == _currentSourceMesh) return;

            RebuildPreviewMesh();
        }

        void RebuildPreviewMesh()
        {
            if (_mesh != null)
            {
                Object.DestroyImmediate(_mesh);
                _mesh = null;
            }

            _currentRenderMode = _authoring.Render.Mode;
            _currentSourceMesh = _authoring.Render.Mesh;

            if (_currentRenderMode == FluxParticleRenderMode.Mesh && _currentSourceMesh != null)
            {
                _mesh = CreateParticleMesh(_currentSourceMesh, _particleCount);
            }
            else
            {
                _mesh = CreateParticleMesh(_particleCount);
            }

            _meshFilter.sharedMesh = _mesh;
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
            mesh.name = "FluxFX Preview Mesh";
            mesh.hideFlags = HideFlags.HideAndDontSave;

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
            mesh.name = "FluxFX Preview Mesh";
            mesh.hideFlags = HideFlags.HideAndDontSave;

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

        void DisposePreviewObject()
        {
            if (_previewObject != null)
            {
                Object.DestroyImmediate(_previewObject);
                _previewObject = null;
            }

            if (_mesh != null)
            {
                Object.DestroyImmediate(_mesh);
                _mesh = null;
            }

            _meshFilter = null;
            _meshRenderer = null;
        }
    }
}