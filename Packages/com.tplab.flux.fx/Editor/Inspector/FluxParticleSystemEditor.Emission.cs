using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor
{
    public partial class FluxParticleSystemEditor
    {
        const float BurstFieldSpacing = 8.0f;

        void DrawEmission()
        {
            _emissionExpanded = DrawSectionHeader(L10n.Tr("EMISSION"), _emissionExpanded, EmissionExpandedKey);
            if (!_emissionExpanded) return;

            EditorGUILayout.Space(2);

            EditorGUILayout.PropertyField(_emission.FindPropertyRelative("rate"), new GUIContent(L10n.Tr("Rate over Time")));

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField(L10n.Tr("Bursts"), EditorStyles.boldLabel);
            _burstList.DoLayoutList();
        }

        void InitializeBurstList()
        {
            var bursts = _emission.FindPropertyRelative("bursts");

            _burstList = new ReorderableList(
                _authoringObject,
                bursts,
                true,
                true,
                true,
                true);

            _burstList.drawHeaderCallback = rect =>
            {
                var fieldWidth = (rect.width - BurstFieldSpacing) * 0.5f;

                var timeRect = new Rect(
                    rect.x,
                    rect.y,
                    fieldWidth,
                    rect.height);

                var countRect = new Rect(
                    timeRect.xMax + BurstFieldSpacing,
                    rect.y,
                    fieldWidth,
                    rect.height);

                EditorGUI.LabelField(timeRect, L10n.Tr("Time"));
                EditorGUI.LabelField(countRect, L10n.Tr("Count"));
            };

            _burstList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var burst = bursts.GetArrayElementAtIndex(index);
                var time = burst.FindPropertyRelative("time");
                var count = burst.FindPropertyRelative("count");

                rect.y += 2;

                var fieldWidth = (rect.width - BurstFieldSpacing) * 0.5f;

                var timeRect = new Rect(
                    rect.x,
                    rect.y,
                    fieldWidth,
                    EditorGUIUtility.singleLineHeight);

                var countRect = new Rect(
                    timeRect.xMax + BurstFieldSpacing,
                    rect.y,
                    fieldWidth,
                    EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(timeRect, time, GUIContent.none);
                EditorGUI.PropertyField(countRect, count, GUIContent.none);
            };

            _burstList.elementHeight = EditorGUIUtility.singleLineHeight + 4;
        }
    }
}