using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;
using UnityEngine.Rendering;

namespace TpLab.Flux.FX.Editor.Preview
{
    public sealed partial class FluxParticlePreview
    {
        void ApplyRenderState()
        {
            ApplyColorOverLifetime();
            ApplySizeOverLifetime();
            ApplyRotationOverLifetime();
            ApplyTextureSheetAnimation();
            ApplyBlendMode();

            _renderMaterial.SetTexture("_PositionTex", _currentPosition);
            _renderMaterial.SetTexture("_VelocityTex", _currentVelocity);
            _renderMaterial.SetTexture("_VisualTex", _currentVisual);
            _renderMaterial.SetTexture("_RotationTex", _currentRotation);
            _renderMaterial.SetFloat("_FluxSourceCount", _particleCount);
            _renderMaterial.SetFloat("_FluxSourceWidth", _textureSize);
            _renderMaterial.SetFloat("_FluxSourceHeight", _textureSize);
            _renderMaterial.SetFloat("_SimulationSpace", (int)_authoring.SimulationSpace);
            _renderMaterial.SetFloat("_RenderMode", (int)_authoring.Render.Mode);
        }

        void ApplyBlendMode()
        {
            var blendMode = _authoring.Render.BlendMode;

            if (blendMode == FluxParticleBlendMode.Alpha)
            {
                _renderMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                _renderMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                _renderMaterial.SetFloat("_ZWrite", 0);
                _renderMaterial.renderQueue = (int)RenderQueue.Transparent;
                return;
            }

            if (blendMode == FluxParticleBlendMode.Additive)
            {
                _renderMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                _renderMaterial.SetFloat("_DstBlend", (float)BlendMode.One);
                _renderMaterial.SetFloat("_ZWrite", 0);
                _renderMaterial.renderQueue = (int)RenderQueue.Transparent;
                return;
            }

            _renderMaterial.SetFloat("_SrcBlend", (float)BlendMode.One);
            _renderMaterial.SetFloat("_DstBlend", (float)BlendMode.Zero);
            _renderMaterial.SetFloat("_ZWrite", 1);
            _renderMaterial.renderQueue = (int)RenderQueue.Geometry;
        }

        void ApplyColorOverLifetime()
        {
            FluxParticleColorOverLifetimeModule colorModule = null;

            foreach (var module in _authoring.Modules)
            {
                if (!(module is FluxParticleColorOverLifetimeModule found)) continue;

                colorModule = found;
                break;
            }

            if (colorModule == null || !colorModule.Enabled)
            {
                _renderMaterial.SetFloat("_ColorOverLifetimeEnabled", 0);
                _renderMaterial.SetFloat("_ColorOverLifetimeMode", 0);
                _renderMaterial.SetVector("_EndColor", Color.white);
                _renderMaterial.SetTexture("_ColorOverLifetimeLut", null);
                return;
            }

            _renderMaterial.SetFloat("_ColorOverLifetimeEnabled", 1);
            _renderMaterial.SetFloat("_ColorOverLifetimeMode", (int)colorModule.Mode);
            _renderMaterial.SetVector("_EndColor", colorModule.EndColor);

            if (colorModule.Mode != FluxParticleColorOverLifetimeMode.Gradient)
            {
                _renderMaterial.SetTexture("_ColorOverLifetimeLut", null);
                return;
            }

            _lutBaker.UpdateColorOverLifetimeLut(_colorOverLifetimeLut, colorModule);
            _renderMaterial.SetTexture("_ColorOverLifetimeLut", _colorOverLifetimeLut);
        }

        void ApplySizeOverLifetime()
        {
            FluxParticleSizeOverLifetimeModule sizeModule = null;

            foreach (var module in _authoring.Modules)
            {
                if (!(module is FluxParticleSizeOverLifetimeModule found)) continue;

                sizeModule = found;
                break;
            }

            if (sizeModule == null || !sizeModule.Enabled)
            {
                _renderMaterial.SetFloat("_SizeOverLifetimeEnabled", 0);
                _renderMaterial.SetFloat("_SizeOverLifetimeMode", 0);
                _renderMaterial.SetFloat("_EndSize", 0);
                _renderMaterial.SetTexture("_SizeOverLifetimeLut", null);
                return;
            }

            _renderMaterial.SetFloat("_SizeOverLifetimeEnabled", 1);
            _renderMaterial.SetFloat("_SizeOverLifetimeMode", (int)sizeModule.Mode);
            _renderMaterial.SetFloat("_EndSize", sizeModule.EndSize);

            if (sizeModule.Mode != FluxParticleSizeOverLifetimeMode.Curve)
            {
                _renderMaterial.SetTexture("_SizeOverLifetimeLut", null);
                return;
            }

            _lutBaker.UpdateSizeOverLifetimeLut(_sizeOverLifetimeLut, sizeModule);
            _renderMaterial.SetTexture("_SizeOverLifetimeLut", _sizeOverLifetimeLut);
        }

        void ApplyRotationOverLifetime()
        {
            FluxParticleRotationOverLifetimeModule rotationModule = null;

            foreach (var module in _authoring.Modules)
            {
                if (!(module is FluxParticleRotationOverLifetimeModule found)) continue;

                rotationModule = found;
                break;
            }

            if (rotationModule == null || !rotationModule.Enabled)
            {
                _renderMaterial.SetFloat("_RotationOverLifetimeEnabled", 0);
                _renderMaterial.SetFloat("_RotationOverLifetimeMode", 0);
                _renderMaterial.SetVector("_EndRotation", Vector3.zero);
                _renderMaterial.SetTexture("_RotationOverLifetimeLut", null);
                return;
            }

            _renderMaterial.SetFloat("_RotationOverLifetimeEnabled", 1);
            _renderMaterial.SetFloat("_RotationOverLifetimeMode", (int)rotationModule.Mode);
            _renderMaterial.SetVector("_EndRotation", rotationModule.EndRotation);

            if (rotationModule.Mode != FluxParticleRotationOverLifetimeMode.Curve)
            {
                _renderMaterial.SetTexture("_RotationOverLifetimeLut", null);
                return;
            }

            _lutBaker.UpdateRotationOverLifetimeLut(_rotationOverLifetimeLut, rotationModule);
            _renderMaterial.SetTexture("_RotationOverLifetimeLut", _rotationOverLifetimeLut);
        }

        void ApplyTextureSheetAnimation()
        {
            FluxParticleTextureSheetAnimationModule textureSheetModule = null;

            foreach (var module in _authoring.Modules)
            {
                if (!(module is FluxParticleTextureSheetAnimationModule found)) continue;

                textureSheetModule = found;
                break;
            }

            if (textureSheetModule == null || !textureSheetModule.Enabled)
            {
                _renderMaterial.SetFloat("_TextureSheetAnimationEnabled", 0);
                _renderMaterial.SetFloat("_TextureSheetTilesX", 1);
                _renderMaterial.SetFloat("_TextureSheetTilesY", 1);
                _renderMaterial.SetFloat("_TextureSheetCycles", 1);
                return;
            }

            _renderMaterial.SetFloat("_TextureSheetAnimationEnabled", 1);
            _renderMaterial.SetFloat("_TextureSheetTilesX", textureSheetModule.TilesX);
            _renderMaterial.SetFloat("_TextureSheetTilesY", textureSheetModule.TilesY);
            _renderMaterial.SetFloat("_TextureSheetCycles", textureSheetModule.Cycles);
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
    }
}
