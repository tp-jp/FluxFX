using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TpLab.Flux.FX.Editor.Authoring
{
    /// <summary>
    /// SerializeReferenceのIDとPrefabの項目別Overrideを保持する明示的なBrightness変換。
    /// 全ファイルの検証と変換計画を完了してから書き込みます。不明な形式は保存せず拒否します。
    /// </summary>
    public static class FluxParticleBrightnessMigration
    {
        const string NewType = "TpLab.Flux.FX.Scripts TpLab.Flux.FX.Scripts.Modules.FluxParticleBrightnessModule";
        const string NewYamlType = "{class: FluxParticleBrightnessModule, ns: TpLab.Flux.FX.Scripts.Modules, asm: TpLab.Flux.FX.Scripts}";
        const string BlinkType = "TpLab.Flux.FX.Scripts TpLab.Flux.FX.Scripts.Modules.FluxParticleBlinkModule";
        static readonly string[] _blinkFields = { "enabled", "frequency", "duty", "minimum", "smoothness", "randomPhase", "timeMode", "target" };
        static readonly IReadOnlyDictionary<string, string> _blinkAdditions = new Dictionary<string, string>
        {
            { "baseMinimum", "1" }, { "baseMaximum", "1" }, { "waveform", "2" },
            { "maximum", "1" }, { "periodMinimum", "1" }, { "periodMaximum", "1" }, { "audioEnabled", "0" },
            { "audioBoost", "2" }, { "audioAttack", "0.025" }, { "audioRelease", "0.12" }, { "preserveBlinkPhase", "1" }
        };
        static readonly Regex _modification = new Regex(@"(?m)^    - target: \{[^}]+\}\r?\n      propertyPath: managedReferences\[(?<id>\d+)\](?:\.(?<field>[^\r\n]+))?\r?\n      value: (?<value>[^\r\n]*)\r?\n      objectReference: \{[^}]+\}\r?\n");
        static readonly Regex _reference = new Regex(@"(?m)^    - rid: (?<id>\d+)\r?\n      type: (?<type>\{[^}]+\})\r?\n      data:\r?\n(?<data>(?:        [^\r\n]*\r?\n)+)");

        /// <summary>
        /// 保存済みの旧BlinkデータをBrightnessへ変換します。旧型・Udon APIには依存しません。
        /// </summary>
        [MenuItem("Tools/FluxFX/Migration/Migrate Blink to Brightness")]
        public static void RunBlink()
        {
            Run(BlinkType, "FluxParticleBlinkModule", _blinkFields, ConvertBlinkField, _blinkAdditions);
        }

        /// <summary>
        /// 位相を保持する移行情報を含め、Blinkからの変換計画を作成します。ファイルは変更しません。
        /// </summary>
        /// <param name="files">パスをキーとする保存済みPrefab／Sceneの本文</param>
        /// <returns>変更が必要なファイルの変換後の本文</returns>
        public static IReadOnlyDictionary<string, string> PlanBlink(IReadOnlyDictionary<string, string> files) =>
            Plan(files, BlinkType, "FluxParticleBlinkModule", _blinkFields, ConvertBlinkField, _blinkAdditions);

        /// <summary>
        /// Blinkの保存値をBrightnessへ変換します。0 Hzは周期0（倍率1）として保持します。
        /// </summary>
        public static IReadOnlyDictionary<string, string> ConvertBlinkField(string field, string value)
        {
            if (field == "frequency")
            {
                var frequency = Parse(value);
                if (frequency < 0) throw new InvalidOperationException("Negative Blink frequency cannot be migrated.");
                return new Dictionary<string, string> { { "period", Number(frequency == 0 ? 0 : 1 / frequency) } };
            }
            return new Dictionary<string, string> { { field, value } };
        }

        /// <summary>
        /// 外部Moduleが自身の変換規則を指定して、Assets内のPrefabと保存済みSceneを移行します。
        /// 未保存Scene・未知の値・同じ参照IDの異なる型・Brightness併用は変更前に拒否します。
        /// </summary>
        public static void Run(string oldType, string oldClass, IReadOnlyCollection<string> fields,
            Func<string, string, IReadOnlyDictionary<string, string>> convert, IReadOnlyDictionary<string, string> additions)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before migration.");
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.scene.isDirty)
                throw new InvalidOperationException("The open Prefab has unsaved changes. Migration has not written any files.");
            for (var i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                if (EditorSceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("An open Scene has unsaved changes. Migration has not written any files.");
            }
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
                .Concat(AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })).Select(AssetDatabase.GUIDToAssetPath).Distinct().ToArray();
            var originals = paths.ToDictionary(path => path, File.ReadAllText);
            if (originals.Any(pair => !pair.Value.StartsWith("%YAML", StringComparison.Ordinal)))
                throw new InvalidOperationException("Migration requires readable text-serialized Prefabs and Scenes. No files were written.");
            var plan = Plan(originals, oldType, oldClass, fields, convert, additions);
            var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            var ordered = new List<string>();
            var visiting = new HashSet<string>();
            foreach (var path in plan.Keys) Order(path, plan, ordered, visiting);
            var directory = "Temp/BrightnessIntegration/Migration-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fffffff");
            Directory.CreateDirectory(directory);
            var report = new List<string>();
            foreach (var path in ordered)
            {
                if (File.ReadAllText(path) != originals[path]) throw new IOException("File changed during migration planning: " + path);
                var backup = Path.Combine(directory, path);
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.Copy(path, backup);
                report.Add(path);
            }
            File.WriteAllLines(Path.Combine(directory, "files.txt"), report);
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in ordered) File.WriteAllText(path, plan[path]);
            }
            catch
            {
                foreach (var path in ordered) File.Copy(Path.Combine(directory, path), path, true);
                throw;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            if (ordered.Count > 0 && sceneSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
            }
            UnityEngine.Debug.Log("Brightness migration: " + ordered.Count + " files. Backup: " + directory);
        }

        /// <summary>
        /// ファイルを変更せず変換差分を作成します。既に移行済みなら空の結果を返します。
        /// 参照IDが異なる型で再利用される構成は、安全な自動変換の範囲外として拒否します。
        /// </summary>
        public static IReadOnlyDictionary<string, string> Plan(IReadOnlyDictionary<string, string> files,
            string oldType, string oldClass, IReadOnlyCollection<string> fields,
            Func<string, string, IReadOnlyDictionary<string, string>> convert, IReadOnlyDictionary<string, string> additions)
        {
            var ids = new HashSet<string>();
            var types = new Dictionary<string, HashSet<string>>();
            foreach (var text in files.Values)
            {
                foreach (Match match in _modification.Matches(text))
                {
                    if (match.Groups["field"].Success) continue;
                    Record(types, match.Groups["id"].Value, match.Groups["value"].Value.Split('.').Last());
                    if (match.Groups["value"].Value == oldType) ids.Add(match.Groups["id"].Value);
                }
                foreach (Match match in _reference.Matches(text))
                {
                    var type = Regex.Match(match.Groups["type"].Value, @"class: ([^,}]+)").Groups[1].Value;
                    Record(types, match.Groups["id"].Value, type);
                    if (type == oldClass) ids.Add(match.Groups["id"].Value);
                }
            }
            foreach (var id in ids)
            {
                if (types[id].Count != 1) throw new InvalidOperationException("Ambiguous managed reference ID: " + id);
            }
            var result = new Dictionary<string, string>();
            foreach (var pair in files)
            {
                var text = pair.Value;
                if ((text.Contains(oldType) || text.Contains("class: " + oldClass + ",")) && text.Contains("FluxParticleBrightnessModule"))
                    throw new InvalidOperationException("Both legacy and standard Brightness exist; resolve separately: " + pair.Key);
                var declarations = _modification.Matches(text).Cast<Match>().Where(m =>
                    !m.Groups["field"].Success && m.Groups["value"].Value == oldType).ToArray();
                foreach (var declaration in declarations)
                {
                    var id = declaration.Groups["id"].Value;
                    var present = _modification.Matches(text).Cast<Match>().Where(m => m.Groups["id"].Value == id && m.Groups["field"].Success)
                        .Select(m => m.Groups["field"].Value).ToArray();
                    if (fields.Any(field => !present.Contains(field)))
                        throw new InvalidOperationException("Incomplete legacy settings for " + pair.Key + "/" + id + ". No default substitution.");
                    var converted = new Dictionary<string, string>(additions.ToDictionary(entry => entry.Key, entry => entry.Value));
                    foreach (Match fieldMatch in _modification.Matches(text))
                    {
                        if (fieldMatch.Groups["id"].Value != id || !fieldMatch.Groups["field"].Success) continue;
                        foreach (var entry in convert(fieldMatch.Groups["field"].Value, fieldMatch.Groups["value"].Value))
                            converted[entry.Key] = entry.Value;
                    }
                    ValidateComplete(converted);
                }
                text = _modification.Replace(text, match =>
                {
                    var id = match.Groups["id"].Value;
                    if (!ids.Contains(id)) return match.Value;
                    if (!match.Groups["field"].Success)
                    {
                        if (match.Groups["value"].Value != oldType) throw new InvalidOperationException("Unresolved reference type: " + pair.Key);
                        return match.Value.Replace(oldType, NewType) + string.Concat(additions.Select(entry =>
                            ReplaceModification(match, entry.Key, entry.Value)));
                    }
                    var field = match.Groups["field"].Value;
                    if (!fields.Contains(field)) throw new InvalidOperationException("Unknown legacy field: " + pair.Key + "/" + field);
                    ValidateNumber(match.Groups["value"].Value);
                    return string.Concat(convert(field, match.Groups["value"].Value).Select(entry =>
                    {
                        ValidateSetting(entry.Key, entry.Value);
                        return ReplaceModification(match, entry.Key, entry.Value);
                    }));
                });
                text = _reference.Replace(text, match =>
                {
                    if (!ids.Contains(match.Groups["id"].Value)) return match.Value;
                    var values = Regex.Matches(match.Groups["data"].Value, @"(?m)^        (?<name>\w+): (?<value>[^\r\n]+)\r?$")
                        .Cast<Match>().ToDictionary(m => m.Groups["name"].Value, m => m.Groups["value"].Value);
                    if (values.Count != fields.Count || fields.Any(field => !values.ContainsKey(field)))
                        throw new InvalidOperationException("Incomplete or unknown legacy fields: " + pair.Key);
                    var converted = new Dictionary<string, string>();
                    foreach (var value in values)
                    {
                        ValidateNumber(value.Value);
                        foreach (var entry in convert(value.Key, value.Value)) converted.Add(entry.Key, entry.Value);
                    }
                    foreach (var entry in additions) converted.Add(entry.Key, entry.Value);
                    ValidateComplete(converted);
                    var newline = match.Value.Contains("\r\n") ? "\r\n" : "\n";
                    return match.Value.Substring(0, match.Groups["type"].Index - match.Index) + NewYamlType + newline + "      data:" + newline +
                        string.Concat(converted.Select(entry => "        " + entry.Key + ": " + entry.Value + newline));
                });
                if (text.Contains(oldType) || text.Contains("class: " + oldClass + ","))
                    throw new InvalidOperationException("Unreadable legacy reference remains in " + pair.Key + ". No files were written.");
                if (text != pair.Value) result.Add(pair.Key, text);
            }
            return result;
        }

        /// <summary>
        /// 保存値を有限の単精度数として読み取ります。
        /// </summary>
        public static float Parse(string value)
        {
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || float.IsNaN(result) || float.IsInfinity(result))
                throw new InvalidOperationException("Unreadable numeric setting: " + value);
            return result;
        }

        /// <summary>
        /// 単精度の値を往復可能な保存表記へ変換します。
        /// </summary>
        public static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        static void ValidateNumber(string value) => Parse(value);

        static void ValidateComplete(IReadOnlyDictionary<string, string> values)
        {
            var required = new[] { "enabled", "baseMinimum", "baseMaximum", "waveform", "period", "periodMinimum", "periodMaximum",
                "randomPhase", "timeMode", "minimum", "maximum", "duty", "smoothness", "target", "audioEnabled", "audioBoost", "audioAttack", "audioRelease" };
            var metadataCount = values.ContainsKey("preserveBlinkPhase") ? 1 : 0;
            if (values.Count != required.Length + metadataCount || required.Any(field => !values.ContainsKey(field)))
                throw new InvalidOperationException("The converted Brightness settings are incomplete. No files were written.");
            foreach (var value in values) ValidateSetting(value.Key, value.Value);
        }

        static void ValidateSetting(string field, string value)
        {
            var number = Parse(value);
            var minimum = field == "periodMinimum" || field == "periodMaximum" ? 0.0001f :
                field == "audioAttack" || field == "audioRelease" ? 0.01f : 0;
            var maximum = field == "duty" || field == "smoothness" || field == "randomPhase" || field == "timeMode" ||
                field == "enabled" || field == "audioEnabled" || field == "preserveBlinkPhase" ? 1 : field == "waveform" || field == "target" ? 2 : float.MaxValue;
            if (number < minimum || number > maximum ||
                ((field == "timeMode" || field == "enabled" || field == "audioEnabled" || field == "preserveBlinkPhase" || field == "waveform" || field == "target") && number != (int)number))
                throw new InvalidOperationException("Invalid converted setting: " + field + " = " + value);
        }

        static void Record(Dictionary<string, HashSet<string>> types, string id, string type)
        {
            if (!types.TryGetValue(id, out var values))
            {
                types.Add(id, values = new HashSet<string>());
            }
            values.Add(type);
        }

        static string ReplaceModification(Match match, string field, string value)
        {
            var property = "managedReferences[" + match.Groups["id"].Value + "]" + (match.Groups["field"].Success ? "." + match.Groups["field"].Value : "");
            var replacement = "managedReferences[" + match.Groups["id"].Value + "]." + field;
            return match.Value.Replace("propertyPath: " + property, "propertyPath: " + replacement)
                .Replace("value: " + match.Groups["value"].Value, "value: " + value);
        }

        static void Order(string path, IReadOnlyDictionary<string, string> plan, List<string> ordered, HashSet<string> visiting)
        {
            if (ordered.Contains(path)) return;
            if (!visiting.Add(path)) throw new InvalidOperationException("Cyclic Prefab dependency: " + path);
            foreach (var dependency in AssetDatabase.GetDependencies(path, false))
            {
                if (dependency != path && plan.ContainsKey(dependency))
                {
                    Order(dependency, plan, ordered, visiting);
                }
            }
            visiting.Remove(path);
            ordered.Add(path);
        }
    }
}
