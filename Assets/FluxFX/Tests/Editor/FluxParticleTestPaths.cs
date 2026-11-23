using System;
using System.IO;
using TpLab.Flux.FX.Editor.Build;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// 配布先に依存しないテスト素材と、書き込み可能な一時アセットの配置先を提供します。
    /// </summary>
    internal static class FluxParticleTestPaths
    {
        internal static string HlslRoot => AssetDatabase.GUIDToAssetPath("d2c7d979afa259d6be2638ff47a8f84f") + "/";

        // テスト自身が削除した専用ルートの台帳だけを後始末します。実際の生成先は扱いません。
        internal static void RemoveEmptyGenerationLedger(string root)
        {
            if (!root.StartsWith("Assets/FluxFXTestTemp/", StringComparison.Ordinal) || Directory.Exists(root))
            {
                throw new InvalidOperationException("Test generation root has not been removed: " + root);
            }
            File.Delete("UserSettings/FluxFXGeneration/Roots/" + Hash128.Compute(root) + ".json");
        }

        internal static string TemporaryRoot
        {
            get
            {
                const string path = "Assets/FluxFXTestTemp";
                FluxParticleGenerationSession.EnsureFolder(path);
                return path;
            }
        }
    }
}
