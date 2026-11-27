using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleInspectorRefreshTests
    {
        Scene _scene;
        GameObject _root;
        FluxParticleAuthoring _authoring;
        UnityEditor.Editor _editor;
        const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            _root = new GameObject("Inspector Refresh Test");
            SceneManager.MoveGameObjectToScene(_root, _scene);
            _authoring = _root.AddComponent<FluxParticleAuthoring>();
            _editor = UnityEditor.Editor.CreateEditor(_authoring, typeof(FluxParticleAuthoringEditor));
            SetModules(new FluxParticleGravityModule(), new FluxParticleDragModule());
        }

        [TearDown]
        public void TearDown()
        {
            if (_editor != null) Object.DestroyImmediate(_editor);
            if (_authoring != null) Undo.ClearUndo(_authoring);
            if (_root != null) Object.DestroyImmediate(_root);
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
        }

        [Test]
        public void ModuleIndexFollowsExternalReplacementReorderAndRemoval()
        {
            Assert.AreEqual(0, Index(typeof(FluxParticleGravityModule)));
            var original = Property(0);
            Assert.AreSame(original, Property(0), "Unchanged configuration reuses the property");
            using (var external = new SerializedObject(_authoring))
            {
                var modules = external.FindProperty("modules");
                modules.MoveArrayElement(0, 1);
                external.ApplyModifiedPropertiesWithoutUndo();
            }
            _editor.serializedObject.Update();
            Assert.AreEqual(1, Index(typeof(FluxParticleGravityModule)));
            Assert.IsInstanceOf<FluxParticleGravityModule>(Property(1).managedReferenceValue);
            Assert.AreNotSame(original, Property(1));
            SetModules(new FluxParticleForceModule());
            Assert.AreEqual(-1, Index(typeof(FluxParticleGravityModule)));
            Assert.AreEqual(0, Index(typeof(FluxParticleForceModule)));
            Assert.IsInstanceOf<FluxParticleForceModule>(Property(0).managedReferenceValue);
            SetModules();
            Assert.AreEqual(-1, Index(typeof(FluxParticleForceModule)));
        }

        [Test]
        public void CachedPropertyWritesAndUndoRedoKeepCorrectModule()
        {
            var property = Property(0).FindPropertyRelative("enabled");
            property.boolValue = false;
            _editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            _editor.serializedObject.Update();
            Assert.IsFalse(Property(0).FindPropertyRelative("enabled").boolValue);
            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(_authoring, "Inspector Test Reorder");
            var modules = _editor.serializedObject.FindProperty("modules");
            modules.MoveArrayElement(0, 1);
            _editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreEqual(1, Index(typeof(FluxParticleGravityModule)));
            Undo.PerformUndo();
            _editor.serializedObject.Update();
            Assert.AreEqual(0, Index(typeof(FluxParticleGravityModule)));
            Assert.IsFalse(Property(0).FindPropertyRelative("enabled").boolValue);
            Undo.PerformRedo();
            _editor.serializedObject.Update();
            Assert.AreEqual(1, Index(typeof(FluxParticleGravityModule)));
            Assert.IsFalse(Property(1).FindPropertyRelative("enabled").boolValue);
        }

        [Test]
        public void NullAndDuplicateModulesPreserveFirstMatch()
        {
            SetModules(null, new FluxParticleGravityModule(), new FluxParticleGravityModule());
            Assert.AreEqual(1, Index(typeof(FluxParticleGravityModule)));
            Assert.IsNull(Property(0).managedReferenceValue);
            Assert.AreEqual(-1, Index(typeof(FluxParticleDragModule)));
        }

        [Test]
        public void EditModeDoesNotInitializeRuntimeBindingsAndScheduleIsPaused()
        {
            var fields = (IDictionary)Field("_liveFields");
            Assert.IsEmpty(fields);
            var inspector = _editor.CreateInspectorGUI();
            Call("RefreshLiveInspector");
            Assert.IsEmpty(fields);
            var schedule = (IVisualElementScheduledItem)Field("_liveSchedule");
            Assert.IsFalse(schedule.isActive);
            Call("OnLivePlayModeStateChanged", PlayModeStateChange.EnteredPlayMode);
            Call("OnLivePlayModeStateChanged", PlayModeStateChange.ExitingPlayMode);
            Assert.IsFalse(schedule.isActive);
            Call("OnLivePlayModeStateChanged", PlayModeStateChange.EnteredEditMode);
            Assert.IsFalse(schedule.isActive);
            GC.KeepAlive(inspector);
        }

        [Test]
        public void SpeedRangeComponentsShareOneRuntimeValue()
        {
            Call("InitializeLiveInspector");
            var fields = (IDictionary)Field("_liveFields");
            var min = fields["initialVelocity.min"];
            var max = fields["initialVelocity.max"];
            Assert.IsNotNull(min);
            Assert.IsNotNull(max);
            var state = min.GetType().GetField("State");
            Assert.AreSame(state.GetValue(min), state.GetValue(max));
            Assert.AreNotSame(state.GetValue(min), state.GetValue(fields["initialVelocity.speed"]));
        }

        void SetModules(params FluxParticleModule[] values)
        {
            _editor.serializedObject.Update();
            var modules = _editor.serializedObject.FindProperty("modules");
            modules.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) modules.GetArrayElementAtIndex(i).managedReferenceValue = values[i];
            _editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            _editor.serializedObject.Update();
        }

        int Index(Type type) => (int)Call("FindModuleIndex", type);
        SerializedProperty Property(int index) => (SerializedProperty)Call("GetModuleProperty", index);
        object Field(string name) => _editor.GetType().GetField(name, InstanceMembers).GetValue(_editor);
        object Call(string name, params object[] args) => _editor.GetType().GetMethod(name, InstanceMembers).Invoke(_editor, args);
    }
}
