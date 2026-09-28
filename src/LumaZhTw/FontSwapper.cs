using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace LumaZhTw
{
    /// <summary>
    /// Repoints the game's dynamic "NotoSansSC-Regular SDF" TMP font asset at Noto Sans TC and clears its
    /// glyph tables, so every glyph (including the pre-baked ones) is regenerated from the TC font.
    /// The asset object, its atlas texture and all materials/shaders stay the same, so the game's custom
    /// text materials keep working. Any failure is logged once and the original font is left alone.
    /// </summary>
    internal static class FontSwapper
    {
        public const string TargetAssetName = "NotoSansSC-Regular SDF";
        public const string FontFileName = "NotoSansTC-Regular.otf";

        private static readonly HashSet<int> Processed = new HashSet<int>();
        private static Font _tcFont;
        private static string _fontPath;
        private static bool _failed, _redirect;
        private static float _nextCheck;
        private static string _lastCode = "<none>";

        internal static void Init(string pluginDir, Harmony harmony)
        {
            _fontPath = Path.Combine(pluginDir ?? "", FontFileName);
            if (!File.Exists(_fontPath)) { Fail("font file not found: " + _fontPath); return; }
            harmony.PatchAll(typeof(RedirectPatch));
            // Own hidden runner: some games destroy the BepInEx manager object, which would stop Update().
            var go = new GameObject("LumaZhTw.FontSwapper") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }

        internal static void Tick()
        {
            if (_failed || _fontPath == null || !Plugin.UseTraditionalFont.Value) return;
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 1f;
            try
            {
                var locale = LocalizationSettings.HasSettings ? LocalizationSettings.SelectedLocale : null;
                string code = locale?.Identifier.Code;
                if (code != _lastCode) { _lastCode = code; Plugin.Log.LogInfo($"Font: selected locale '{code}'" + (code == "zh" ? ", will use Noto Sans TC" : "")); }
                if (code != "zh") return;
                foreach (var fa in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                {
                    if (fa == null || fa.name != TargetAssetName || !Processed.Add(fa.GetInstanceID())) continue;
                    Swap(fa);
                }
            }
            catch (Exception e) { Fail(e.ToString()); }
        }

        private static Font LoadTcFont()
        {
            if (_tcFont != null) return _tcFont;
            // In a player build, new Font(path) is expected to load the file; verify, else redirect FontEngine.
            var f = new Font(_fontPath) { name = "NotoSansTC-Regular", hideFlags = HideFlags.DontUnloadUnusedAsset };
            _redirect = false;
            if (FontEngine.LoadFontFace(f, 90) != FontEngineError.Success || FontEngine.GetFaceInfo().familyName != "Noto Sans TC")
            {
                _redirect = true; // make FontEngine.LoadFontFace(font, size) load the file for this Font
                Plugin.Log.LogInfo("Font: new Font(path) did not load the file; redirecting FontEngine.LoadFontFace to the file path");
            }
            _tcFont = f;
            return f;
        }

        private static void Swap(TMP_FontAsset fa)
        {
            try
            {
                var old = fa.faceInfo;
                if (fa.atlasPopulationMode != AtlasPopulationMode.Dynamic) { Fail("asset is not dynamic"); return; }
                if (fa.atlasTexture == null || !fa.atlasTexture.isReadable) { Fail("atlas texture not readable"); return; }
                var font = LoadTcFont();
                int size = old.pointSize;
                if (FontEngine.LoadFontFace(font, size) != FontEngineError.Success) { Fail("LoadFontFace failed"); return; }
                FaceInfo tc = FontEngine.GetFaceInfo();
                if (tc.familyName != "Noto Sans TC") { Fail("loaded face is '" + tc.familyName + "', not Noto Sans TC"); return; }
                int before = fa.characterTable.Count;
                AccessTools.Field(typeof(TMP_FontAsset), "m_SourceFontFile").SetValue(fa, font);
                fa.faceInfo = tc;
                fa.ClearFontAssetData(false);
                // Regenerate the glyphs that were pre-baked, plus a TC probe.
                var chars = "說骨角青令強值選返過這遊";
                fa.TryAddCharacters(chars, out string missing);
                Plugin.Log.LogInfo($"Font: '{fa.name}' now renders from Noto Sans TC (size {size}, padding {fa.atlasPadding}, " +
                                   $"atlas {fa.atlasWidth}x{fa.atlasHeight}, {fa.atlasRenderMode}); cleared {before} chars; probe missing '{missing}'");
                Plugin.Log.LogInfo($"Font metrics SC -> TC: lineHeight {old.lineHeight}->{tc.lineHeight}, ascent {old.ascentLine}->{tc.ascentLine}, " +
                                   $"descent {old.descentLine}->{tc.descentLine}, scale {old.scale}->{tc.scale}");
                int dirty = 0;
                foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
                    if (t != null && t.font == fa) { t.SetAllDirty(); dirty++; }
                Plugin.Log.LogInfo($"Font: refreshed {dirty} text components");
            }
            catch (Exception e) { Fail(e.ToString()); }
        }

        private class Runner : MonoBehaviour
        {
            private void Update() => Tick();
        }

        private static void Fail(string why)
        {
            if (_failed) return;
            _failed = true;
            Plugin.Log.LogError("Font: Traditional font disabled, keeping the original font: " + why);
        }

        [HarmonyPatch(typeof(FontEngine), nameof(FontEngine.LoadFontFace), new[] { typeof(Font), typeof(int) })]
        private static class RedirectPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(Font font, int pointSize, ref FontEngineError __result)
            {
                if (!_redirect || font == null || !ReferenceEquals(font, _tcFont)) return true;
                __result = FontEngine.LoadFontFace(_fontPath, pointSize);
                return false;
            }
        }
    }
}
