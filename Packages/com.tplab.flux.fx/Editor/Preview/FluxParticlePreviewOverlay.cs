using TpLab.Flux.FX.Editor.Inspector;
using UnityEditor;
using UnityEditor.Overlays;

namespace TpLab.Flux.FX.Editor.Preview
{
    /// <summary>
    /// SceneViewにParticleプレビューの操作と生存粒子数を表示します。
    /// </summary>
    [Overlay(typeof(SceneView), "FluxFX.Preview", "FluxFX Preview", true,
        defaultDockZone = DockZone.RightColumn, defaultDockPosition = DockPosition.Bottom, defaultLayout = Layout.Panel)]
    public sealed class FluxParticlePreviewOverlay : IMGUIOverlay, ITransientOverlay
    {
        /// <summary>
        /// Particle SystemのInspectorを表示しているか
        /// </summary>
        public bool visible => FluxParticleSystemEditor.PreviewEditor != null &&
            FluxParticleSystemEditor.PreviewEditor.HasScenePreview;

        public override void OnGUI()
        {
            if (!visible) return;
            FluxParticleSystemEditor.PreviewEditor.DrawScenePreviewControls();
        }
    }
}
