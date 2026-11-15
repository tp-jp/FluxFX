using System;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.ModuleCreation
{
    /// <summary>
    /// 最小カスタムModuleの生成画面。
    /// </summary>
    public sealed class FluxParticleModuleCreatorWindow : EditorWindow
    {
        [SerializeField]
        string moduleName = "CustomAcceleration";

        [SerializeField]
        string namespaceName = "MyCompany.FluxFX";

        [SerializeField]
        string parentFolder = "Assets";

        [SerializeField]
        bool runtimeApi;

        string _message;
        MessageType _messageType;
        Vector2 _scroll;

        /// <summary>
        /// メニューから生成画面を開きます。
        /// </summary>
        [MenuItem("Tools/FluxFX/Create Custom Module...")]
        public static void Open()
        {
            var window = GetWindow<FluxParticleModuleCreatorWindow>();
            window.titleContent = new GUIContent(L10n.Tr("Create Custom Module"));
            window.minSize = new Vector2(440, 300);
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox(L10n.Tr("Create an editable module that accelerates particles upward. After compilation, add it from Authoring > Add Module > Custom and start Preview."), MessageType.Info);
            EditorGUI.BeginChangeCheck();
            moduleName = EditorGUILayout.TextField(L10n.Tr("Module Name"), moduleName);
            namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(parentFolder);
            var selected = (DefaultAsset)EditorGUILayout.ObjectField(L10n.Tr("Parent Folder"), folder, typeof(DefaultAsset), false);
            if (selected != folder)
            {
                parentFolder = selected == null ? "" : AssetDatabase.GetAssetPath(selected);
            }
            runtimeApi = EditorGUILayout.Toggle(L10n.Tr("Generate Runtime API"), runtimeApi);
            if (EditorGUI.EndChangeCheck())
            {
                _message = null;
            }
            EditorGUILayout.LabelField(L10n.Tr("Destination"), parentFolder + "/" + moduleName, EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox(L10n.Tr("A new folder is required. Existing files are never overwritten. Runtime API adds an optional Udon handle; it is not required for Preview or normal playback."), MessageType.None);
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button(L10n.Tr("Create Module")))
                {
                    try
                    {
                        var path = FluxParticleModuleCreator.Create(moduleName, namespaceName, parentFolder, runtimeApi);
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<DefaultAsset>(path));
                        _message = L10n.Tr("Sources created. Wait for compilation, then add the module from Add Module > Custom.");
                        _messageType = MessageType.Info;
                    }
                    catch (Exception exception)
                    {
                        _message = exception.Message;
                        _messageType = MessageType.Error;
                    }
                }
            }
            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, _messageType);
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
