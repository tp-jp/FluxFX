using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleAuthoringEditor
    {
        void DrawSystemFollow()
        {
            var follow = serializedObject.FindProperty("systemFollow");
            var target = follow.FindPropertyRelative("target");
            EditorGUILayout.Space(4);
            DrawEnum(target, "System Follow", "Moves the System GameObject and its children. Scale is preserved. Configured before Play Mode.");
            if (target.intValue == (int)FluxParticleFollowTarget.Off) return;
            var specified = target.intValue == (int)FluxParticleFollowTarget.SpecifiedTransform;
            if (specified)
            {
                var reference = follow.FindPropertyRelative("targetTransform");
                EditorGUILayout.PropertyField(reference, new GUIContent(L10n.Tr("Follow Transform")));
                var transform = reference.objectReferenceValue as Transform;
                if (transform == null || transform == ParticleSystem.transform || transform.IsChildOf(ParticleSystem.transform))
                {
                    EditorGUILayout.HelpBox(L10n.Tr("Choose a Follow Transform outside this System's hierarchy."), MessageType.Error);
                }
            }
            EditorGUILayout.PropertyField(follow.FindPropertyRelative("positionOffset"), new GUIContent(L10n.Tr("Follow Offset")));
            DrawEnum(follow.FindPropertyRelative("offsetSpace"), "Follow Offset Space");
            DrawEnum(follow.FindPropertyRelative("rotation"), "Follow Rotation");
            if (CanEditAuthoring) EditorGUILayout.HelpBox(L10n.Tr("Preview starts at the placed position. Choose its follow source in the SceneView Preview controls."), MessageType.Info);
            EditorGUILayout.HelpBox(L10n.Tr("World simulation leaves emitted particles in the world; Local simulation moves them with the System. Culling Bounds must also cover particles and trails left behind."), MessageType.Info);
        }

        void DrawPreviewFollowControls()
        {
            if (((FluxParticleAuthoring)target).SystemFollow.Target == FluxParticleFollowTarget.Off) return;
            var follow = _particlePreview.Follow;
            EditorGUILayout.Space(4);
            GUILayout.Label(new GUIContent(L10n.Tr("Preview Follow"), L10n.Tr("Preview only. Does not change runtime settings or scene Transforms.")));
            var mode = (FluxParticlePreviewFollowMode)EditorGUILayout.Popup((int)follow.Mode, new[]
            {
                L10n.Tr("Fixed at Placement"), L10n.Tr("Follow Scene Camera"), L10n.Tr("Follow Specified Transform")
            }, GUILayout.Width(288));
            var previewTarget = follow.Target;
            if (mode == FluxParticlePreviewFollowMode.SpecifiedTransform)
            {
                previewTarget = (Transform)EditorGUILayout.ObjectField(previewTarget, typeof(Transform), true, GUILayout.Width(288));
            }
            if (mode != follow.Mode || previewTarget != follow.Target)
            {
                follow.Configure(mode, previewTarget);
                SceneView.RepaintAll();
            }
            if (mode == FluxParticlePreviewFollowMode.Placement) return;
            using (new EditorGUI.DisabledScope(!follow.HasTarget && !follow.IsFrozen))
            {
                if (GUILayout.Button(L10n.Tr(follow.IsFrozen ? "Release Follow Position" : "Freeze Follow Position"), GUILayout.Width(288)))
                {
                    follow.SetFrozen(!follow.IsFrozen);
                    SceneView.RepaintAll();
                }
            }
            if (follow.IsFrozen) GUILayout.Label(L10n.Tr("Follow position frozen; simulation continues."));
            else if (!follow.HasTarget) GUILayout.Label(L10n.Tr("Preview follow target is unavailable."));
        }
    }
}
