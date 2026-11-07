using System.Collections;
using NUnit.Framework;
using UnityEditor;

namespace TpLab.Flux.FX.Tests.Editor
{
    internal static class FluxParticleEditorTestWait
    {
        internal static IEnumerator ForDelayCall()
        {
            var completed = false;
            EditorApplication.CallbackFunction callback = () => completed = true;
            EditorApplication.delayCall += callback;
            var deadline = EditorApplication.timeSinceStartup + 10;
            try
            {
                while (!completed && EditorApplication.timeSinceStartup < deadline)
                {
                    yield return null;
                }
                Assert.IsTrue(completed, "EditorApplication.delayCall did not run.");
            }
            finally
            {
                EditorApplication.delayCall -= callback;
            }
        }
    }
}
