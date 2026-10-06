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
        FluxBuffer visualBufferA;

        [SerializeField]
        FluxBuffer visualBufferB;

        [SerializeField]
        FluxBuffer rotationBufferA;

        [SerializeField]
        FluxBuffer rotationBufferB;

        [SerializeField]
        FluxKernel initializeKernel;

        FluxBuffer _currentPositionBuffer;
        FluxBuffer _nextPositionBuffer;
        FluxBuffer _currentVelocityBuffer;
        FluxBuffer _nextVelocityBuffer;
        FluxBuffer _currentVisualBuffer;
        FluxBuffer _nextVisualBuffer;
        FluxBuffer _currentRotationBuffer;
        FluxBuffer _nextRotationBuffer;

        public FluxBuffer CurrentPosition => _currentPositionBuffer;

        public FluxBuffer NextPosition => _nextPositionBuffer;

        public FluxBuffer CurrentVelocity => _currentVelocityBuffer;

        public FluxBuffer NextVelocity => _nextVelocityBuffer;

        public FluxBuffer CurrentVisual => _currentVisualBuffer;

        public FluxBuffer NextVisual => _nextVisualBuffer;

        public FluxBuffer CurrentRotation => _currentRotationBuffer;

        public FluxBuffer NextRotation => _nextRotationBuffer;

        internal void Initialize(int count)
        {
            positionBufferA.SetCount(count);
            positionBufferB.SetCount(count);
            velocityBufferA.SetCount(count);
            velocityBufferB.SetCount(count);
            visualBufferA.SetCount(count);
            visualBufferB.SetCount(count);
            rotationBufferA.SetCount(count);
            rotationBufferB.SetCount(count);

            _currentPositionBuffer = positionBufferA;
            _nextPositionBuffer = positionBufferB;
            _currentVelocityBuffer = velocityBufferA;
            _nextVelocityBuffer = velocityBufferB;
            _currentVisualBuffer = visualBufferA;
            _nextVisualBuffer = visualBufferB;
            _currentRotationBuffer = rotationBufferA;
            _nextRotationBuffer = rotationBufferB;

            Clear();
        }

        internal void Clear()
        {
            initializeKernel.Dispatch(positionBufferA);
            initializeKernel.Dispatch(positionBufferB);
            initializeKernel.Dispatch(velocityBufferA);
            initializeKernel.Dispatch(velocityBufferB);
            initializeKernel.Dispatch(visualBufferA);
            initializeKernel.Dispatch(visualBufferB);
            initializeKernel.Dispatch(rotationBufferA);
            initializeKernel.Dispatch(rotationBufferB);
        }

        internal void SwapSimulation()
        {
            var positionTemp = _currentPositionBuffer;
            _currentPositionBuffer = _nextPositionBuffer;
            _nextPositionBuffer = positionTemp;

            var velocityTemp = _currentVelocityBuffer;
            _currentVelocityBuffer = _nextVelocityBuffer;
            _nextVelocityBuffer = velocityTemp;
        }

        internal void SwapVisual()
        {
            var visualTemp = _currentVisualBuffer;
            _currentVisualBuffer = _nextVisualBuffer;
            _nextVisualBuffer = visualTemp;

            var rotationTemp = _currentRotationBuffer;
            _currentRotationBuffer = _nextRotationBuffer;
            _nextRotationBuffer = rotationTemp;
        }
    }
}