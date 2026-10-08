using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleSystemEditor
    {
        void DrawVelocity()
        {
            _velocityExpanded = DrawSectionHeader(L10n.Tr("VELOCITY"), _velocityExpanded, VelocityExpandedKey);
            if (!_velocityExpanded) return;

            EditorGUILayout.Space(2);

            _velocityOverLifetimeExpanded = DrawModule(
                L10n.Tr("Velocity over Lifetime"),
                _velocityOverLifetime,
                _velocityOverLifetimeExpanded,
                VelocityOverLifetimeExpandedKey);

            _gravityExpanded = DrawOptionalModule<FluxParticleGravityModule>(
                L10n.Tr("Gravity"),
                _gravityExpanded,
                GravityExpandedKey);

            _forceExpanded = DrawOptionalModule<FluxParticleForceModule>(
                L10n.Tr("Force"),
                _forceExpanded,
                ForceExpandedKey);

            _dragExpanded = DrawOptionalModule<FluxParticleDragModule>(
                L10n.Tr("Drag"),
                _dragExpanded,
                DragExpandedKey);

            _noiseExpanded = DrawOptionalModule<FluxParticleNoiseModule>(
                L10n.Tr("Noise"),
                _noiseExpanded,
                NoiseExpandedKey);

            _vortexExpanded = DrawOptionalModule<FluxParticleVortexModule>(
                L10n.Tr("Vortex"),
                _vortexExpanded,
                VortexExpandedKey);

            _limitVelocityExpanded = DrawOptionalModule<FluxParticleLimitVelocityModule>(
                L10n.Tr("Limit Velocity"),
                _limitVelocityExpanded,
                LimitVelocityExpandedKey);
        }
    }
}