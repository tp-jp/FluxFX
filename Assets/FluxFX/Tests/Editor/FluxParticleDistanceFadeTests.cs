using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Editor.Modules.AudioInput;
using TpLab.Flux.FX.Editor.Modules.DistanceFade;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleDistanceFadeTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void StandardColorModulesCompileInEitherRegistrationOrder(bool reverse)
        {
            var root = new GameObject("Distance fade compilation");
            Shader shader = null;
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var baseline = FluxParticleBuildPipeline.Build(authoring);
                var modules = new FluxParticleModule[] { new FluxParticleColorOverLifetimeModule(), new FluxParticleBrightnessModule(), new FluxParticleDistanceFadeModule() };
                typeof(FluxParticleAuthoring).GetField("modules", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(authoring, (reverse ? modules.Reverse() : modules).ToList());
                var build = FluxParticleBuildPipeline.Build(authoring);
                Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, build.Shaders.Compilation.StateLayout.Pages.Count);
                var operations = build.Shaders.Compilation.Operations.Where(operation => operation.Phase ==
                    TpLab.Flux.FX.Editor.Compilation.Definitions.FluxParticlePhase.Render).ToArray();
                Assert.Greater(Array.FindIndex(operations, operation => operation.Id == FluxParticleDistanceFadeDefinition.RenderOperation),
                    Array.FindIndex(operations, operation => operation.Id == "render.color"));
                Assert.Greater(Array.FindIndex(operations, operation => operation.Id == "render.brightness"),
                    Array.FindIndex(operations, operation => operation.Id == "render.color"));
                shader = ShaderUtil.CreateShaderAsset(build.RendererSource, true);
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), ShaderErrors(shader));
            }
            finally
            {
                if (shader != null)
                {
                    Object.DestroyImmediate(shader);
                }
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NewModulesHaveStandardMetadataAndLiveBindings()
        {
            var registry = new FluxParticleInspectorModuleRegistry();
            foreach (var type in new[] { typeof(FluxParticleAudioInputModule), typeof(FluxParticleDistanceFadeModule) })
            {
                Assert.IsTrue(registry.TryGet(type, out var metadata));
                Assert.IsFalse(metadata.IsCustom);
                var bindings = new TpLab.Flux.FX.Editor.Modules.FluxParticleInputAppearanceLiveInspectorProvider()
                    .Bindings.Where(binding => binding.ModuleType == type).ToArray();
                var contract = type == typeof(FluxParticleAudioInputModule)
                    ? FluxParticleAudioInputContract.Module : FluxParticleDistanceFadeContract.Module;
                CollectionAssert.AreEquivalent(contract.Parameters.Select(parameter => parameter.Id).Concat(new[] { contract.Enabled.Id }),
                    bindings.Select(binding => binding.Contract.Id));
            }
        }

        [Test]
        public void NewModuleInspectorLabelsSwitchBetweenJapaneseAndEnglish()
        {
            var localization = typeof(FluxParticleInspectorModuleRegistry).Assembly.GetType("TpLab.Flux.FX.Editor.Localization.L10n", true);
            var language = localization.GetProperty("Language", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = language.GetValue(null);
            try
            {
                var registry = new FluxParticleInspectorModuleRegistry();
                Assert.IsTrue(registry.TryGet(typeof(FluxParticleAudioInputModule), out var audio));
                Assert.IsTrue(registry.TryGet(typeof(FluxParticleDistanceFadeModule), out var fade));
                language.SetValue(null, TpLab.Flux.FX.Editor.Localization.Language.Japanese);
                Assert.AreEqual("音声入力", audio.Translate(audio.DisplayName));
                Assert.AreEqual("全音域平均の入力感度", audio.Translate("Average Gain"));
                Assert.AreEqual("距離フェード", fade.Translate(fade.DisplayName));
                Assert.AreEqual("終了距離", fade.Translate("End Distance"));
                language.SetValue(null, TpLab.Flux.FX.Editor.Localization.Language.English);
                Assert.AreEqual("Audio Input", audio.Translate(audio.DisplayName));
                Assert.AreEqual("Distance Fade", fade.Translate(fade.DisplayName));
            }
            finally
            {
                language.SetValue(null, previous);
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void DistanceFadeGpuUsesSharedOriginScaleAndCaptureContract(bool stereo, bool intrinsic)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("GPU required.");
            }
            var shader = ShaderUtil.CreateShaderAsset(ProbeShader(stereo, intrinsic), true);
            Material material = null;
            var target = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), ShaderErrors(shader));
                material = new Material(shader);
                target.Create();
                foreach (var scale in new[] { 0.5f, 1f, 3f })
                foreach (var effectScale in new[] { 1f, 200f })
                foreach (var fixedWorld in new[] { false, true })
                foreach (var distance in new[] { 2f, 5f, 7.5f, 10f, 12f })
                foreach (var orthographic in new[] { false, true })
                foreach (var alpha in new[] { false, true })
                {
                    var origin = new Vector3(3, 4, 5);
                    var camera = origin + Vector3.forward * distance * (fixedWorld ? 1 : scale * effectScale);
                    material.SetVector("_ProbeScaleSettings", new Vector4(effectScale, fixedWorld ? 1 : 0, 0, 0));
                    material.SetMatrix("_ProbeTransform", Matrix4x4.TRS(origin, Quaternion.Euler(10, 30, 20), Vector3.one * scale));
                    material.SetVector("_ProbeCamera", camera);
                    // A wide eye separation makes accidental per-eye fading observable.
                    material.SetVectorArray("_ProbeEyes", new[] { (Vector4)(camera + Vector3.right), (Vector4)(camera - Vector3.right) });
                    material.SetVector("_ProbeOrtho", new Vector4(0, 0, 0, orthographic ? 1 : 0));
                    material.SetVector("_ProbeSettings", new Vector4(5, 10, alpha ? 1 : 0, 0));
                    var factor = intrinsic || orthographic ? 1 : 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(5, 10, distance));
                    var actual = DrawProbe(material, target, pixels);
                    var expected = alpha ? new Color(2, 4, 6, 0.8f * factor) : new Color(2 * factor, 4 * factor, 6 * factor, 0.8f);
                    for (var component = 0; component < 4; component++)
                        Assert.AreEqual(expected[component], actual[component], 0.0002f, $"scale={scale}, distance={distance}, ortho={orthographic}, alpha={alpha}");
                }
                material.SetVector("_ProbeSettings", new Vector4(10, 5, 0, 0));
                var disabled = DrawProbe(material, target, pixels);
                Assert.AreEqual(2, disabled.r, 0.0001f);
                material.SetVector("_ProbeSettings", new Vector4(5, 5, 0, 0));
                Assert.AreEqual(2, DrawProbe(material, target, pixels).r, 0.0001f);
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
                if (material != null)
                {
                    Object.DestroyImmediate(material);
                }
                Object.DestroyImmediate(shader);
            }
        }

        static Color DrawProbe(Material material, RenderTexture target, Texture2D pixels)
        {
            Graphics.Blit(null, target, material);
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
            pixels.Apply();
            return pixels.GetPixel(0, 0);
        }

        static string ShaderErrors(Shader shader) => string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(message => message.message));

        static string ProbeShader(bool stereo, bool intrinsic)
        {
            return @"Shader ""Hidden/FluxFX/DistanceFadeProbe"" { SubShader { Pass { ZTest Always Cull Off ZWrite Off
CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#pragma target 3.0
#include ""UnityCG.cginc""
float4x4 _ProbeTransform;
float4 _ProbeCamera, _ProbeOrtho, _ProbeSettings, _ProbeScaleSettings;
float4 _ProbeEyes[2];
#define unity_ObjectToWorld _ProbeTransform
#define _WorldSpaceCameraPos _ProbeCamera.xyz
#define unity_OrthoParams _ProbeOrtho
" + (stereo ? "#define USING_STEREO_MATRICES\n#define unity_StereoWorldSpaceCameraPos _ProbeEyes\n" : "") +
                (intrinsic ? "#define FLUXFX_INTRINSIC_RENDER\n" : "") + @"
struct FluxRenderDistanceFadeContext {
    float4 valueRenderColor;
    float valueSystemEffectScale, parameterDistanceFadeDistanceMode;
    float parameterDistanceFadeStartDistance, parameterDistanceFadeEndDistance, parameterDistanceFadeTarget;
};
#include ""Packages/com.tplab.flux.fx/Editor/Modules/DistanceFade/DistanceFade.hlsl""
float4 frag(v2f_img i) : SV_Target {
    FluxRenderDistanceFadeContext ctx;
    ctx.valueRenderColor = float4(2, 4, 6, 0.8);
    ctx.valueSystemEffectScale = _ProbeScaleSettings.x; ctx.parameterDistanceFadeDistanceMode = _ProbeScaleSettings.y;
    ctx.parameterDistanceFadeStartDistance = _ProbeSettings.x;
    ctx.parameterDistanceFadeEndDistance = _ProbeSettings.y;
    ctx.parameterDistanceFadeTarget = _ProbeSettings.z;
    FluxFX_DistanceFadeRender(ctx);
    return ctx.valueRenderColor;
}
ENDCG
} } }";
        }
    }
}
