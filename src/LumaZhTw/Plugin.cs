using System;
using System.Collections.Generic;
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
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> DebugLogSamples;
        internal static OpenCcConverter Converter;

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

            try
            {
                var t0 = DateTime.UtcNow;
                Converter = OpenCcConverter.FromEmbeddedResources();
                Log.LogInfo($"OpenCC dictionaries loaded in {(DateTime.UtcNow - t0).TotalMilliseconds:F0} ms; self-test: 软件信息 -> {Converter.Convert("软件信息")}");
                var harmony = new Harmony(Guid);
                harmony.PatchAll(typeof(Patches));
                foreach (var m in harmony.GetPatchedMethods())
                    Log.LogInfo("Patched " + m.DeclaringType?.FullName + "." + m.Name);
            }
            catch (Exception e)
            {
                Log.LogError("Initialization failed, mod disabled: " + e);
            }
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
                result = Converter.Convert(text);
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
