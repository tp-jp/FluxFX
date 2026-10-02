﻿using JetBrains.Annotations;
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
        FluxKernel initializeKernel;

        FluxBuffer _currentPositionBuffer;
        FluxBuffer _nextPositionBuffer;
        FluxBuffer _currentVelocityBuffer;
        FluxBuffer _nextVelocityBuffer;
        FluxBuffer _currentVisualBuffer;
        FluxBuffer _nextVisualBuffer;

        [PublicAPI]
        public FluxBuffer CurrentPosition => _currentPositionBuffer;

        [PublicAPI]
        public FluxBuffer NextPosition => _nextPositionBuffer;

        [PublicAPI]
        public FluxBuffer CurrentVelocity => _currentVelocityBuffer;

        [PublicAPI]
        public FluxBuffer NextVelocity => _nextVelocityBuffer;

        [PublicAPI]
        public FluxBuffer CurrentVisual => _currentVisualBuffer;

        [PublicAPI]
        public FluxBuffer NextVisual => _nextVisualBuffer;

        [PublicAPI]
        public void Initialize(int count)
        {
            positionBufferA.SetCount(count);
            positionBufferB.SetCount(count);
            velocityBufferA.SetCount(count);
            velocityBufferB.SetCount(count);
            visualBufferA.SetCount(count);
            visualBufferB.SetCount(count);

            initializeKernel.Dispatch(positionBufferA);
            initializeKernel.Dispatch(positionBufferB);
            initializeKernel.Dispatch(velocityBufferA);
            initializeKernel.Dispatch(velocityBufferB);
            initializeKernel.Dispatch(visualBufferA);
            initializeKernel.Dispatch(visualBufferB);

            _currentPositionBuffer = positionBufferA;
            _nextPositionBuffer = positionBufferB;
            _currentVelocityBuffer = velocityBufferA;
            _nextVelocityBuffer = velocityBufferB;
            _currentVisualBuffer = visualBufferA;
            _nextVisualBuffer = visualBufferB;
        }

        [PublicAPI]
        public void SwapSimulation()
        {
            var positionTemp = _currentPositionBuffer;
            _currentPositionBuffer = _nextPositionBuffer;
            _nextPositionBuffer = positionTemp;

            var velocityTemp = _currentVelocityBuffer;
            _currentVelocityBuffer = _nextVelocityBuffer;
            _nextVelocityBuffer = velocityTemp;
        }

        [PublicAPI]
        public void SwapVisual()
        {
            var visualTemp = _currentVisualBuffer;
            _currentVisualBuffer = _nextVisualBuffer;
            _nextVisualBuffer = visualTemp;
        }
    }
}