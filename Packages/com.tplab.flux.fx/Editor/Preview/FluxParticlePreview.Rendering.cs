using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEngine;

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

        void ApplyColorOverLifetime()
        {
            var settings = _authoring.Render.ColorOverLifetime;

            _renderMaterial.SetFloat("_ColorOverLifetimeEnabled", settings.Enabled ? 1 : 0);
            _renderMaterial.SetFloat("_ColorOverLifetimeMode", (int)settings.Mode);
            _renderMaterial.SetVector("_EndColor", settings.EndColor);

            if (!settings.Enabled || settings.Mode != FluxParticleColorOverLifetimeMode.Gradient)
            {
                _renderMaterial.SetTexture("_ColorOverLifetimeLut", null);
                return;
            }

            _lutBaker.UpdateColorOverLifetimeLut(_colorOverLifetimeLut, settings);
            _renderMaterial.SetTexture("_ColorOverLifetimeLut", _colorOverLifetimeLut);
        }

        void ApplySizeOverLifetime()
        {
            var settings = _authoring.Render.SizeOverLifetime;

            _renderMaterial.SetFloat("_SizeOverLifetimeEnabled", settings.Enabled ? 1 : 0);
            _renderMaterial.SetFloat("_SizeOverLifetimeMode", (int)settings.Mode);
            _renderMaterial.SetFloat("_EndSize", settings.EndSize);

            if (!settings.Enabled || settings.Mode != FluxParticleSizeOverLifetimeMode.Curve)
            {
                _renderMaterial.SetTexture("_SizeOverLifetimeLut", null);
                return;
            }

            _lutBaker.UpdateSizeOverLifetimeLut(_sizeOverLifetimeLut, settings);
            _renderMaterial.SetTexture("_SizeOverLifetimeLut", _sizeOverLifetimeLut);
        }

        void ApplyRotationOverLifetime()
        {
            var settings = _authoring.Render.RotationOverLifetime;

            _renderMaterial.SetFloat("_RotationOverLifetimeEnabled", settings.Enabled ? 1 : 0);
            _renderMaterial.SetFloat("_RotationOverLifetimeMode", (int)settings.Mode);
            _renderMaterial.SetVector("_EndRotation", settings.EndRotation);

            if (!settings.Enabled || settings.Mode != FluxParticleRotationOverLifetimeMode.Curve)
            {
                _renderMaterial.SetTexture("_RotationOverLifetimeLut", null);
                return;
            }

            _lutBaker.UpdateRotationOverLifetimeLut(_rotationOverLifetimeLut, settings);
            _renderMaterial.SetTexture("_RotationOverLifetimeLut", _rotationOverLifetimeLut);
        }

        void ApplyTextureSheetAnimation()
        {
            var settings = _authoring.Render.TextureSheetAnimation;

            _renderMaterial.SetFloat("_TextureSheetAnimationEnabled", settings.Enabled ? 1 : 0);
            _renderMaterial.SetFloat("_TextureSheetTilesX", settings.TilesX);
            _renderMaterial.SetFloat("_TextureSheetTilesY", settings.TilesY);
            _renderMaterial.SetFloat("_TextureSheetCycles", settings.Cycles);
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