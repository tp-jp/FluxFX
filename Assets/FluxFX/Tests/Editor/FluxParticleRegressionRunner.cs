using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// 通常・全件・大規模の回帰テストを実行し、結果XMLを保存します。
    /// </summary>
    internal sealed class FluxParticleRegressionRunner : IErrorCallbacks
    {
        static FluxParticleRegressionRunner _active;
        readonly TestRunnerApi _api;
        readonly string _resultPath;
        bool _finished;

        internal FluxParticleRegressionRunner(string resultPath, TestRunnerApi api)
        {
            _resultPath = resultPath;
            _api = api;
            _api.RegisterCallbacks(this);
            AssemblyReloadEvents.beforeAssemblyReload += Release;
        }

        [MenuItem("Tools/FluxFX/Tests/Run Standard Regression")]
        public static void RunStandard()
        {
            Run("Standard", test => test.RunState != RunState.Explicit);
        }

        [MenuItem("Tools/FluxFX/Tests/Run Full Regression")]
        public static void RunFull()
        {
            Run("Full", test => true);
        }

        [MenuItem("Tools/FluxFX/Tests/Run Scale Tests")]
        public static void RunScale()
        {
            Run("Scale", test => test.Categories.Contains("Scale"));
        }

        [MenuItem("Tools/FluxFX/Tests/Run Runner Lifecycle Tests")]
        public static void RunRunnerLifecycle()
        {
            Run("RunnerLifecycle", test => test.FullName.StartsWith(
                "TpLab.Flux.FX.Tests.Editor.FluxParticleRegressionRunnerTests.", StringComparison.Ordinal));
        }

        [MenuItem("Tools/FluxFX/Tests/Run Live Inspector Tests")]
        public static void RunLiveInspector()
        {
            if (!CanRun()) return;
            UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync(new UdonSharp.Compiler.UdonSharpCompileOptions { IsEditorBuild = false });
            UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync(new UdonSharp.Compiler.UdonSharpCompileOptions { IsEditorBuild = true });
            Run("LiveInspector", test => test.Name.StartsWith("LiveInspector", StringComparison.Ordinal) ||
                test.Name == "TrailsDurationChangesClearOnlyHistoryAndDeferWhileOff");
        }

        [MenuItem("Tools/FluxFX/Tests/Run Standard Regression", true)]
        [MenuItem("Tools/FluxFX/Tests/Run Full Regression", true)]
        [MenuItem("Tools/FluxFX/Tests/Run Scale Tests", true)]
        [MenuItem("Tools/FluxFX/Tests/Run Runner Lifecycle Tests", true)]
        [MenuItem("Tools/FluxFX/Tests/Run Live Inspector Tests", true)]
        static bool CanRun()
        {
            return _active == null && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        static void Run(string suite, Func<ITestAdaptor, bool> include)
        {
            if (!CanRun()) return;
            Directory.CreateDirectory("Temp/FluxFXTests");
            var path = "Temp/FluxFXTests/" + suite + ".xml";
            File.WriteAllText(path + ".progress.txt", "Discovering tests\n");
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var callback = new FluxParticleRegressionRunner(path, api);
            _active = callback;
            api.RetrieveTestList(TestMode.EditMode, root =>
            {
                if (callback._finished) return;
                try
                {
                    var names = new List<string>();
                    Collect(root, include, names);
                    if (names.Count == 0) throw new InvalidOperationException("No FluxFX tests found.");
                    File.AppendAllText(path + ".progress.txt", "Selected: " + names.Count + "\n");
                    api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = names.ToArray() }));
                }
                catch (Exception exception)
                {
                    callback.Release();
                    Debug.LogException(exception);
                }
            });
        }

        static void Collect(ITestAdaptor test, Func<ITestAdaptor, bool> include, List<string> names)
        {
            if (!test.IsSuite && test.FullName.StartsWith("TpLab.Flux.FX.Tests.Editor.", StringComparison.Ordinal) && include(test))
            {
                names.Add(test.FullName);
            }
            if (test.HasChildren)
            {
                foreach (var child in test.Children) Collect(child, include, names);
            }
        }

        public void RunStarted(ITestAdaptor test) { }
        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (_finished) return;
            if (!result.HasChildren)
            {
                File.AppendAllText(_resultPath + ".progress.txt", result.ResultState + " " + result.FullName + "\n");
            }
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (_finished) return;
            _finished = true;
            try
            {
                File.WriteAllText(_resultPath, result.ToXml().OuterXml);
                Debug.Log($"[FluxFX Tests] Passed={result.PassCount}, Failed={result.FailCount}, Skipped={result.SkipCount}; Results: {_resultPath}");
            }
            finally
            {
                Release();
            }
        }

        public void OnError(string message)
        {
            if (_finished) return;
            _finished = true;
            try
            {
                File.AppendAllText(_resultPath + ".progress.txt", "Run failed: " + message + "\n");
            }
            finally
            {
                Release();
            }
        }

        void Release()
        {
            _finished = true;
            AssemblyReloadEvents.beforeAssemblyReload -= Release;
            // APIオブジェクトが破棄されても、Test Frameworkの通知登録はグローバルに残る。
            var api = _api != null ? _api : ScriptableObject.CreateInstance<TestRunnerApi>();
            try
            {
                api.UnregisterCallbacks(this);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(api);
                if (ReferenceEquals(_active, this)) _active = null;
            }
        }
    }
}
