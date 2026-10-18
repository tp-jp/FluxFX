using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Editor.Compilation.Contracts;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;
using TpLab.Flux.FX.Udon;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.Udon;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleSystemEditor
    {
        sealed class LiveField
        {
            public FluxParticleLiveBinding Binding;
            public FluxParticleParameterContract Contract => Binding.Contract;
            public Vector4 Value;
            public bool Available;
            public string Error;
            public int Type;
        }

        readonly Dictionary<string, LiveField> _liveFields = new Dictionary<string, LiveField>();
        bool _liveWasPlaying;
        UdonBehaviour _liveExecutor;
        UdonBehaviour _liveEmitter;
        double _nextLivePoll;

        void InitializeLiveInspector()
        {
            _liveFields.Clear();
            foreach (var type in TypeCache.GetTypesDerivedFrom<FluxParticleLiveInspectorProvider>().OrderBy(type => type.FullName))
            {
                if (type.IsAbstract) continue;
                var provider = (FluxParticleLiveInspectorProvider)Activator.CreateInstance(type);
                foreach (var binding in provider.Bindings)
                {
                    var contract = binding.Contract;
                    if (contract.Policy != FluxParticleParameterPolicy.Live) continue;
                    var path = binding.PropertyPath;
                    if (binding.ModuleType != null)
                    {
                        var index = FindModuleIndex(binding.ModuleType);
                        if (index < 0) continue;
                        path = _modules.GetArrayElementAtIndex(index).propertyPath + "." + path;
                    }
                    if (_authoringObject.FindProperty(path) == null)
                    {
                        throw new InvalidOperationException($"Live Inspector property not found: {path}");
                    }
                    _liveFields.Add(path, new LiveField { Binding = binding, Type = (int)FluxParticleParameterContractCompiler.Compile(contract).Type });
                }
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            // UdonSharpのIMGUIラッパーは描画後にProxy全体をVMへ書き戻すため、
            // 独自UIの経路を使い、Live操作の反映先をVMに限定する。
            var inspector = new IMGUIContainer(OnInspectorGUI);
            inspector.schedule.Execute(() =>
            {
                if (EditorApplication.isPlaying && EditorApplication.timeSinceStartup >= _nextLivePoll)
                {
                    inspector.MarkDirtyRepaint();
                }
            }).Every(200);
            return inspector;
        }

        void RefreshLiveInspector()
        {
            if (!EditorApplication.isPlaying)
            {
                _liveWasPlaying = false;
                return;
            }
            if (EditorApplication.isCompiling) return;
            if (!_liveWasPlaying)
            {
                InitializeLiveInspector();
                _liveWasPlaying = true;
                _nextLivePoll = 0;
            }
            var system = UdonSharpEditorUtility.GetBackingUdonBehaviour((FluxParticleSystem)target);
            var executor = system == null ? null : system.GetProgramVariable("particleExecutor") as UdonBehaviour;
            var emitter = system == null ? null : system.GetProgramVariable("particleEmitter") as UdonBehaviour;
            if (_liveExecutor != executor || _liveEmitter != emitter || EditorApplication.timeSinceStartup >= _nextLivePoll)
            {
                _liveExecutor = executor;
                _liveEmitter = emitter;
                _nextLivePoll = EditorApplication.timeSinceStartup + 0.2;
                foreach (var field in _liveFields.Values)
                {
                    field.Available = FluxParticleLiveConnection.Request(LiveTarget(field), field.Contract.Id, field.Type, false,
                        ref field.Value, out _);
                }
            }

        }

        UdonBehaviour LiveTarget(LiveField field)
        {
            return field.Binding.Target == FluxParticleLiveTarget.Emitter ? _liveEmitter : _liveExecutor;
        }

        bool TryDrawLiveProperty(SerializedProperty property, Rect? toggleRect = null, string labelOverride = null)
        {
            if (!EditorApplication.isPlaying || !_liveFields.TryGetValue(property.propertyPath, out var field)) return false;
            var previousEnabled = GUI.enabled;
            try
            {
                GUI.enabled = field.Available && !EditorApplication.isCompiling;
                var value = field.Value;
                var label = new GUIContent(L10n.Tr(labelOverride ?? property.displayName),
                    L10n.Tr("Runtime value. Changes are discarded when Play Mode ends."));
                EditorGUI.BeginChangeCheck();
                if (field.Binding.Component >= 0)
                {
                    value[field.Binding.Component] = EditorGUILayout.FloatField(label, value[field.Binding.Component]);
                }
                else if (field.Contract.Kind == FluxParticleParameterKind.Enum)
                {
                    var options = field.Contract.EnumConstraint.AllowedValues;
                    var names = options.Select(option => L10n.Tr(ObjectNames.NicifyVariableName(option.ToString()))).ToArray();
                    var selected = -1;
                    for (var i = 0; i < options.Count; i++)
                    {
                        if (Convert.ToSingle(options[i]) == value.x) selected = i;
                    }
                    var next = EditorGUILayout.Popup(label, selected, names.Select(name => new GUIContent(name)).ToArray());
                    if (next >= 0 && next != selected) value.x = Convert.ToSingle(options[next]);
                }
                else if (field.Contract.Kind == FluxParticleParameterKind.Color)
                {
                    value = (Vector4)EditorGUILayout.ColorField(label, (Color)value, true, true, true);
                }
                else if (field.Contract.Kind == FluxParticleParameterKind.Vector2)
                {
                    var vector = EditorGUILayout.Vector2Field(label, new Vector2(value.x, value.y));
                    value = new Vector4(vector.x, vector.y, 0, 0);
                }
                else if (field.Contract.Kind == FluxParticleParameterKind.Vector4)
                {
                    value = EditorGUILayout.Vector4Field(label, value);
                }
                else if (field.Contract.Kind == FluxParticleParameterKind.Bool)
                {
                    var enabled = toggleRect.HasValue
                        ? EditorGUI.Toggle(toggleRect.Value, new GUIContent(string.Empty, label.tooltip), value.x > 0.5f)
                        : EditorGUILayout.Toggle(label, value.x > 0.5f);
                    value.x = enabled ? 1 : 0;
                }
                else if (field.Contract.Kind == FluxParticleParameterKind.Vector3)
                {
                    value = EditorGUILayout.Vector3Field(label, value);
                }
                else
                {
                    value.x = EditorGUILayout.FloatField(label, value.x);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    // 範囲の片側だけを編集する際も、もう片側はVMの最新値を維持する。
                    if (field.Binding.Component >= 0)
                    {
                        var current = Vector4.zero;
                        if (!FluxParticleLiveConnection.Request(LiveTarget(field), field.Contract.Id, field.Type, false, ref current, out _))
                        {
                            field.Available = false;
                            return true;
                        }
                        current[field.Binding.Component] = value[field.Binding.Component];
                        value = current;
                    }
                    field.Available = FluxParticleLiveConnection.Request(LiveTarget(field), field.Contract.Id, field.Type, true,
                        ref value, out var accepted);
                    field.Error = field.Available && accepted ? null : L10n.Tr("Change rejected. Check the input constraints and Runtime state.");
                    if (field.Available) field.Value = value;
                }
            }
            finally
            {
                GUI.enabled = previousEnabled;
            }
            if (field.Error != null) EditorGUILayout.HelpBox(field.Error, MessageType.Warning);
            return true;
        }
    }
}
