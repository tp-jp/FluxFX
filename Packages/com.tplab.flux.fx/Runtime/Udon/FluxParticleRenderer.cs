using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// コンパイル済みParticle Programの描画用Meshを生成して設定します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleRenderer : UdonSharpBehaviour
    {
        const int MaxUInt16VertexCount = 65535;

        [SerializeField]
        MeshFilter meshFilter;

        [SerializeField]
        MeshRenderer meshRenderer;

        [SerializeField]
        Mesh sourceMesh;

        [SerializeField]
        Bounds cullingBounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        Mesh _programMesh;

        internal int MaximumParticleCount => FluxParticleCapacity.GetMaximumCount(sourceMesh);

        /// <summary>
        /// 粒子ごとの描画用Meshを作成し、コンパイル済みの描画Materialを設定します。
        /// </summary>
        /// <param name="capacity">Particle数</param>
        /// <param name="material">このParticle System専用の描画Material</param>
        internal void InitializeProgram(int capacity, Material material)
        {
            _programMesh = sourceMesh == null ? CreateParticleMesh(capacity) : CreateParticleMesh(sourceMesh, capacity);
            meshFilter.sharedMesh = _programMesh;
            meshRenderer.sharedMaterial = material;
        }

        void OnDestroy()
        {
            if (_programMesh != null) Destroy(_programMesh);
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
            mesh.bounds = cullingBounds;

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
            mesh.bounds = cullingBounds;

            return mesh;
        }
    }
}