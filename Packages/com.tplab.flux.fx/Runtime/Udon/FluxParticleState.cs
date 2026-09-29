using JetBrains.Annotations;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleState : UdonSharpBehaviour
    {
        [SerializeField]
        FluxBuffer positionBufferA;

        [SerializeField]
        FluxBuffer positionBufferB;

        [SerializeField]
        FluxBuffer velocityBufferA;

        [SerializeField]
        FluxBuffer velocityBufferB;

        [SerializeField]
        FluxBuffer visualBuffer;

        FluxBuffer _currentPositionBuffer;
        FluxBuffer _nextPositionBuffer;
        FluxBuffer _currentVelocityBuffer;
        FluxBuffer _nextVelocityBuffer;

        [PublicAPI]
        public FluxBuffer CurrentPosition => _currentPositionBuffer;

        [PublicAPI]
        public FluxBuffer NextPosition => _nextPositionBuffer;

        [PublicAPI]
        public FluxBuffer CurrentVelocity => _currentVelocityBuffer;

        [PublicAPI]
        public FluxBuffer NextVelocity => _nextVelocityBuffer;

        [PublicAPI]
        public FluxBuffer Visual => visualBuffer;

        [PublicAPI]
        public void Initialize(int count)
        {
            positionBufferA.SetCount(count);
            positionBufferB.SetCount(count);
            velocityBufferA.SetCount(count);
            velocityBufferB.SetCount(count);
            visualBuffer.SetCount(count);

            _currentPositionBuffer = positionBufferA;
            _nextPositionBuffer = positionBufferB;
            _currentVelocityBuffer = velocityBufferA;
            _nextVelocityBuffer = velocityBufferB;
        }

        [PublicAPI]
        public void Swap()
        {
            var positionTemp = _currentPositionBuffer;
            _currentPositionBuffer = _nextPositionBuffer;
            _nextPositionBuffer = positionTemp;

            var velocityTemp = _currentVelocityBuffer;
            _currentVelocityBuffer = _nextVelocityBuffer;
            _nextVelocityBuffer = velocityTemp;
        }
    }
}