using TpLab.Flux.FX.Udon;
using UnityEditor;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor
{
    public partial class FluxParticleSystemEditor
    {
        void DrawShape()
        {
            _shapeExpanded = DrawSectionHeader(L10n.Tr("SHAPE"), _shapeExpanded, ShapeExpandedKey);
            if (!_shapeExpanded) return;

            EditorGUILayout.Space(2);

            var type = _shape.FindPropertyRelative("type");
            var shapeType = (FluxParticleShapeType)type.enumValueIndex;

            DrawEnum(type, "Type");

            if (shapeType == FluxParticleShapeType.Sphere ||
                shapeType == FluxParticleShapeType.Hemisphere ||
                shapeType == FluxParticleShapeType.Circle)
            {
                DrawProperty(_shape.FindPropertyRelative("radius"));
            }
            else if (shapeType == FluxParticleShapeType.Box)
            {
                DrawProperty(_shape.FindPropertyRelative("size"));
            }
            else if (shapeType == FluxParticleShapeType.Cone)
            {
                DrawProperty(_shape.FindPropertyRelative("radius"));
                DrawProperty(_shape.FindPropertyRelative("angle"));
            }
        }
    }
}