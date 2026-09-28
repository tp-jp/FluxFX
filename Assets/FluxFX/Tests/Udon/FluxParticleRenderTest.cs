using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Udon
{
    public class FluxParticleRenderTest : UdonSharpBehaviour
    {
        const int ParticleCount = 64;
        const int Columns = 8;
        const float Spacing = 0.3f;
        const float Height = 1.2f;

        [SerializeField]
        FluxBuffer positionBufferA;

        [SerializeField]
        FluxBuffer positionBufferB;

        [SerializeField]
        FluxUpload upload;

        [SerializeField]
        FluxKernel updateKernel;

        [SerializeField]
        MeshFilter meshFilter;

        [SerializeField]
        MeshRenderer meshRenderer;

        FluxBuffer _currentBuffer;
        FluxBuffer _nextBuffer;
        Material _renderMaterial;

        void Start()
        {
            positionBufferA.SetCount(ParticleCount);
            positionBufferB.SetCount(ParticleCount);

            var positions = new Vector4[ParticleCount];

            for (var i = 0; i < ParticleCount; i++)
            {
                var x = i % Columns;
                var y = i / Columns;

                positions[i] = new Vector4(
                    (x - 3.5f) * Spacing,
                    Height + (y - 3.5f) * Spacing,
                    0,
                    1
                );
            }

            upload.Upload(positions, positionBufferA);

            _currentBuffer = positionBufferA;
            _nextBuffer = positionBufferB;

            meshFilter.mesh = CreateParticleMesh();

            _renderMaterial = meshRenderer.material;
            ApplyRenderBuffer();
        }

        void Update()
        {
            updateKernel.SetFloat("_DeltaTime", Time.deltaTime);
            updateKernel.Dispatch(_currentBuffer, _nextBuffer);

            var temp = _currentBuffer;
            _currentBuffer = _nextBuffer;
            _nextBuffer = temp;

            ApplyRenderBuffer();
        }

        Mesh CreateParticleMesh()
        {
            var vertices = new Vector3[ParticleCount * 4];
            var uv2 = new Vector2[ParticleCount * 4];
            var triangles = new int[ParticleCount * 6];

            for (var i = 0; i < ParticleCount; i++)
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
            mesh.name = "Flux Particle Test Mesh";
            mesh.vertices = vertices;
            mesh.uv2 = uv2;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

            return mesh;
        }

        void ApplyRenderBuffer()
        {
            var texture = _currentBuffer.Texture;

            _renderMaterial.SetTexture("_PositionTex", texture);
            _renderMaterial.SetFloat("_FluxSourceCount", _currentBuffer.Count);
            _renderMaterial.SetFloat("_FluxSourceWidth", texture.width);
            _renderMaterial.SetFloat("_FluxSourceHeight", texture.height);
        }
    }
}