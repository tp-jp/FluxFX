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
    internal sealed class FluxParticleRegressionRunner : ICallbacks
    {
        static TestRunnerApi api;
        readonly string resultPath;

        FluxParticleRegressionRunner(string resultPath)
        {
            this.resultPath = resultPath;
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

        [MenuItem("Tools/FluxFX/Tests/Run Standard Regression", true)]
        [MenuItem("Tools/FluxFX/Tests/Run Full Regression", true)]
        [MenuItem("Tools/FluxFX/Tests/Run Scale Tests", true)]
        static bool CanRun()
        {
            return api == null && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        static void Run(string suite, Func<ITestAdaptor, bool> include)
        {
            if (!CanRun()) return;
            Directory.CreateDirectory("Temp/FluxFXTests");
            var path = "Temp/FluxFXTests/" + suite + ".xml";
            File.WriteAllText(path + ".progress.txt", "Discovering tests\n");
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var callback = new FluxParticleRegressionRunner(path);
            api.RegisterCallbacks(callback);
            api.RetrieveTestList(TestMode.EditMode, root =>
            {
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
            if (!result.HasChildren)
            {
                File.AppendAllText(resultPath + ".progress.txt", result.ResultState + " " + result.FullName + "\n");
            }
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            try
            {
                File.WriteAllText(resultPath, result.ToXml().OuterXml);
                Debug.Log($"[FluxFX Tests] Passed={result.PassCount}, Failed={result.FailCount}, Skipped={result.SkipCount}; Results: {resultPath}");
            }
            finally
            {
                Release();
            }
        }

        void Release()
        {
            if (api == null) return;
            api.UnregisterCallbacks(this);
            UnityEngine.Object.DestroyImmediate(api);
            api = null;
        }
    }
}
