using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Authoring;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Modules.Brightness;
using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleBrightnessTests
    {
        const string Old = "TpLab.Flux.FX.Scripts TpLab.Flux.FX.Scripts.Modules.FluxParticleBlinkModule";
        static readonly string[] _fields = { "enabled", "frequency", "duty", "minimum", "smoothness", "randomPhase", "timeMode", "target" };

        [TestCase(false)]
        [TestCase(true)]
        public void PhaseMigrationMetadataIsHiddenAndSelectsOnlyTheRenderEntry(bool migrated)
        {
            var root = new GameObject("Brightness phase metadata test");
            Shader shader = null;
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var brightness = new FluxParticleBrightnessModule();
                Assert.IsFalse(brightness.PreserveBlinkPhase);
                if (migrated)
                {
                    JsonUtility.FromJsonOverwrite("{\"preserveBlinkPhase\":true}", brightness);
                }
                var clone = JsonUtility.FromJson<FluxParticleBrightnessModule>(JsonUtility.ToJson(brightness));
                Assert.AreEqual(migrated, clone.PreserveBlinkPhase);
                typeof(FluxParticleAuthoring).GetField("modules", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(authoring, new List<FluxParticleModule> { clone });
                using (var serialized = new SerializedObject(authoring))
                {
                    var module = serialized.FindProperty("modules").GetArrayElementAtIndex(0);
                    Assert.AreEqual(migrated, module.FindPropertyRelative("preserveBlinkPhase").boolValue);
                    var property = module.Copy();
                    var end = property.GetEndProperty();
                    Assert.IsTrue(property.NextVisible(true));
                    do
                    {
                        Assert.AreNotEqual("preserveBlinkPhase", property.name);
                    } while (property.NextVisible(false) && !SerializedProperty.EqualContents(property, end));
                }
                var build = FluxParticleBuildPipeline.Build(authoring);
                var operation = build.Shaders.Compilation.Operations.Single(op => op.Id == "render.brightness");
                Assert.AreEqual(migrated ? "FluxFX_BrightnessRenderWithBlinkPhase" : "FluxFX_BrightnessRender", operation.EntryPoint);
                shader = ShaderUtil.CreateShaderAsset(build.RendererSource, true);
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(m => m.message)));
                Assert.AreEqual(16, FluxParticleBrightnessContract.Module.Parameters.Count);
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
        public void MigrationMarksInlineBlinkDataWithoutChangingOtherSettings()
        {
            var data = string.Concat(_fields.Select(field => "        " + field + ": " +
                (field == "frequency" ? "0.2" : field == "duty" ? "0.4" : "1") + "\n"));
            var original = "    - rid: 456\n      type: {class: FluxParticleBlinkModule, ns: TpLab.Flux.FX.Scripts.Modules, asm: TpLab.Flux.FX.Scripts}\n      data:\n" + data;
            var files = new Dictionary<string, string> { { "inline.prefab", original } };
            var plan = Plan(files);
            Assert.That(plan["inline.prefab"], Does.Contain("        preserveBlinkPhase: 1\n"));
            Assert.That(plan["inline.prefab"], Does.Contain("        period: 5\n"));
            Assert.That(plan["inline.prefab"], Does.Contain("        duty: 0.4\n"));
            files["inline.prefab"] = plan["inline.prefab"];
            Assert.IsEmpty(Plan(files));
        }

        [TestCase(0)]
        [TestCase(16000000)]
        public void NewPulseAndSineUseTheSameIntegerHashAtSmallAndLargeKeys(int firstKey)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("GPU required");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("df8558e756554ae080c94e0f56f6ae7d"));
            var material = new Material(shader);
            var rt = new RenderTexture(64, 64, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var image = new Texture2D(64, 64, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
                material.SetFloat("_PhaseCheck", 1);
                material.SetFloat("_KeyBase", firstKey);
                Graphics.Blit(null, rt, material);
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
                var pixels = image.GetPixels();
                var unique = new HashSet<float>();
                var maximumSineError = 0f;
                var maximumHashError = 0f;
                var offCount = 0;
                for (var i = 0; i < pixels.Length; i++)
                {
                    var key = (uint)(firstKey + i);
                    var value = key + 1723u;
                    unchecked
                    {
                        value ^= value >> 16;
                        value *= 0x7feb352du;
                        value ^= value >> 15;
                        value *= 0x846ca68bu;
                        value ^= value >> 16;
                    }
                    var phase = (value & 65535u) / 65535f;
                    Assert.AreEqual(phase < 0.4f ? 1 : 0, pixels[i].r, "Pulse key " + key);
                    maximumHashError = Mathf.Max(maximumHashError, Mathf.Abs(phase - pixels[i].a));
                    maximumSineError = Mathf.Max(maximumSineError, Mathf.Abs(0.5f + 0.5f * Mathf.Sin(6.2831853f * phase) - pixels[i].g));
                    unique.Add(pixels[i].a);
                    if (pixels[i].r == 0)
                    {
                        offCount++;
                    }
                    if (firstKey == 16000000)
                    {
                        Assert.AreEqual(1, pixels[i].b, "Legacy phase must remain zero at these keys.");
                    }
                }
                Assert.Greater(unique.Count, 3800);
                Assert.Greater(offCount, 2000);
                Assert.Less(offCount, 2900);
                Assert.LessOrEqual(maximumHashError, 0.000001f);
                Assert.LessOrEqual(maximumSineError, 0.000001f);
                Directory.CreateDirectory("Temp/BrightnessPhase");
                File.WriteAllText("Temp/BrightnessPhase/hash-" + firstKey + ".txt",
                    "4096 GPU keys; distinct phases=" + unique.Count + "; off=" + offCount +
                    "; max hash error=" + maximumHashError.ToString("R") + "; max sine error=" + maximumSineError.ToString("R"));
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(image);
            }
        }

        [Test]
        public void AudioDependencyResolvesRegardlessOfRegistrationOrder()
        {
            var root = new GameObject("Brightness dependency test");
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var brightness = new FluxParticleBrightnessModule();
                JsonUtility.FromJsonOverwrite("{\"audioEnabled\":true}", brightness);
                var modules = new List<FluxParticleModule> { brightness, new FluxParticleAudioInputModule() };
                typeof(FluxParticleAuthoring).GetField("modules", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(authoring, modules);
                Assert.DoesNotThrow(() => FluxParticleBuildPipeline.Build(authoring));
                modules.RemoveAt(1);
                var error = Assert.Catch(() => FluxParticleBuildPipeline.Build(authoring));
                Assert.That(error.Message, Does.Contain("audio-input"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InspectorLabelsSwitchBetweenJapaneseAndEnglish()
        {
            var registry = new TpLab.Flux.FX.Editor.Inspector.FluxParticleInspectorModuleRegistry();
            Assert.IsTrue(registry.TryGet(typeof(FluxParticleBrightnessModule), out var metadata));
            Assert.IsFalse(metadata.IsCustom);
            var localization = typeof(FluxParticleBrightnessDefinition).Assembly.GetType("TpLab.Flux.FX.Editor.Localization.L10n", true);
            var language = localization.GetProperty("Language", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = language.GetValue(null);
            try
            {
                language.SetValue(null, TpLab.Flux.FX.Editor.Localization.Language.Japanese);
                Assert.AreEqual("明るさ", metadata.Translate("Brightness"));
                Assert.AreEqual("周期（秒）", metadata.Translate("Period"));
                Assert.AreEqual("正弦波", metadata.Translate("Sine"));
                language.SetValue(null, TpLab.Flux.FX.Editor.Localization.Language.English);
                Assert.AreEqual("Brightness", metadata.Translate("Brightness"));
                Assert.AreEqual("Period", metadata.Translate("Period"));
            }
            finally
            {
                language.SetValue(null, previous);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BuildPreservesColorOrderAndOnlyAllocatesAudioWhenRequested(bool audio)
        {
            var root = new GameObject("Brightness build test");
            Shader shader = null;
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var baseline = FluxParticleBuildPipeline.Build(authoring);
                var brightness = new FluxParticleBrightnessModule();
                JsonUtility.FromJsonOverwrite("{\"audioEnabled\":" + (audio ? "true" : "false") + "}", brightness);
                var modules = new List<FluxParticleModule>();
                if (audio) modules.Add(new FluxParticleAudioInputModule());
                modules.Add(new FluxParticleColorOverLifetimeModule());
                modules.Add(new FluxParticleDistanceFadeModule());
                modules.Add(brightness);
                typeof(FluxParticleAuthoring).GetField("modules", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(authoring, modules);
                var result = FluxParticleBuildPipeline.Build(authoring);
                var ops = result.Shaders.Compilation.Operations.Select(op => op.Id).ToArray();
                Assert.Greater(Array.IndexOf(ops, "render.brightness"), Array.IndexOf(ops, "render.distance-fade"));
                Assert.AreEqual(audio, ops.Contains("brightness.update"));
                if (!audio) Assert.AreEqual(baseline.Shaders.Compilation.StateLayout.Pages.Count, result.Shaders.Compilation.StateLayout.Pages.Count);
                Assert.AreEqual(1, brightness.BaseMinimum);
                Assert.AreEqual(1, brightness.BaseMaximum);
                Assert.AreEqual(0, (int)brightness.Waveform);
                shader = ShaderUtil.CreateShaderAsset(result.RendererSource, true);
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(m => m.message)));
                Assert.AreEqual(FluxParticleBrightnessContract.Module.Parameters.Count + 1,
                    new FluxParticleBrightnessLiveInspectorProvider().Bindings.Count());
            }
            finally
            {
                if (shader != null) Object.DestroyImmediate(shader);
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(0.2f, 5f)]
        [TestCase(0f, 0f)]
        public void FrequencyConvertsWithoutDefaultSubstitution(float frequency, float period)
        {
            Assert.AreEqual(period, FluxParticleBrightnessMigration.Parse(
                FluxParticleBrightnessMigration.ConvertBlinkField("frequency", FluxParticleBrightnessMigration.Number(frequency))["period"]));
        }

        [Test]
        public void MigrationPreservesIdsAndIndividualOverridesAndIsIdempotent()
        {
            var original = string.Concat(new[] { Modification("", Old) }.Concat(_fields.Select(field =>
                Modification("." + field, field == "frequency" ? "0.2" : field == "duty" ? "0.4" : "1"))));
            var files = new Dictionary<string, string> { { "base.prefab", original }, { "variant.prefab", Modification(".frequency", "0") },
                { "scene.unity", Modification(".target", "2") + "unrelated: unchanged\n" } };
            var plan = Plan(files);
            Assert.AreEqual(2, plan.Count);
            Assert.That(plan["base.prefab"], Does.Contain("managedReferences[123].period\n      value: 5\n"));
            Assert.That(plan["base.prefab"], Does.Contain("managedReferences[123].preserveBlinkPhase\n      value: 1\n"));
            Assert.That(plan["variant.prefab"], Does.Not.Contain("preserveBlinkPhase"));
            Assert.That(plan["variant.prefab"], Does.Contain("managedReferences[123].period\n      value: 0\n"));
            Assert.IsFalse(plan.ContainsKey("scene.unity"));
            foreach (var pair in plan) files[pair.Key] = pair.Value;
            Assert.IsEmpty(Plan(files));
        }

        [Test]
        public void MigrationRejectsMissingOrUnreadableFieldsBeforeWriting()
        {
            Assert.Throws<InvalidOperationException>(() => Plan(new Dictionary<string, string> { { "bad.prefab", Modification("", Old) } }));
            Assert.Throws<InvalidOperationException>(() => FluxParticleBrightnessMigration.ConvertBlinkField("frequency", "NaN"));
            Assert.Throws<InvalidOperationException>(() => FluxParticleBrightnessMigration.ConvertBlinkField("frequency", "-1"));
        }

        [Test]
        public void MigrationRestoresMissingLegacyTypeAndPreservesSavedReferenceId()
        {
            var path = FluxParticleTestPaths.TemporaryRoot + "/BrightnessMigration_" + Guid.NewGuid().ToString("N") + ".prefab";
            var root = new GameObject("Missing legacy module migration");
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                using (var serialized = new SerializedObject(authoring))
                {
                    var modules = serialized.FindProperty("modules");
                    modules.arraySize = 1;
                    modules.GetArrayElementAtIndex(0).managedReferenceValue = new FluxParticleBrightnessModule();
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                var original = File.ReadAllText(path);
                var reference = System.Text.RegularExpressions.Regex.Match(original,
                    @"(?m)^    - rid: (?<id>\d+)\r?\n      type: \{class: FluxParticleBrightnessModule[^}]+\}\r?\n      data:\r?\n(?:        [^\r\n]*\r?\n)+");
                Assert.IsTrue(reference.Success, original);
                var id = reference.Groups["id"].Value;
                var legacy = "    - rid: " + id + "\n      type: {class: FluxParticleBlinkModule, ns: TpLab.Flux.FX.Scripts.Modules, asm: TpLab.Flux.FX.Scripts}\n" +
                    "      data:\n        enabled: 1\n        frequency: 0.2\n        duty: 0.4\n        minimum: 0\n" +
                    "        smoothness: 1\n        randomPhase: 1\n        timeMode: 0\n        target: 1\n";
                File.WriteAllText(path, original.Replace(reference.Value, legacy));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var missingRoot = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var missing = missingRoot.GetComponent<FluxParticleAuthoring>();
                    Assert.IsTrue(UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(missing), JsonUtility.ToJson(missing));
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(missingRoot);
                }
                var files = new Dictionary<string, string> { { path, File.ReadAllText(path) } };
                var plan = Plan(files);
                Assert.AreEqual(1, plan.Count);
                File.WriteAllText(path, plan[path]);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var restoredRoot = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var restored = restoredRoot.GetComponent<FluxParticleAuthoring>();
                    Assert.IsFalse(UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(restored));
                    var brightness = (FluxParticleBrightnessModule)restored.Modules.Single();
                    Assert.AreEqual(5, brightness.Period);
                    Assert.AreEqual(0.4f, brightness.Duty);
                    Assert.AreEqual(TpLab.Flux.FX.Udon.FluxParticleBrightnessTarget.Alpha, brightness.Target);
                    Assert.IsTrue(brightness.PreserveBlinkPhase);
                    using (var serialized = new SerializedObject(restored))
                    {
                        Assert.AreEqual(long.Parse(id), serialized.FindProperty("modules").GetArrayElementAtIndex(0).managedReferenceId);
                    }
                    Assert.IsEmpty(Plan(new Dictionary<string, string> { { path, File.ReadAllText(path) } }));
                    Assert.DoesNotThrow(() => FluxParticleBuildPipeline.Build(restored));
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(restoredRoot);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                AssetDatabase.DeleteAsset(path);
            }
        }


        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(1, 0)]
        public void GpuMatchesFrozenLegacyWaveformsAndHdr(int sine, int target)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("GPU required");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("df8558e756554ae080c94e0f56f6ae7d"));
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(m => m.message)));
            var material = new Material(shader);
            var rt = new RenderTexture(64, 64, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var image = new Texture2D(64, 64, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            var maximumError = 0f;
            var cases = 0;
            Directory.CreateDirectory("Temp/BrightnessPhase/Gpu");
            try
            {
                foreach (var frequency in new[] { 0f, 0.2f, 2f })
                foreach (var duty in new[] { 0f, 0.4f, 1f })
                foreach (var smoothness in new[] { 0f, 1f })
                foreach (var time in new[] { 0f, 0.3f, 4.999f, 8f, 77.5f })
                foreach (var timeMode in new[] { 0f, 1f })
                {
                    material.SetVector("_Settings", new Vector4(frequency, duty, 0, smoothness));
                    material.SetVector("_Times", new Vector4(time, time * 0.3f, timeMode, 1));
                    material.SetVector("_Config", new Vector4(sine, target, 0.7f, 0));
                    Graphics.Blit(null, rt, material);
                    RenderTexture.active = rt;
                    image.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
                    foreach (var pixel in image.GetPixels())
                    {
                        maximumError = Mathf.Max(maximumError, Mathf.Max(Mathf.Max(pixel.r, pixel.g), Mathf.Max(pixel.b, pixel.a)));
                    }
                    cases++;
                }
                File.WriteAllText("Temp/BrightnessPhase/Gpu/metrics-" + sine + "-" + target + ".txt",
                    "GPU pixels compared: " + cases * 4096 + "\nMaximum absolute RGBA difference: " + maximumError.ToString("R"));
                for (var output = 1; output <= 2; output++)
                {
                    material.SetVector("_Config", new Vector4(sine, target, 0.7f, output));
                    Graphics.Blit(null, rt, material);
                    RenderTexture.active = rt;
                    image.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
                    File.WriteAllBytes("Temp/BrightnessPhase/Gpu/" + sine + "-" + target + "-" + output + ".exr", image.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));
                }
                Assert.LessOrEqual(maximumError, 0.0002f);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(material);
            }
        }

        static IReadOnlyDictionary<string, string> Plan(IReadOnlyDictionary<string, string> files) =>
            FluxParticleBrightnessMigration.PlanBlink(files);

        [TestCase(1)]
        [TestCase(4)]
        public void PreviewImagesPreserveEmberwayPulseWithMsaa(int samples)
        {
            var brightness = new FluxParticleBrightnessModule();
            JsonUtility.FromJsonOverwrite("{\"waveform\":2,\"period\":5,\"duty\":0.4,\"minimum\":0,\"maximum\":1,\"smoothness\":1,\"randomPhase\":1,\"target\":1,\"preserveBlinkPhase\":true}", brightness);
            var before = CapturePreview(brightness, samples, "before", true);
            var after = CapturePreview(brightness, samples, "after");
            var maximum = 0f;
            var energy = 0f;
            for (var i = 0; i < before.Length; i++)
            {
                energy += before[i].r + before[i].g + before[i].b;
                for (var channel = 0; channel < 4; channel++) maximum = Mathf.Max(maximum, Mathf.Abs(before[i][channel] - after[i][channel]));
            }
            File.WriteAllText("Temp/BrightnessPhase/Gpu/preview-msaa" + samples + ".txt", "RGB energy=" + energy + "; maximum absolute RGBA error=" + maximum.ToString("R"));
            Assert.Greater(energy, 0.01f, "The comparison must contain visible particles.");
            Assert.LessOrEqual(maximum, 0.0001f);
        }

        static Color[] CapturePreview(FluxParticleModule module, int samples, string label, bool reference = false)
        {
            var root = new GameObject("Brightness preview comparison");
            var cameraRoot = new GameObject("Brightness comparison camera");
            var rt = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBFloat) { antiAliasing = samples };
            var image = new Texture2D(128, 128, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                typeof(FluxParticleAuthoring).GetField("modules", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(authoring, new List<FluxParticleModule> { module });
                using (var settings = new SerializedObject(authoring))
                {
                    settings.FindProperty("emission.rate").floatValue = 64;
                    settings.FindProperty("initialVelocity.speed").floatValue = 0;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                var camera = cameraRoot.AddComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = new Vector3(0, 0, -2);
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << 31;
                camera.targetTexture = rt;
                var build = FluxParticleBuildPipeline.Build(authoring);
                if (reference)
                {
                    const string include = "Packages/com.tplab.flux.fx/Editor/Modules/Brightness/Render.hlsl";
                    Assert.That(build.RendererSource, Does.Contain(include));
                    var referencePath = AssetDatabase.GUIDToAssetPath("0daca833cd3161c41a89520aef1b9e31");
                    Assert.IsNotEmpty(referencePath);
                    build = new FluxParticleBuildResult(build.Shaders, build.InitialValues,
                        build.RendererSource.Replace(include, referencePath), build.ModuleBindings);
                }
                using (var preview = new FluxParticleEditorPreview(build, authoring, 64))
                {
                    preview.Step(0.5f);
                    preview.Step(0.5f);
                    preview.Draw(camera, Matrix4x4.identity, 31);
                    camera.Render();
                    RenderTexture.active = rt;
                    image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                    Directory.CreateDirectory("Temp/BrightnessPhase/Gpu");
                    File.WriteAllBytes("Temp/BrightnessPhase/Gpu/preview-msaa" + samples + "-" + label + ".exr", image.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));
                    return image.GetPixels();
                }
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(cameraRoot);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(rt);
            }
        }

        static string Modification(string suffix, string value) =>
            "    - target: {fileID: 1, guid: abc,\n        type: 3}\n      propertyPath: managedReferences[123]" + suffix +
            "\n      value: " + value + "\n      objectReference: {fileID: 0}\n";
    }
}
