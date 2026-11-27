using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Compilation.Contracts;
using TpLab.Flux.FX.Editor.Modules.AudioInput;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleAuthoringEditor
    {
        void InitializeAudioChannelLiveInspector()
        {
            var index = FindModuleIndex(typeof(FluxParticleAudioInputModule));
            if (index < 0) return;
            var module = GetModuleProperty(index);
            var channels = module.FindPropertyRelative("channels");
            for (var i = 0; i < channels.arraySize; i++)
            {
                var channel = channels.GetArrayElementAtIndex(i);
                var id = channel.FindPropertyRelative("id").stringValue;
                FluxParticleAudioChannelContract contract;
                try { contract = new FluxParticleAudioChannelContract(id); }
                catch (ArgumentException) { continue; }
                var fields = new[] { "source", "gain", "attack", "release" };
                for (var j = 0; j < fields.Length; j++)
                {
                    var parameter = contract.Parameters[j];
                    var path = channel.FindPropertyRelative(fields[j]).propertyPath;
                    _liveFields.Add(path, new LiveField { Binding = new FluxParticleLiveBinding(null, path, parameter),
                        Type = (int)FluxParticleParameterContractCompiler.Compile(parameter).Type });
                }
            }
        }

        void DrawAudioChannels(SerializedProperty channels)
        {
            EditorGUILayout.LabelField(L10n.Tr("Channels"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(L10n.Tr("Channels share input gain and noise floor, then apply their own gain and response times. References use the fixed ID, not the name or order."), MessageType.Info);
            for (var i = 0; i < channels.arraySize; i++)
            {
                var channel = channels.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                using (new EditorGUI.DisabledScope(!CanEditAuthoring))
                {
                    DrawProperty(channel.FindPropertyRelative("name"));
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(L10n.Tr("Channel ID"));
                    EditorGUILayout.SelectableLabel(channel.FindPropertyRelative("id").stringValue, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.EndHorizontal();
                }
                DrawEnum(channel.FindPropertyRelative("source"));
                DrawProperty(channel.FindPropertyRelative("gain"));
                DrawProperty(channel.FindPropertyRelative("attack"));
                DrawProperty(channel.FindPropertyRelative("release"));
                var remove = false;
                using (new EditorGUI.DisabledScope(!CanEditAuthoring))
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(i == 0))
                    {
                        if (GUILayout.Button(L10n.Tr("Move Up"))) channels.MoveArrayElement(i, i - 1);
                    }
                    using (new EditorGUI.DisabledScope(i + 1 == channels.arraySize))
                    {
                        if (GUILayout.Button(L10n.Tr("Move Down"))) channels.MoveArrayElement(i, i + 1);
                    }
                    remove = GUILayout.Button(L10n.Tr("Remove Channel"));
                }
                EditorGUILayout.EndVertical();
                if (remove)
                {
                    channels.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
            using (new EditorGUI.DisabledScope(!CanEditAuthoring))
            {
                if (GUILayout.Button(L10n.Tr("Add Channel"))) AddAudioChannel(channels);
            }
            var index = FindModuleIndex(typeof(FluxParticleAudioInputModule));
            if (index >= 0 && GetModuleProperty(index).managedReferenceValue is FluxParticleAudioInputModule input)
            {
                try { FluxParticleAudioChannelContract.Validate(input.Channels); }
                catch (Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Error); }
            }
        }

        internal static void AddAudioChannel(SerializedProperty channels)
        {
            channels.InsertArrayElementAtIndex(channels.arraySize);
            var channel = channels.GetArrayElementAtIndex(channels.arraySize - 1);
            channel.FindPropertyRelative("id").stringValue = Guid.NewGuid().ToString("N");
            channel.FindPropertyRelative("name").stringValue = "Channel " + channels.arraySize;
            channel.FindPropertyRelative("source").intValue = 1;
            channel.FindPropertyRelative("gain").floatValue = 1;
            channel.FindPropertyRelative("attack").floatValue = 0.2f;
            channel.FindPropertyRelative("release").floatValue = 0.8f;
        }

        void DrawBrightnessAudioChannel(SerializedProperty property)
        {
            var ids = new List<string> { "" };
            var names = new List<string> { L10n.Tr("Independent Response") };
            var index = FindModuleIndex(typeof(FluxParticleAudioInputModule));
            if (index >= 0)
            {
                var channels = GetModuleProperty(index).FindPropertyRelative("channels");
                for (var i = 0; i < channels.arraySize; i++)
                {
                    var channel = channels.GetArrayElementAtIndex(i);
                    var id = channel.FindPropertyRelative("id").stringValue;
                    ids.Add(id);
                    names.Add(channel.FindPropertyRelative("name").stringValue + " (" + id.Substring(0, Math.Min(8, id.Length)) + ")");
                }
            }
            var selected = ids.IndexOf(property.stringValue);
            if (selected < 0)
            {
                EditorGUILayout.HelpBox(L10n.Tr("The selected audio channel is missing. Select a valid channel or Independent Response."), MessageType.Error);
                selected = ids.Count;
                ids.Add(property.stringValue);
                names.Add(L10n.Tr("Missing") + ": " + property.stringValue);
            }
            using (new EditorGUI.DisabledScope(!CanEditAuthoring))
            {
                var next = EditorGUILayout.Popup(L10n.Tr("Audio Channel"), selected, names.ToArray());
                if (next != selected) property.stringValue = ids[next];
            }
            if (!string.IsNullOrEmpty(property.stringValue))
                EditorGUILayout.HelpBox(L10n.Tr("Uses the channel response directly. Brightness does not apply a second Attack or Release."), MessageType.Info);
        }
    }
}
