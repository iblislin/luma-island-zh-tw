# Plan

## Background / demand

- **Current locales:** the `localization-locales` bundle (checked 2026-09-28) lists 16 locales. Chinese is `zh` (Simplified) only; there is no Traditional Chinese locale.
- **Earlier attempt:** a popular Traditional-Chinese Steam review (2024-11-28) says a Traditional Chinese option existed at launch but was "just Simplified→Traditional conversion". It is no longer present; the reason is unknown.
- **Our angle:** close exactly that quality gap with `s2twp` Taiwan phrasing, Taiwan-standard glyphs from Noto Sans TC, and a glossary.
- **Demand:** 215 Steam reviews in Traditional Chinese (156 positive / 59 negative) as of 2026-09. No Steam discussion threads request Traditional Chinese, and there is no developer statement about it.
- **Reference:** in 2026-07 the developers declined a Czech request ("no plans to implement Czech language support").

## Goals

- Show Luma Island in Traditional Chinese (Taiwan) by converting the shipped Simplified Chinese at runtime with OpenCC `s2twp`.
- Render with Noto Sans TC for Taiwan-standard glyph shapes.
- Stay small, transparent and easy for the developers to adopt officially.

## Non-goals

- No hand translation of the whole game.
- No game text, DLLs, decompiled code or assets in this repo.
- Not yet: Hong Kong variant (`s2hk`), human-reviewed glossary contributions.

## Technical approach

- **Platform:** Unity 2021.3, Mono. BepInEx 5 + Harmony. Game logic in `AzureValley.dll`; text via Unity Localization (`Unity.Localization.dll`), string tables in addressable bundles `localization-string-tables-<lang>`.
- **Build refs:** game DLLs are referenced from a local, git-ignored `GameRefs/` folder.
- **String hook:** patch the Localization lookup (candidates: StringTable entry's localized value, or `LocalizedString.GetLocalizedString`; exact target to confirm by decompiling). Convert only when the locale is Simplified Chinese and the zh-TW option is on. Cache results in a dictionary.
- **Font:** the SC asset `NotoSansSC-Regular SDF` is Dynamic, multi-atlas, 657 pre-baked chars, with the full Noto Sans SC source embedded (30,890 codepoints), so Traditional characters already render with PRC shapes. Create a dynamic `TMP_FontAsset` from Noto Sans TC at runtime; make it primary (or first fallback) in zh-TW mode; keep SC as fallback.
- **Glossary:** built-in JSON glossary embedded in the DLL, plus an optional user JSON file (config or plugin folder) that overrides it. It runs after OpenCC on the Traditional text: longest match first, no cascading. The cache stores the final result and is rebuilt every game start. Rules are checked against the full string table with `tools/audit_terms.py`.
- **TMP fallback hook:** optional `TMP_Text` text-setter patch for strings that bypass Localization.
- **Config:** BepInEx config toggle for zh-TW.

## Roadmap

1. **Done (2026-09):** OpenCC `s2twp` conversion via a Localization hook, plus config toggle. Verified in game (locale zh): main-menu strings are converted, e.g. 音频→音訊, 视频→影片. Glyphs still use the SC font.
2. **Done (2026-09):** glossary. It has built-in rules (视频→影像, 视频游戏→電子遊戲, 图纸→藍圖) and user overrides, and was verified in game (视频 -> 影像). More terms will come from play reports.
3. TMP fallback hook, if strings that bypass Localization turn up.
4. Polish: handle game updates; list of known untranslated strings.
5. **Done (2026-09):** Noto Sans TC (Taiwan glyph shapes). The SC dynamic asset is repointed in place at `NotoSansTC-Regular.otf` (`new Font(path)` loads the file in the player; a `FontEngine.LoadFontFace` path redirect is the fallback) and its tables are cleared. Face metrics at size 90 are identical to SC (line height 130.32, ascent 104.4, descent -25.92), so layout is unchanged. Log-verified in game; the main-menu screenshot at 1080p is not visually conclusive.

## Risks

| Risk | Mitigation |
|---|---|
| Game updates rename hooked methods | Hook public Unity Localization APIs where possible; fail safe and log |
| UI text bypasses Localization | Optional TMP fallback hook |
| Word-level conversion errors | User glossary |
| Performance | Cache conversions; convert lazily |

## Open questions

- ~~Hook target~~: `StringTableEntry.GetLocalizedString(IFormatProvider, IList<object>, PseudoLocale)` + `TableEntry.LocalizedValue` getter.
- ~~OpenCC runtime~~: small C# port with OpenCC's text dictionaries embedded.
- ~~Glossary format~~: JSON, applied after OpenCC.
- ~~Font settings~~: reuse the SC asset (size 90, padding 9, 1024x1024 multi-atlas, SDFAA); ship the official SubsetOTF TC Regular (5.4 MB) unmodified.
- How to expose the toggle in game (config file only vs. in-game menu).
