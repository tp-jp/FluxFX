using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Preview
{
    public sealed partial class FluxParticlePreview
    {
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
                Logger.LogError("Preview shaders could not be found.", _particleSystem);
                return false;
            }

            _initializeMaterial = new Material(initializeShader);
            _initializeMaterial.hideFlags = HideFlags.HideAndDontSave;

            _velocityMaterial = new Material(velocityShader);
            _velocityMaterial.hideFlags = HideFlags.HideAndDontSave;

            _positionMaterial = new Material(positionShader);
            _positionMaterial.hideFlags = HideFlags.HideAndDontSave;

            _visualMaterial = new Material(visualShader);
            _visualMaterial.hideFlags = HideFlags.HideAndDontSave;

            _currentSourceMaterial = _authoring.Render.Material;
            _renderMaterial = new Material(_currentSourceMaterial);
            _renderMaterial.hideFlags = HideFlags.HideAndDontSave;

            return true;
        }

        bool UpdatePreviewMaterial()
        {
            var sourceMaterial = _authoring.Render.Material;

            if (sourceMaterial == null)
            {
                Logger.LogWarning("Preview requires a particle material.", _particleSystem);
                Stop();
                return false;
            }

            DisposeMaterial(ref _renderMaterial);

            _currentSourceMaterial = sourceMaterial;
            _renderMaterial = new Material(_currentSourceMaterial);
            _renderMaterial.hideFlags = HideFlags.HideAndDontSave;

            _meshRenderer.sharedMaterial = _renderMaterial;

            ApplyRenderState();

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

        void CreateLuts()
        {
            _colorOverLifetimeLut = _lutBaker.CreatePreviewLut("FluxFX Preview Color over Lifetime");
            _sizeOverLifetimeLut = _lutBaker.CreatePreviewLut("FluxFX Preview Size over Lifetime");
            _rotationOverLifetimeLut = _lutBaker.CreatePreviewLut("FluxFX Preview Rotation over Lifetime");
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

        void DisposeLuts()
        {
            DisposeTexture(ref _colorOverLifetimeLut);
            DisposeTexture(ref _sizeOverLifetimeLut);
            DisposeTexture(ref _rotationOverLifetimeLut);
        }

        void DisposeTexture(ref Texture2D texture)
        {
            if (texture == null) return;

            Object.DestroyImmediate(texture);
            texture = null;
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