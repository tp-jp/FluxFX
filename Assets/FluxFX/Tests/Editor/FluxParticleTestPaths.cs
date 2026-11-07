using TpLab.Flux.FX.Editor.Build;
using UnityEditor;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// 配布先に依存しないテスト素材と、書き込み可能な一時アセットの配置先を提供します。
    /// </summary>
    internal static class FluxParticleTestPaths
    {
        internal static string HlslRoot => AssetDatabase.GUIDToAssetPath("d2c7d979afa259d6be2638ff47a8f84f") + "/";

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
