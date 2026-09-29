using TpLab.Flux.FX.Udon;
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
        FluxParticleRenderer particleRenderer;

        FluxBuffer _currentBuffer;
        FluxBuffer _nextBuffer;

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

            particleRenderer.Initialize(ParticleCount);
            particleRenderer.SetPositionBuffer(_currentBuffer);
        }

        void Update()
        {
            updateKernel.SetFloat("_DeltaTime", Time.deltaTime);
            updateKernel.Dispatch(_currentBuffer, _nextBuffer);

            var temp = _currentBuffer;
            _currentBuffer = _nextBuffer;
            _nextBuffer = temp;

            particleRenderer.SetPositionBuffer(_currentBuffer);
        }
    }
}