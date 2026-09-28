using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Tables;

namespace LumaZhTw
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "iblislin.luma.zhtw";
        public const string Name = "Luma Island zh-TW";
        public const string Version = "0.3.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> DebugLogSamples;
        internal static ConfigEntry<bool> UseTraditionalFont;
        internal static OpenCcConverter Converter;
        internal static Glossary Glossary = new Glossary();
        public const string GlossaryFileName = Guid + ".glossary.json";

        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly object CacheLock = new object();
        private static bool _errorLogged;
        private static int _samplesLogged;
        private const int MaxSamples = 20;

        private void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("General", "Enabled", true,
                "Convert Simplified Chinese (zh) text to Traditional Chinese (Taiwan, OpenCC s2twp).");
            DebugLogSamples = Config.Bind("Debug", "DebugLogSamples", false,
                "Log the first 20 conversions (original -> converted) at Info level.");

            UseTraditionalFont = Config.Bind("General", "UseTraditionalFont", true,
                "When the locale is zh, render with Noto Sans TC (Taiwan glyph shapes) instead of Noto Sans SC. Takes effect on restart.");

            try
            {
                var t0 = DateTime.UtcNow;
                Converter = OpenCcConverter.FromEmbeddedResources();
                Log.LogInfo($"OpenCC dictionaries loaded in {(DateTime.UtcNow - t0).TotalMilliseconds:F0} ms; self-test: 软件信息 -> {Converter.Convert("软件信息")}");
                LoadGlossary();
                RunSelfChecks();
                var harmony = new Harmony(Guid);
                harmony.PatchAll(typeof(Patches));
                if (UseTraditionalFont.Value)
                {
                    try { FontSwapper.Init(Path.GetDirectoryName(Info.Location), harmony); }
                    catch (Exception e) { Log.LogError("Font: init failed, keeping the original font: " + e); }
                }
                foreach (var m in harmony.GetPatchedMethods())
                    Log.LogInfo("Patched " + m.DeclaringType?.FullName + "." + m.Name);
            }
            catch (Exception e)
            {
                Log.LogError("Initialization failed, mod disabled: " + e);
            }
        }


        // Loaded once per game start; the in-memory cache stores the final (OpenCC + glossary) result
        // and starts empty each run, so edits to the user glossary take effect on restart.
        private void LoadGlossary()
        {
            var g = new Glossary();
            using (var st = typeof(Plugin).Assembly.GetManifestResourceStream("LumaZhTw.Glossary.default.json"))
            using (var r = new StreamReader(st, System.Text.Encoding.UTF8))
                g.Merge(r.ReadToEnd());
            Log.LogInfo($"Glossary: {g.Count} built-in entries");
            var dirs = new[] { Path.GetDirectoryName(Info.Location), Paths.ConfigPath };
            foreach (var dir in dirs)
            {
                string path = Path.Combine(dir ?? "", GlossaryFileName);
                if (!File.Exists(path)) continue;
                try
                {
                    g.Merge(File.ReadAllText(path, System.Text.Encoding.UTF8));
                    Log.LogInfo($"Glossary: merged user file {path}");
                }
                catch (Exception e) { Log.LogError($"Glossary: ignoring invalid user file {path}: {e.Message}"); }
            }
            Glossary = g;
            lock (CacheLock) Cache.Clear();
            Log.LogInfo($"Glossary loaded: {g.Count} entries, fingerprint {g.Fingerprint()}");
        }

        internal static string ConvertFull(string text) => Glossary.Apply(Converter.Convert(text));

        private static void RunSelfChecks()
        {
            // Checks the built-in rules through the full pipeline; a user glossary may legitimately change these.
            var checks = new[]
            {
                ("视频", "影像"), ("视频设置", "影像設定"), ("视频游戏", "電子遊戲"),
                ("图纸", "藍圖"), ("工作台图纸", "工作臺藍圖"), ("软件信息", "軟體資訊"),
            };
            int ok = 0;
            foreach (var (src, want) in checks)
            {
                string got = ConvertFull(src);
                if (got == want) ok++;
                else Log.LogWarning($"Self-check: {src} -> {got}, expected {want}");
            }
            Log.LogInfo($"Self-check: {ok}/{checks.Length} passed (视频设置 -> {ConvertFull("视频设置")}, 图纸 -> {ConvertFull("图纸")})");
        }

        private static string _lastLocale;

        private static bool IsSimplifiedChinese(LocalizationTable table)
        {
            if (table == null) return false;
            string code = table.LocaleIdentifier.Code;
            if (code != _lastLocale)
            {
                _lastLocale = code;
                Log.LogInfo($"String table locale in use: '{code}'" + (code == "zh" ? " (converting)" : " (not zh, passing through)"));
            }
            return code == "zh" || code == "zh-Hans" || code == "zh-CN";
        }

        internal static string Process(LocalizationTable table, string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text) || Converter == null || !Enabled.Value) return text;
                if (!IsSimplifiedChinese(table)) return text;
                string result;
                lock (CacheLock)
                {
                    if (Cache.TryGetValue(text, out result)) return result;
                }
                result = ConvertFull(text);
                lock (CacheLock)
                {
                    Cache[text] = result;
                    if (DebugLogSamples.Value && _samplesLogged < MaxSamples && result != text)
                    {
                        _samplesLogged++;
                        Log.LogInfo($"[sample {_samplesLogged}] {text} -> {result}");
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                if (!_errorLogged) { _errorLogged = true; Log?.LogError("Conversion failed, returning original: " + e); }
                return text;
            }
        }
    }

    internal static class Patches
    {
        // Every StringTableEntry.GetLocalizedString overload (and hence LocalizedStringDatabase and
        // LocalizedString) funnels into this one, so patching only it avoids converting twice.
        [HarmonyPatch(typeof(StringTableEntry), nameof(StringTableEntry.GetLocalizedString),
            new[] { typeof(IFormatProvider), typeof(IList<object>), typeof(PseudoLocale) })]
        [HarmonyPostfix]
        private static void GetLocalizedStringPostfix(StringTableEntry __instance, ref string __result)
        {
            __result = Plugin.Process(__instance?.Table, __result);
        }

        // Raw value accessor; the game's Yarn dialogue line provider reads it directly.
        [HarmonyPatch(typeof(TableEntry), nameof(TableEntry.LocalizedValue), MethodType.Getter)]
        [HarmonyPostfix]
        private static void LocalizedValuePostfix(TableEntry __instance, ref string __result)
        {
            if (__instance is StringTableEntry)
                __result = Plugin.Process(__instance.Table, __result);
        }
    }
}
