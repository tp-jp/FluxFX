using JetBrains.Annotations;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleRenderer : UdonSharpBehaviour
    {
        [SerializeField]
        MeshFilter meshFilter;

        [SerializeField]
        MeshRenderer meshRenderer;

        Material _material;

        [PublicAPI]
        public void Initialize(int capacity)
        {
            meshFilter.mesh = CreateParticleMesh(capacity);
            _material = meshRenderer.material;
        }

        [PublicAPI]
        public void SetState(FluxParticleState state)
        {
            SetBuffer("_PositionTex", state.CurrentPosition);
            SetBuffer("_VelocityTex", state.CurrentVelocity);
            SetBuffer("_VisualTex", state.Visual);

            var texture = state.CurrentPosition.Texture;

            _material.SetFloat("_FluxSourceCount", state.CurrentPosition.Count);
            _material.SetFloat("_FluxSourceWidth", texture.width);
            _material.SetFloat("_FluxSourceHeight", texture.height);
        }

        void SetBuffer(string name, FluxBuffer buffer)
        {
            _material.SetTexture(name, buffer.Texture);
        }

        Mesh CreateParticleMesh(int capacity)
        {
            var vertices = new Vector3[capacity * 4];
            var uv2 = new Vector2[capacity * 4];
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
            mesh.vertices = vertices;
            mesh.uv2 = uv2;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

            return mesh;
        }
    }
}