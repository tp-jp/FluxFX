using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Preview
{
    public sealed partial class FluxParticlePreview
    {
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
                GetStartColorRange(out var startColorMin, out var startColorMax);

                ApplyVisualParameters(
                    spawnStart,
                    spawnSeedStart,
                    spawnCount,
                    startColorMin,
                    startColorMax);

                Dispatch(_visualMaterial, _currentVisual, _nextVisual);

                ApplyRotationParameters(
                    spawnStart,
                    spawnSeedStart,
                    spawnCount,
                    startColorMin.a,
                    startColorMax.a);

                Dispatch(_rotationMaterial, _currentRotation, _nextRotation);

                SwapVisual();
            }

            SwapSimulation();

            _spawnCursor = (_spawnCursor + spawnCount) % _particleCount;
            _spawnSeed = (_spawnSeed + spawnCount) % SpawnSeedPeriod;

            ApplyRenderState();
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

            if (_authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.RandomBetweenTwoConstants)
            {
                speedMin = Mathf.Min(_authoring.InitialVelocity.Min, _authoring.InitialVelocity.Max);
                speedMax = Mathf.Max(_authoring.InitialVelocity.Min, _authoring.InitialVelocity.Max);
            }

            var gravity = GetGravity();
            var force = GetForce();
            var drag = _authoring.Drag.Enabled ? _authoring.Drag.Drag : 0;
            var noiseStrength = _authoring.Noise.Enabled ? _authoring.Noise.Strength : 0;
            var noiseScale = _authoring.Noise.Enabled ? _authoring.Noise.Scale : 0;
            var noiseTime = _authoring.Noise.Enabled ? _simulationTime * _authoring.Noise.Speed : 0;
            var vortexCenter = _authoring.Vortex.Enabled ? _authoring.Vortex.Center : Vector3.zero;
            var vortexAxis = _authoring.Vortex.Enabled ? _authoring.Vortex.Axis.normalized : Vector3.zero;
            var vortexStrength = _authoring.Vortex.Enabled ? _authoring.Vortex.Strength : 0;
            var maxSpeed = _authoring.LimitVelocity.Enabled ? _authoring.LimitVelocity.MaxSpeed : 0;
            var systemRotation = _particleSystem.transform.rotation;

            _velocityMaterial.SetFloat("_DeltaTime", deltaTime);
            _velocityMaterial.SetFloat("_SpawnStart", spawnStart);
            _velocityMaterial.SetFloat("_SpawnSeedStart", spawnSeedStart);
            _velocityMaterial.SetFloat("_SpawnCount", spawnCount);
            _velocityMaterial.SetFloat("_LifetimeMin", lifetimeMin);
            _velocityMaterial.SetFloat("_LifetimeMax", lifetimeMax);
            _velocityMaterial.SetFloat("_InitialSpeedMin", speedMin);
            _velocityMaterial.SetFloat("_InitialSpeedMax", lifetimeMax);
            _velocityMaterial.SetFloat("_ShapeType", (int)_authoring.Shape.Type);
            _velocityMaterial.SetFloat("_ShapeAngle", _authoring.Shape.Angle);
            _velocityMaterial.SetFloat("_SimulationSpace", (int)_authoring.SimulationSpace);
            _velocityMaterial.SetVector("_SystemRotation", new Vector4(systemRotation.x, systemRotation.y, systemRotation.z, systemRotation.w));
            _velocityMaterial.SetVector("_Gravity", gravity);
            _velocityMaterial.SetVector("_Force", force);
            _velocityMaterial.SetFloat("_Drag", drag);
            _velocityMaterial.SetFloat("_NoiseStrength", noiseStrength);
            _velocityMaterial.SetFloat("_NoiseScale", noiseScale);
            _velocityMaterial.SetFloat("_NoiseTime", noiseTime);
            _velocityMaterial.SetVector("_VortexCenter", vortexCenter);
            _velocityMaterial.SetVector("_VortexAxis", vortexAxis);
            _velocityMaterial.SetFloat("_VortexStrength", vortexStrength);
            _velocityMaterial.SetFloat("_MaxSpeed", maxSpeed);
            _velocityMaterial.SetTexture("_PositionTex", _currentPosition);
        }

        Vector3 GetGravity()
        {
            foreach (var module in _authoring.Modules)
            {
                if (!(module is FluxParticleGravityModule gravityModule)) continue;

                return gravityModule.Enabled ? gravityModule.Gravity : Vector3.zero;
            }

            return Vector3.zero;
        }

        Vector3 GetForce()
        {
            foreach (var module in _authoring.Modules)
            {
                if (!(module is FluxParticleForceModule forceModule)) continue;

                return forceModule.Enabled ? forceModule.Force : Vector3.zero;
            }

            return Vector3.zero;
        }

        void ApplyPositionParameters(float deltaTime, int spawnStart, int spawnSeedStart, int spawnCount)
        {
            var shapeSize = _authoring.Shape.Size;
            var velocityOverLifetime = _authoring.VelocityOverLifetime;
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
            _positionMaterial.SetVector("_SystemPosition", new Vector4(systemPosition.x, systemPosition.y, systemPosition.z, 0));
            _positionMaterial.SetVector("_SystemRotation", new Vector4(systemRotation.x, systemRotation.y, systemRotation.z, systemRotation.w));
            _positionMaterial.SetVector("_SystemScale", new Vector4(systemScale.x, systemScale.y, systemScale.z, 0));
            _positionMaterial.SetFloat("_VelocityOverLifetimeEnabled", velocityOverLifetime.Enabled ? 1.0f : 0.0f);
            _positionMaterial.SetVector("_VelocityOverLifetimeStart", velocityOverLifetime.Start);
            _positionMaterial.SetVector("_VelocityOverLifetimeEnd", velocityOverLifetime.End);
            _positionMaterial.SetVector("_VelocityOverLifetimeOrbital", velocityOverLifetime.Orbital);
            _positionMaterial.SetVector("_VelocityOverLifetimeOffset", velocityOverLifetime.Offset);
            _positionMaterial.SetFloat("_VelocityOverLifetimeRadial", velocityOverLifetime.Radial);
            _positionMaterial.SetTexture("_VelocityTex", _nextVelocity);
            _positionMaterial.SetTexture("_CurrentVelocityTex", _currentVelocity);
        }

        void ApplyVisualParameters(int spawnStart, int spawnSeedStart, int spawnCount, Color startColorMin, Color startColorMax)
        {
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

        void ApplyRotationParameters(int spawnStart, int spawnSeedStart, int spawnCount, float startAlphaMin, float startAlphaMax)
        {
            _rotationMaterial.SetVector("_StartRotation", _authoring.Render.StartRotation);
            _rotationMaterial.SetFloat("_StartAlphaMin", startAlphaMin);
            _rotationMaterial.SetFloat("_StartAlphaMax", startAlphaMax);
            _rotationMaterial.SetFloat("_SpawnStart", spawnStart);
            _rotationMaterial.SetFloat("_SpawnSeedStart", spawnSeedStart);
            _rotationMaterial.SetFloat("_SpawnCount", spawnCount);
            _rotationMaterial.SetTexture("_CurrentVelocityTex", _currentVelocity);
            _rotationMaterial.SetTexture("_VelocityTex", _nextVelocity);
        }

        void GetStartColorRange(out Color min, out Color max)
        {
            min = _authoring.Render.StartColor;
            max = _authoring.Render.StartColor;

            if (_authoring.Render.StartColorMode != FluxParticleStartColorMode.RandomBetweenTwoConstants) return;

            min = _authoring.Render.StartColorMin;
            max = _authoring.Render.StartColorMax;
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

            var rotationTemp = _currentRotation;
            _currentRotation = _nextRotation;
            _nextRotation = rotationTemp;
        }
    }
}