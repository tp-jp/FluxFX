using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleAuthoringEditor
    {
        const float ConeGizmoLength = 2.0f;

        /// <summary>
        /// Simulation SpaceにかかわらずRendererのローカル座標でカリング範囲を表示します。
        /// </summary>
        void DrawCullingBoundsGizmo()
        {
            if (Event.current.type != EventType.Repaint) return;

            var renderer = ParticleSystem.GetComponentInChildren<FluxParticleRenderer>(true);
            if (renderer == null) return;

            var bounds = _render.FindPropertyRelative("cullingBounds").boundsValue;
            using (new Handles.DrawingScope(new Color(1.0f, 0.65f, 0.15f), renderer.transform.localToWorldMatrix))
            {
                Handles.DrawWireCube(bounds.center, bounds.size);
                Handles.Label(bounds.center + Vector3.up * bounds.extents.y, L10n.Tr("Culling Bounds"));
            }
        }

        void DrawPointShapeGizmo()
        {
            var particleSystem = ParticleSystem;
            var position = particleSystem.transform.position;
            var size = HandleUtility.GetHandleSize(position) * 0.08f;

            Handles.DotHandleCap(
                0,
                position,
                Quaternion.identity,
                size,
                EventType.Repaint);
        }

        void DrawSphereShapeGizmo()
        {
            var particleSystem = ParticleSystem;
            var radius = _shape.FindPropertyRelative("radius");
            var previousMatrix = Handles.matrix;

            Handles.matrix = particleSystem.transform.localToWorldMatrix;

            Handles.DrawWireDisc(Vector3.zero, Vector3.right, radius.floatValue);
            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius.floatValue);
            Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius.floatValue);

            if (!CanEditAuthoring)
            {
                Handles.matrix = previousMatrix;
                return;
            }

            EditorGUI.BeginChangeCheck();

            var newRadius = Handles.RadiusHandle(Quaternion.identity, Vector3.zero, radius.floatValue);

            if (EditorGUI.EndChangeCheck())
            {
                radius.floatValue = Mathf.Max(0, newRadius);
                _authoringObject.ApplyModifiedProperties();
            }

            Handles.matrix = previousMatrix;
        }

        void DrawHemisphereShapeGizmo()
        {
            var particleSystem = ParticleSystem;
            var radius = _shape.FindPropertyRelative("radius");
            var previousMatrix = Handles.matrix;

            Handles.matrix = particleSystem.transform.localToWorldMatrix;

            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius.floatValue);
            Handles.DrawWireArc(Vector3.zero, Vector3.forward, Vector3.right, 180, radius.floatValue);
            Handles.DrawWireArc(Vector3.zero, Vector3.right, Vector3.forward, -180, radius.floatValue);

            if (!CanEditAuthoring)
            {
                Handles.matrix = previousMatrix;
                return;
            }

            EditorGUI.BeginChangeCheck();

            var newRadius = Handles.ScaleSlider(
                radius.floatValue,
                Vector3.zero,
                Vector3.up,
                Quaternion.identity,
                radius.floatValue,
                0);

            if (EditorGUI.EndChangeCheck())
            {
                radius.floatValue = Mathf.Max(0, newRadius);
                _authoringObject.ApplyModifiedProperties();
            }

            Handles.matrix = previousMatrix;
        }

        void DrawCircleShapeGizmo()
        {
            var particleSystem = ParticleSystem;
            var radius = _shape.FindPropertyRelative("radius");
            var previousMatrix = Handles.matrix;

            Handles.matrix = particleSystem.transform.localToWorldMatrix;

            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius.floatValue);

            if (!CanEditAuthoring)
            {
                Handles.matrix = previousMatrix;
                return;
            }

            EditorGUI.BeginChangeCheck();

            var newRadius = Handles.ScaleSlider(
                radius.floatValue,
                Vector3.zero,
                Vector3.up,
                Quaternion.Euler(90, 0, 0),
                radius.floatValue,
                0);

            if (EditorGUI.EndChangeCheck())
            {
                radius.floatValue = Mathf.Max(0, newRadius);
                _authoringObject.ApplyModifiedProperties();
            }

            Handles.matrix = previousMatrix;
        }

        void DrawBoxShapeGizmo()
        {
            var particleSystem = ParticleSystem;
            var size = _shape.FindPropertyRelative("size");
            var previousMatrix = Handles.matrix;

            Handles.matrix = particleSystem.transform.localToWorldMatrix;

            var currentSize = size.vector3Value;
            Handles.DrawWireCube(Vector3.zero, currentSize);

            if (!CanEditAuthoring)
            {
                Handles.matrix = previousMatrix;
                return;
            }

            EditorGUI.BeginChangeCheck();

            var halfSize = currentSize * 0.5f;

            var newHalfX = Handles.ScaleSlider(
                halfSize.x,
                Vector3.zero,
                Vector3.right,
                Quaternion.identity,
                halfSize.x,
                0);

            var newHalfY = Handles.ScaleSlider(
                halfSize.y,
                Vector3.zero,
                Vector3.up,
                Quaternion.identity,
                halfSize.y,
                0);

            var newHalfZ = Handles.ScaleSlider(
                halfSize.z,
                Vector3.zero,
                Vector3.forward,
                Quaternion.identity,
                halfSize.z,
                0);

            if (EditorGUI.EndChangeCheck())
            {
                size.vector3Value = new Vector3(
                    Mathf.Max(0, newHalfX * 2.0f),
                    Mathf.Max(0, newHalfY * 2.0f),
                    Mathf.Max(0, newHalfZ * 2.0f));
                _authoringObject.ApplyModifiedProperties();
            }

            Handles.matrix = previousMatrix;
        }

        void DrawConeShapeGizmo()
        {
            var particleSystem = ParticleSystem;
            var radius = _shape.FindPropertyRelative("radius");
            var angle = _shape.FindPropertyRelative("angle");
            var previousMatrix = Handles.matrix;

            Handles.matrix = particleSystem.transform.localToWorldMatrix;

            var angleRadians = angle.floatValue * Mathf.Deg2Rad;
            var directionRadius = Mathf.Sin(angleRadians) * ConeGizmoLength;
            var directionHeight = Mathf.Cos(angleRadians) * ConeGizmoLength;
            var endRadius = radius.floatValue + directionRadius;
            var directionCenter = Vector3.up * directionHeight;

            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius.floatValue);
            Handles.DrawWireDisc(directionCenter, Vector3.up, endRadius);

            Handles.DrawLine(
                new Vector3(radius.floatValue, 0, 0),
                new Vector3(endRadius, directionHeight, 0));
            Handles.DrawLine(
                new Vector3(-radius.floatValue, 0, 0),
                new Vector3(-endRadius, directionHeight, 0));
            Handles.DrawLine(
                new Vector3(0, 0, radius.floatValue),
                new Vector3(0, directionHeight, endRadius));
            Handles.DrawLine(
                new Vector3(0, 0, -radius.floatValue),
                new Vector3(0, directionHeight, -endRadius));

            if (!CanEditAuthoring)
            {
                Handles.matrix = previousMatrix;
                return;
            }

            EditorGUI.BeginChangeCheck();

            var newRadius = Handles.ScaleSlider(
                radius.floatValue,
                Vector3.zero,
                Vector3.right,
                Quaternion.identity,
                radius.floatValue,
                0);

            var angleBasePosition = new Vector3(radius.floatValue, 0, 0);
            var angleHandlePosition = new Vector3(endRadius, directionHeight, 0);
            var newAngleHandlePosition = Handles.Slider(
                angleHandlePosition,
                Vector3.right,
                HandleUtility.GetHandleSize(angleHandlePosition) * 0.08f,
                Handles.DotHandleCap,
                0);

            if (EditorGUI.EndChangeCheck())
            {
                radius.floatValue = Mathf.Max(0, newRadius);

                var handleDirection = newAngleHandlePosition - angleBasePosition;

                if (handleDirection.sqrMagnitude > 0)
                {
                    var newAngle = Vector3.Angle(Vector3.up, handleDirection);
                    angle.floatValue = Mathf.Clamp(newAngle, 0, 90);
                }

                _authoringObject.ApplyModifiedProperties();
            }

            Handles.matrix = previousMatrix;
        }
    }
}
