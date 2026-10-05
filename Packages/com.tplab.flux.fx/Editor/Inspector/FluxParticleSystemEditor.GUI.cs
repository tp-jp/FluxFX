using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleSystemEditor
    {
        void DrawProperty(SerializedProperty property, bool includeChildren = false)
        {
            var tooltip = GetTooltip(property.displayName);

            EditorGUILayout.PropertyField(
                property,
                new GUIContent(L10n.Tr(property.displayName), tooltip),
                includeChildren);
        }

        void DrawEnum(SerializedProperty property, string label = null, string tooltip = null)
        {
            var names = property.enumDisplayNames;
            var localizedNames = new string[names.Length];
            var displayName = label ?? property.displayName;

            for (var i = 0; i < names.Length; i++)
            {
                localizedNames[i] = L10n.Tr(names[i]);
            }

            var selectedIndex = EditorGUILayout.Popup(
                new GUIContent(L10n.Tr(displayName), tooltip == null ? GetTooltip(displayName) : L10n.Tr(tooltip)),
                property.enumValueIndex,
                localizedNames);

            if (selectedIndex != property.enumValueIndex)
            {
                property.enumValueIndex = selectedIndex;
            }
        }

        string GetTooltip(string label)
        {
            if (label == "Orbital")
            {
                return L10n.Tr("Sets the angular velocity around the center defined by Offset.");
            }

            if (label == "Offset")
            {
                return L10n.Tr("Sets the center used by Orbital and Radial velocity.");
            }

            if (label == "Radial")
            {
                return L10n.Tr("Sets the velocity away from or toward the center. Positive values move outward and negative values move inward.");
            }

            return null;
        }

        void DrawSectionHeader(string label)
        {
            DrawSectionHeaderInternal(label, false, false);
        }

        bool DrawSectionHeader(string label, bool expanded, string sessionStateKey)
        {
            var nextExpanded = DrawSectionHeaderInternal(label, true, expanded);

            if (nextExpanded != expanded)
            {
                SessionState.SetBool(sessionStateKey, nextExpanded);
            }

            return nextExpanded;
        }

        bool DrawSectionHeaderInternal(string label, bool collapsible, bool expanded)
        {
            InitializeSectionHeaderStyles();

            EditorGUILayout.Space(SectionSpacing);

            var rect = GUILayoutUtility.GetRect(0, SectionHeaderHeight, GUILayout.ExpandWidth(true));
            rect.x = 0;
            rect.width = EditorGUIUtility.currentViewWidth;

            var backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f)
                : new Color(0.76f, 0.76f, 0.76f);

            var borderColor = EditorGUIUtility.isProSkin
                ? new Color(0.10f, 0.10f, 0.10f)
                : new Color(0.58f, 0.58f, 0.58f);

            EditorGUI.DrawRect(rect, backgroundColor);

            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y, rect.width, 1),
                borderColor);

            EditorGUI.DrawRect(
                new Rect(rect.x, rect.yMax - 1, rect.width, 1),
                borderColor);

            var chevronRect = new Rect(
                rect.x + 12,
                rect.y,
                16,
                rect.height);

            if (collapsible)
            {
                GUI.Label(
                    chevronRect,
                    expanded ? "▼" : "▶",
                    _sectionHeaderChevronStyle);

                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            }

            var labelRect = new Rect(
                rect.x + 34,
                rect.y,
                rect.width - 46,
                rect.height);

            GUI.Label(labelRect, label, _sectionHeaderLabelStyle);

            if (!collapsible) return expanded;

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                expanded = !expanded;
                Event.current.Use();
                GUI.changed = true;
            }

            return expanded;
        }

        void InitializeSectionHeaderStyles()
        {
            if (_sectionHeaderLabelStyle != null) return;

            _sectionHeaderLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            _sectionHeaderLabelStyle.alignment = TextAnchor.MiddleLeft;
            _sectionHeaderLabelStyle.fontSize = 12;

            _sectionHeaderChevronStyle = new GUIStyle(EditorStyles.miniLabel);
            _sectionHeaderChevronStyle.alignment = TextAnchor.MiddleCenter;
        }

        bool DrawModule(string label, SerializedProperty module, bool expanded, string sessionStateKey)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            expanded = DrawModuleHeader(label, module, expanded, sessionStateKey);

            if (expanded)
            {
                var enabled = module.FindPropertyRelative("enabled");

                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                {
                    EditorGUI.indentLevel++;

                    var property = module.Copy();
                    var endProperty = property.GetEndProperty();

                    property.NextVisible(true);

                    while (property.NextVisible(false) && !SerializedProperty.EqualContents(property, endProperty))
                    {
                        if (property.name == "enabled")
                        {
                            continue;
                        }

                        if (property.propertyType == SerializedPropertyType.Enum)
                        {
                            DrawEnum(property);
                        }
                        else
                        {
                            DrawProperty(property, true);
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();

            return expanded;
        }

        bool DrawModuleHeader(string label, SerializedProperty module, bool expanded, string sessionStateKey)
        {
            InitializeModuleHeaderStyles();

            var enabled = module.FindPropertyRelative("enabled");
            var headerRect = GUILayoutUtility.GetRect(0, ModuleHeaderHeight, GUILayout.ExpandWidth(true));

            var toggleRect = new Rect(
                headerRect.x + 2,
                headerRect.y + 2,
                18,
                headerRect.height - 4);

            var labelRect = new Rect(
                headerRect.x + 24,
                headerRect.y,
                headerRect.width - 26,
                headerRect.height);

            enabled.boolValue = EditorGUI.Toggle(toggleRect, enabled.boolValue);

            if (labelRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.Repaint)
            {
                var hoverColor = EditorGUIUtility.isProSkin
                    ? new Color(1, 1, 1, 0.04f)
                    : new Color(0, 0, 0, 0.04f);

                EditorGUI.DrawRect(labelRect, hoverColor);
            }

            GUI.Label(labelRect, label, _moduleHeaderLabelStyle);
            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                labelRect.Contains(Event.current.mousePosition))
            {
                expanded = !expanded;
                SessionState.SetBool(sessionStateKey, expanded);
                Event.current.Use();
                GUI.changed = true;
            }

            return expanded;
        }

        void InitializeModuleHeaderStyles()
        {
            if (_moduleHeaderLabelStyle != null) return;

            _moduleHeaderLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            _moduleHeaderLabelStyle.alignment = TextAnchor.MiddleLeft;
            _moduleHeaderLabelStyle.fontSize = 12;
        }
    }
}