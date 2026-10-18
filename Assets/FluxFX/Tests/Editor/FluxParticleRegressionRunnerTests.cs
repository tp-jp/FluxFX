using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleRegressionRunnerTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CompletionUnregistersAndIgnoresLaterRuns(bool destroyedApi)
        {
            Verify(destroyedApi, false);
        }

        [Test]
        public void FailedLaunchPreservesPreviousResultAndUnregisters()
        {
            Verify(true, true);
        }

        static void Verify(bool destroyedApi, bool error)
        {
            Directory.CreateDirectory("Temp/FluxFXTests");
            var path = "Temp/FluxFXTests/RunnerLifecycle-" + Guid.NewGuid().ToString("N") + ".xml";
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var observer = ScriptableObject.CreateInstance<TestRunnerApi>();
            var callback = new FluxParticleRegressionRunner(path, api);
            try
            {
                Assert.IsTrue(IsRegistered(observer, callback));
                File.WriteAllText(path, "previous result");
                if (destroyedApi) Object.DestroyImmediate(api);
                if (error)
                {
                    callback.OnError("Expected launch failure");
                    Assert.AreEqual("previous result", File.ReadAllText(path));
                }
                else
                {
                    callback.TestFinished(new Result("First"));
                    callback.RunFinished(new Result("First"));
                    StringAssert.Contains("First", File.ReadAllText(path));
                }
                Assert.IsFalse(IsRegistered(observer, callback));
                var xml = File.ReadAllBytes(path);
                var progress = File.ReadAllBytes(path + ".progress.txt");
                // 既に通知先の配列へコピーされたコールバックも書き込まない。
                callback.RunStarted(null);
                callback.TestFinished(new Result("Another run"));
                callback.RunFinished(new Result("Another run"));
                callback.OnError("Another failure");
                CollectionAssert.AreEqual(xml, File.ReadAllBytes(path));
                CollectionAssert.AreEqual(progress, File.ReadAllBytes(path + ".progress.txt"));
            }
            finally
            {
                observer.UnregisterCallbacks(callback);
                callback.OnError("Cleanup");
                if (api != null) Object.DestroyImmediate(api);
                Object.DestroyImmediate(observer);
                File.Delete(path);
                File.Delete(path + ".progress.txt");
            }
        }

        static bool IsRegistered(TestRunnerApi api, ICallbacks callback)
        {
            var holder = typeof(TestRunnerApi).GetProperty("m_CallbacksHolder", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(api);
            var callbacks = (ICallbacks[])holder.GetType().GetMethod("GetAll").Invoke(holder, null);
            return Array.IndexOf(callbacks, callback) >= 0;
        }

        sealed class Result : ITestResultAdaptor
        {
            internal Result(string name) { Name = name; }
            public ITestAdaptor Test => null;
            public string Name { get; }
            public string FullName => Name;
            public string ResultState => "Passed";
            public UnityEditor.TestTools.TestRunner.Api.TestStatus TestStatus => UnityEditor.TestTools.TestRunner.Api.TestStatus.Passed;
            public double Duration => 0;
            public DateTime StartTime => DateTime.MinValue;
            public DateTime EndTime => DateTime.MinValue;
            public string Message => null;
            public string StackTrace => null;
            public int AssertCount => 0;
            public int FailCount => 0;
            public int PassCount => 1;
            public int SkipCount => 0;
            public int InconclusiveCount => 0;
            public bool HasChildren => false;
            public IEnumerable<ITestResultAdaptor> Children => Array.Empty<ITestResultAdaptor>();
            public string Output => string.Empty;
            public TNode ToXml() => new TNode("test-run", Name);
        }
    }
}
