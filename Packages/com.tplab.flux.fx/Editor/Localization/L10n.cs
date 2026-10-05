using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.Localization
{
    internal enum Language
    {
        English,
        Japanese
    }

    internal static class L10n
    {
        const string LanguageEditorPrefsKey = "TpLab.FluxFX.Language";
        const string JapanesePoRelativePath = "Editor/Localization/ja.po";

        static readonly Dictionary<string, string> _translations = new();

        static Language? _language;

        internal static Language Language
        {
            get
            {
                if (_language.HasValue) return _language.Value;

                _language = (Language)EditorPrefs.GetInt(LanguageEditorPrefsKey, (int)Language.English);
                LoadTranslations();

                return _language.Value;
            }
            set
            {
                if (Language == value) return;

                _language = value;
                EditorPrefs.SetInt(LanguageEditorPrefsKey, (int)value);
                LoadTranslations();
            }
        }

        internal static string Tr(string text)
        {
            if (Language == Language.English) return text;

            return _translations.TryGetValue(text, out var translated)
                ? translated
                : text;
        }

        static void LoadTranslations()
        {
            _translations.Clear();

            if (!_language.HasValue || _language.Value == Language.English) return;

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(L10n).Assembly);
            if (packageInfo == null) return;

            var path = Path.Combine(packageInfo.resolvedPath, JapanesePoRelativePath);
            if (!File.Exists(path)) return;

            ParsePo(File.ReadAllLines(path, Encoding.UTF8));
        }

        static void ParsePo(string[] lines)
        {
            string msgId = null;
            string msgStr = null;
            var target = PoTarget.None;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("msgid "))
                {
                    AddTranslation(msgId, msgStr);

                    msgId = ParsePoString(line.Substring(6));
                    msgStr = null;
                    target = PoTarget.MsgId;
                    continue;
                }

                if (line.StartsWith("msgstr "))
                {
                    msgStr = ParsePoString(line.Substring(7));
                    target = PoTarget.MsgStr;
                    continue;
                }

                if (line.StartsWith("\""))
                {
                    var value = ParsePoString(line);

                    if (target == PoTarget.MsgId)
                    {
                        msgId += value;
                    }
                    else if (target == PoTarget.MsgStr)
                    {
                        msgStr += value;
                    }

                    continue;
                }

                if (string.IsNullOrEmpty(line))
                {
                    AddTranslation(msgId, msgStr);

                    msgId = null;
                    msgStr = null;
                    target = PoTarget.None;
                }
            }

            AddTranslation(msgId, msgStr);
        }

        static void AddTranslation(string msgId, string msgStr)
        {
            if (string.IsNullOrEmpty(msgId) || string.IsNullOrEmpty(msgStr)) return;

            _translations[msgId] = msgStr;
        }

        static string ParsePoString(string value)
        {
            value = value.Trim();

            if (value.Length < 2 || value[0] != '"' || value[^1] != '"') return string.Empty;

            value = value.Substring(1, value.Length - 2);

            return value
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }

        enum PoTarget
        {
            None,
            MsgId,
            MsgStr
        }
    }
}