using System;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Preview
{
    public sealed partial class FluxParticlePreview : IDisposable
    {
        const int SpawnSeedPeriod = 1048576;
        const int MaxUInt16VertexCount = 65535;
        const float MinDuration = 0.01f;

        readonly FluxParticleSystem _particleSystem;
        readonly FluxParticleAuthoring _authoring;
        readonly FluxParticleLutBaker _lutBaker = new FluxParticleLutBaker();

        GameObject _previewObject;
        MeshFilter _meshFilter;
        MeshRenderer _meshRenderer;

        Mesh _mesh;

        Material _initializeMaterial;
        Material _velocityMaterial;
        Material _positionMaterial;
        Material _visualMaterial;
        Material _renderMaterial;

        RenderTexture _positionA;
        RenderTexture _positionB;
        RenderTexture _velocityA;
        RenderTexture _velocityB;
        RenderTexture _visualA;
        RenderTexture _visualB;

        RenderTexture _currentPosition;
        RenderTexture _nextPosition;
        RenderTexture _currentVelocity;
        RenderTexture _nextVelocity;
        RenderTexture _currentVisual;
        RenderTexture _nextVisual;

        Texture2D _colorOverLifetimeLut;
        Texture2D _sizeOverLifetimeLut;
        Texture2D _rotationOverLifetimeLut;

        double _previousTime;

        float _emissionAccumulator;
        float _emissionTime;
        float _simulationTime;
        float _startDelayRemaining;
        int _nextBurstIndex;
        int _spawnCursor;
        int _spawnSeed;
        bool _emissionCompleted;

        int _particleCount;
        int _textureSize;

        FluxParticleRenderMode _currentRenderMode;
        Mesh _currentSourceMesh;
        Material _currentSourceMaterial;

        public bool IsPlaying { get; private set; }

        public FluxParticlePreview(FluxParticleSystem particleSystem, FluxParticleAuthoring authoring)
        {
            _particleSystem = particleSystem;
            _authoring = authoring;
        }

        public void Play()
        {
            Stop();

            if (_authoring.Render.Material == null)
            {
                Logger.LogWarning("Preview requires a particle material.", _particleSystem);
                return;
            }

            if (_authoring.Render.Mode == FluxParticleRenderMode.Mesh && _authoring.Render.Mesh == null)
            {
                Logger.LogWarning("Preview requires a mesh for Mesh render mode.", _particleSystem);
                return;
            }

            if (!CreateMaterials())
            {
                DisposeMaterials();
                return;
            }

            _particleCount = _particleSystem.ParticleCount;
            _textureSize = Mathf.CeilToInt(Mathf.Sqrt(_particleCount));

            CreateTextures();
            CreateLuts();
            InitializeTextures();
            CreatePreviewObject();

            _emissionAccumulator = 0;
            _emissionTime = 0;
            _simulationTime = 0;
            _startDelayRemaining = Mathf.Max(0, _authoring.Playback.StartDelay);
            _nextBurstIndex = 0;
            _spawnCursor = 0;
            _spawnSeed = 0;
            _emissionCompleted = false;
            _previousTime = EditorApplication.timeSinceStartup;

            IsPlaying = true;

            EditorApplication.update += Update;
            SceneView.RepaintAll();
        }

        public void Restart()
        {
            Play();
        }

        public void Stop()
        {
            if (IsPlaying)
            {
                EditorApplication.update -= Update;
            }

            IsPlaying = false;

            DisposePreviewObject();
            DisposeTextures();
            DisposeLuts();
            DisposeMaterials();

            _emissionAccumulator = 0;
            _emissionTime = 0;
            _simulationTime = 0;
            _startDelayRemaining = 0;
            _nextBurstIndex = 0;
            _spawnCursor = 0;
            _spawnSeed = 0;
            _emissionCompleted = false;
            _currentSourceMesh = null;
            _currentSourceMaterial = null;
        }

        public void Dispose()
        {
            Stop();
        }

        void Update()
        {
            if (!IsPlaying) return;

            if (_particleSystem.ParticleCount != _particleCount)
            {
                Play();
                return;
            }

            if (_particleSystem == null || _authoring == null)
            {
                Stop();
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Stop();
                return;
            }

            if (_authoring.Render.Material != _currentSourceMaterial)
            {
                if (!UpdatePreviewMaterial()) return;
            }

            var currentTime = EditorApplication.timeSinceStartup;
            var deltaTime = Mathf.Min((float)(currentTime - _previousTime), 0.05f);
            _previousTime = currentTime;

            if (deltaTime <= 0) return;

            _simulationTime += deltaTime;

            UpdatePreviewMesh();
            UpdatePreviewTransform();
            Simulate(deltaTime);

            SceneView.RepaintAll();
        }
    }
}