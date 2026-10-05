using UnityEditor;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor
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

            _gravityExpanded = DrawModule(
                L10n.Tr("Gravity"),
                _gravity,
                _gravityExpanded,
                GravityExpandedKey);

            _forceExpanded = DrawModule(
                L10n.Tr("Force"),
                _force,
                _forceExpanded,
                ForceExpandedKey);

            _dragExpanded = DrawModule(
                L10n.Tr("Drag"),
                _drag,
                _dragExpanded,
                DragExpandedKey);

            _noiseExpanded = DrawModule(
                L10n.Tr("Noise"),
                _noise,
                _noiseExpanded,
                NoiseExpandedKey);

            _vortexExpanded = DrawModule(
                L10n.Tr("Vortex"),
                _vortex,
                _vortexExpanded,
                VortexExpandedKey);

            _limitVelocityExpanded = DrawModule(
                L10n.Tr("Limit Velocity"),
                _limitVelocity,
                _limitVelocityExpanded,
                LimitVelocityExpandedKey);
        }
    }
}