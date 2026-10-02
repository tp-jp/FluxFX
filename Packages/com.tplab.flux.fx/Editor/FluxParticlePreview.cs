using System;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor
{
    public sealed class FluxParticlePreview : IDisposable
    {
        const int MaxPreviewParticleCount = 65536;
        const int SpawnSeedPeriod = 1048576;

        readonly FluxParticleSystem _particleSystem;
        readonly FluxParticleAuthoring _authoring;

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

        double _previousTime;

        float _emissionAccumulator;
        float _simulationTime;
        int _spawnCursor;
        int _spawnSeed;

        int _particleCount;
        int _textureSize;

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
                Debug.LogWarning("[FluxFX] Preview requires a particle material.", _particleSystem);
                return;
            }

            if (!CreateMaterials())
            {
                DisposeMaterials();
                return;
            }

            _particleCount = Mathf.Min(_particleSystem.ParticleCount, MaxPreviewParticleCount);
            _textureSize = Mathf.CeilToInt(Mathf.Sqrt(_particleCount));

            CreateTextures();
            InitializeTextures();
            CreatePreviewObject();

            _emissionAccumulator = 0;
            _simulationTime = 0;
            _spawnCursor = 0;
            _spawnSeed = 0;
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
            DisposeMaterials();

            _emissionAccumulator = 0;
            _simulationTime = 0;
            _spawnCursor = 0;
            _spawnSeed = 0;
        }

        public void Dispose()
        {
            Stop();
        }

        void Update()
        {
            if (!IsPlaying) return;

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

            var currentTime = EditorApplication.timeSinceStartup;
            var deltaTime = Mathf.Min((float)(currentTime - _previousTime), 0.05f);
            _previousTime = currentTime;

            if (deltaTime <= 0) return;

            _simulationTime += deltaTime;

            UpdatePreviewTransform();
            Simulate(deltaTime);

            SceneView.RepaintAll();
        }

        void Simulate(float deltaTime)
        {
            var spawnCount = UpdateEmission(deltaTime);
            var spawnStart = _spawnCursor;
            var spawnSeedStart = _spawnSeed;

            ApplyVelocityParameters(deltaTime, spawnStart, spawnSeedStart, spawnCount);
            Dispatch(_velocityMaterial, _currentVelocity, _nextVelocity);

            ApplyPositionParameters(deltaTime, spawnStart, spawnSeedStart, spawnCount);
            Dispatch(_positionMaterial, _currentPosition, _nextPosition);

            if (spawnCount > 0)
            {
                ApplyVisualParameters(spawnStart, spawnSeedStart, spawnCount);
                Dispatch(_visualMaterial, _currentVisual, _nextVisual);
                SwapVisual();
            }

            SwapSimulation();

            _spawnCursor = (_spawnCursor + spawnCount) % _particleCount;
            _spawnSeed = (_spawnSeed + spawnCount) % SpawnSeedPeriod;

            ApplyRenderState();
        }

        int UpdateEmission(float deltaTime)
        {
            _emissionAccumulator += deltaTime * _authoring.Emission.Rate;

            var spawnCount = Mathf.FloorToInt(_emissionAccumulator);

            if (spawnCount > 0)
            {
                _emissionAccumulator -= spawnCount;
            }

            return spawnCount;
        }

        void ApplyVelocityParameters(float deltaTime, int spawnStart, int spawnSeedStart, int spawnCount)
        {
            var lifetimeMin = _authoring.Lifetime.Lifetime;
            var lifetimeMax = _authoring.Lifetime.Lifetime;

            if (_authoring.Lifetime.Mode == FluxParticleStartLifetimeMode.RandomBetweenTwoConstants)
            {
                lifetimeMin = Mathf.Min(_authoring.Lifetime.Min, _authoring.Lifetime.Max);
                lifetimeMax = Mathf.Max(_authoring.Lifetime.Min, _authoring.Lifetime.Max);
            }

            var speedMin = _authoring.InitialVelocity.Speed;
            var speedMax = _authoring.InitialVelocity.Speed;

            if (_authoring.InitialVelocity.Mode == FluxParticleInitialSpeedMode.RandomBetweenTwoConstants)
            {
                speedMin = Mathf.Min(_authoring.InitialVelocity.Min, _authoring.InitialVelocity.Max);
                speedMax = Mathf.Max(_authoring.InitialVelocity.Min, _authoring.InitialVelocity.Max);
            }

            var gravity = _authoring.Gravity.Enabled
                ? _authoring.Gravity.Gravity
                : Vector3.zero;

            var drag = _authoring.Drag.Enabled
                ? _authoring.Drag.Drag
                : 0;

            var noiseStrength = _authoring.Noise.Enabled
                ? _authoring.Noise.Strength
                : 0;

            var noiseScale = _authoring.Noise.Enabled
                ? _authoring.Noise.Scale
                : 0;

            var noiseTime = _authoring.Noise.Enabled
                ? _simulationTime * _authoring.Noise.Speed
                : 0;

            var vortexCenter = _authoring.Vortex.Enabled
                ? _authoring.Vortex.Center
                : Vector3.zero;

            var vortexAxis = _authoring.Vortex.Enabled
                ? _authoring.Vortex.Axis
                : Vector3.zero;

            var vortexStrength = _authoring.Vortex.Enabled
                ? _authoring.Vortex.Strength
                : 0;

            var systemRotation = _particleSystem.transform.rotation;

            _velocityMaterial.SetFloat("_DeltaTime", deltaTime);
            _velocityMaterial.SetFloat("_SpawnStart", spawnStart);
            _velocityMaterial.SetFloat("_SpawnSeedStart", spawnSeedStart);
            _velocityMaterial.SetFloat("_SpawnCount", spawnCount);
            _velocityMaterial.SetFloat("_LifetimeMin", lifetimeMin);
            _velocityMaterial.SetFloat("_LifetimeMax", lifetimeMax);
            _velocityMaterial.SetFloat("_InitialSpeedMin", speedMin);
            _velocityMaterial.SetFloat("_InitialSpeedMax", speedMax);
            _velocityMaterial.SetFloat("_ShapeType", (int)_authoring.Shape.Type);
            _velocityMaterial.SetFloat("_SimulationSpace", (int)_authoring.SimulationSpace);
            _velocityMaterial.SetVector(
                "_SystemRotation",
                new Vector4(systemRotation.x, systemRotation.y, systemRotation.z, systemRotation.w));
            _velocityMaterial.SetVector("_Gravity", gravity);
            _velocityMaterial.SetFloat("_Drag", drag);
            _velocityMaterial.SetFloat("_NoiseStrength", noiseStrength);
            _velocityMaterial.SetFloat("_NoiseScale", noiseScale);
            _velocityMaterial.SetFloat("_NoiseTime", noiseTime);
            _velocityMaterial.SetVector("_VortexCenter", vortexCenter);
            _velocityMaterial.SetVector("_VortexAxis", vortexAxis);
            _velocityMaterial.SetFloat("_VortexStrength", vortexStrength);
            _velocityMaterial.SetTexture("_PositionTex", _currentPosition);
        }

        void ApplyPositionParameters(float deltaTime, int spawnStart, int spawnSeedStart, int spawnCount)
        {
            var shapeSize = _authoring.Shape.Size;

            var systemTransform = _particleSystem.transform;
            var systemPosition = systemTransform.position;
            var systemRotation = systemTransform.rotation;
            var systemScale = systemTransform.lossyScale;

            _positionMaterial.SetFloat("_DeltaTime", deltaTime);
            _positionMaterial.SetFloat("_SpawnStart", spawnStart);
            _positionMaterial.SetFloat("_SpawnSeedStart", spawnSeedStart);
            _positionMaterial.SetFloat("_SpawnCount", spawnCount);
            _positionMaterial.SetFloat("_ShapeType", (int)_authoring.Shape.Type);
            _positionMaterial.SetFloat("_ShapeRadius", _authoring.Shape.Radius);
            _positionMaterial.SetVector("_ShapeSize", new Vector4(shapeSize.x, shapeSize.y, shapeSize.z, 0));
            _positionMaterial.SetFloat("_SimulationSpace", (int)_authoring.SimulationSpace);
            _positionMaterial.SetVector(
                "_SystemPosition",
                new Vector4(systemPosition.x, systemPosition.y, systemPosition.z, 0));
            _positionMaterial.SetVector(
                "_SystemRotation",
                new Vector4(systemRotation.x, systemRotation.y, systemRotation.z, systemRotation.w));
            _positionMaterial.SetVector(
                "_SystemScale",
                new Vector4(systemScale.x, systemScale.y, systemScale.z, 0));
            _positionMaterial.SetTexture("_VelocityTex", _nextVelocity);
            _positionMaterial.SetTexture("_CurrentVelocityTex", _currentVelocity);
        }

        void ApplyVisualParameters(int spawnStart, int spawnSeedStart, int spawnCount)
        {
            var startColorMin = _authoring.Render.StartColor;
            var startColorMax = _authoring.Render.StartColor;

            if (_authoring.Render.StartColorMode == FluxParticleStartColorMode.RandomBetweenTwoConstants)
            {
                startColorMin = _authoring.Render.StartColorMin;
                startColorMax = _authoring.Render.StartColorMax;
            }

            var startSizeMin = _authoring.Render.StartSize;
            var startSizeMax = _authoring.Render.StartSize;

            if (_authoring.Render.StartSizeMode == FluxParticleStartSizeMode.RandomBetweenTwoConstants)
            {
                startSizeMin = Mathf.Min(_authoring.Render.StartSizeMin, _authoring.Render.StartSizeMax);
                startSizeMax = Mathf.Max(_authoring.Render.StartSizeMin, _authoring.Render.StartSizeMax);
            }

            _visualMaterial.SetFloat("_SpawnStart", spawnStart);
            _visualMaterial.SetFloat("_SpawnSeedStart", spawnSeedStart);
            _visualMaterial.SetFloat("_SpawnCount", spawnCount);
            _visualMaterial.SetVector("_StartColorMin", startColorMin);
            _visualMaterial.SetVector("_StartColorMax", startColorMax);
            _visualMaterial.SetFloat("_StartSizeMin", startSizeMin);
            _visualMaterial.SetFloat("_StartSizeMax", startSizeMax);
            _visualMaterial.SetTexture("_CurrentVelocityTex", _currentVelocity);
            _visualMaterial.SetTexture("_VelocityTex", _nextVelocity);
        }

        void ApplyRenderState()
        {
            _renderMaterial.SetTexture("_PositionTex", _currentPosition);
            _renderMaterial.SetTexture("_VelocityTex", _currentVelocity);
            _renderMaterial.SetTexture("_VisualTex", _currentVisual);

            _renderMaterial.SetFloat("_FluxSourceCount", _particleCount);
            _renderMaterial.SetFloat("_FluxSourceWidth", _textureSize);
            _renderMaterial.SetFloat("_FluxSourceHeight", _textureSize);

            _renderMaterial.SetFloat("_SimulationSpace", (int)_authoring.SimulationSpace);
            _renderMaterial.SetFloat("_RenderMode", (int)FluxParticleRenderMode.Billboard);

            _renderMaterial.SetFloat("_ColorOverLifetimeEnabled", 0);
            _renderMaterial.SetFloat("_SizeOverLifetimeEnabled", 0);
            _renderMaterial.SetFloat("_RotationOverLifetimeEnabled", 0);
        }

        bool CreateMaterials()
        {
            var initializeShader = Shader.Find("FluxFX/ParticleInitialize");
            var velocityShader = Shader.Find("FluxFX/ParticleVelocityUpdate");
            var positionShader = Shader.Find("FluxFX/ParticlePositionUpdate");
            var visualShader = Shader.Find("FluxFX/ParticleVisualUpdate");

            if (initializeShader == null ||
                velocityShader == null ||
                positionShader == null ||
                visualShader == null)
            {
                Debug.LogError("[FluxFX] Preview shaders could not be found.", _particleSystem);
                return false;
            }

            _initializeMaterial = new Material(initializeShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            _velocityMaterial = new Material(velocityShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            _positionMaterial = new Material(positionShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            _visualMaterial = new Material(visualShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            _renderMaterial = new Material(_authoring.Render.Material)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            return true;
        }

        void CreateTextures()
        {
            _positionA = CreateStateTexture("FluxFX Preview Position A");
            _positionB = CreateStateTexture("FluxFX Preview Position B");
            _velocityA = CreateStateTexture("FluxFX Preview Velocity A");
            _velocityB = CreateStateTexture("FluxFX Preview Velocity B");
            _visualA = CreateStateTexture("FluxFX Preview Visual A");
            _visualB = CreateStateTexture("FluxFX Preview Visual B");

            _currentPosition = _positionA;
            _nextPosition = _positionB;
            _currentVelocity = _velocityA;
            _nextVelocity = _velocityB;
            _currentVisual = _visualA;
            _nextVisual = _visualB;
        }

        RenderTexture CreateStateTexture(string name)
        {
            var texture = new RenderTexture(
                _textureSize,
                _textureSize,
                0,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);

            texture.name = name;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.useMipMap = false;
            texture.autoGenerateMips = false;
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.Create();

            return texture;
        }

        void InitializeTextures()
        {
            DispatchInitialize(_positionA);
            DispatchInitialize(_positionB);
            DispatchInitialize(_velocityA);
            DispatchInitialize(_velocityB);
            DispatchInitialize(_visualA);
            DispatchInitialize(_visualB);
        }

        void DispatchInitialize(RenderTexture destination)
        {
            PrepareDestination(_initializeMaterial);

            Graphics.Blit(null, destination, _initializeMaterial);
        }

        void Dispatch(Material material, RenderTexture source, RenderTexture destination)
        {
            PrepareSource(material);
            PrepareDestination(material);

            Graphics.Blit(source, destination, material);
        }

        void PrepareSource(Material material)
        {
            material.SetFloat("_FluxSourceCount", _particleCount);
            material.SetFloat("_FluxSourceWidth", _textureSize);
            material.SetFloat("_FluxSourceHeight", _textureSize);
        }

        void PrepareDestination(Material material)
        {
            material.SetFloat("_FluxDestinationCount", _particleCount);
            material.SetFloat("_FluxDestinationWidth", _textureSize);
            material.SetFloat("_FluxDestinationHeight", _textureSize);
        }

        void CreatePreviewObject()
        {
            _previewObject = new GameObject("FluxFX Preview");
            _previewObject.hideFlags = HideFlags.HideAndDontSave;

            _meshFilter = _previewObject.AddComponent<MeshFilter>();
            _meshRenderer = _previewObject.AddComponent<MeshRenderer>();

            _mesh = CreateBillboardMesh(_particleCount);
            _meshFilter.sharedMesh = _mesh;
            _meshRenderer.sharedMaterial = _renderMaterial;

            UpdatePreviewTransform();
            ApplyRenderState();
        }

        Mesh CreateBillboardMesh(int capacity)
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

            var mesh = new Mesh
            {
                name = "FluxFX Preview Mesh",
                hideFlags = HideFlags.HideAndDontSave
            };

            if (vertexCount > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.uv2 = uv2;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

            return mesh;
        }

        void UpdatePreviewTransform()
        {
            if (_previewObject == null) return;

            var previewTransform = _previewObject.transform;

            if (_authoring.SimulationSpace == FluxParticleSimulationSpace.Local)
            {
                var sourceTransform = _particleSystem.transform;

                previewTransform.position = sourceTransform.position;
                previewTransform.rotation = sourceTransform.rotation;
                previewTransform.localScale = sourceTransform.lossyScale;
            }
            else
            {
                previewTransform.position = Vector3.zero;
                previewTransform.rotation = Quaternion.identity;
                previewTransform.localScale = Vector3.one;
            }
        }

        void SwapSimulation()
        {
            var positionTemp = _currentPosition;
            _currentPosition = _nextPosition;
            _nextPosition = positionTemp;

            var velocityTemp = _currentVelocity;
            _currentVelocity = _nextVelocity;
            _nextVelocity = velocityTemp;
        }

        void SwapVisual()
        {
            var visualTemp = _currentVisual;
            _currentVisual = _nextVisual;
            _nextVisual = visualTemp;
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

        void DisposeTextures()
        {
            DisposeTexture(ref _positionA);
            DisposeTexture(ref _positionB);
            DisposeTexture(ref _velocityA);
            DisposeTexture(ref _velocityB);
            DisposeTexture(ref _visualA);
            DisposeTexture(ref _visualB);

            _currentPosition = null;
            _nextPosition = null;
            _currentVelocity = null;
            _nextVelocity = null;
            _currentVisual = null;
            _nextVisual = null;
        }

        void DisposeTexture(ref RenderTexture texture)
        {
            if (texture == null) return;

            texture.Release();
            Object.DestroyImmediate(texture);
            texture = null;
        }

        void DisposeMaterials()
        {
            DisposeMaterial(ref _initializeMaterial);
            DisposeMaterial(ref _velocityMaterial);
            DisposeMaterial(ref _positionMaterial);
            DisposeMaterial(ref _visualMaterial);
            DisposeMaterial(ref _renderMaterial);
        }

        void DisposeMaterial(ref Material material)
        {
            if (material == null) return;

            Object.DestroyImmediate(material);
            material = null;
        }
    }
}