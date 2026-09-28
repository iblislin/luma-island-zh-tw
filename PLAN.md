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
- **Glossary:** optional user JSON/CSV term overrides, applied after/around OpenCC.
- **TMP fallback hook:** optional `TMP_Text` text-setter patch for strings that bypass Localization.
- **Config:** BepInEx config toggle for zh-TW.

## Roadmap

1. **Done (2026-09):** OpenCC `s2twp` conversion via a Localization hook, plus config toggle. In-game visual check with the language set to 简体中文 still pending.
2. Noto Sans TC dynamic font asset.
3. Glossary override and TMP fallback hook.
4. Polish: handle game updates; list of known untranslated strings.

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
- Glossary format: JSON or CSV, and ordering relative to OpenCC.
- Font atlas size/settings for Noto Sans TC; ship OTF or subset?
- How to expose the toggle in game (config file only vs. in-game menu).
