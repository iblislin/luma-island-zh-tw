# Luma Island – Traditional Chinese (Taiwan) mod

An **unofficial** fan mod that adds Traditional Chinese (Taiwan, zh-TW) to [Luma Island](https://store.steampowered.com/app/2408820/) by Feel Free Games.

> Status: OpenCC text conversion, a built-in glossary and the Noto Sans TC font are implemented. See [PLAN.md](PLAN.md).

## How it works

Luma Island already ships Simplified Chinese. This mod reuses it:

1. **Runtime conversion with OpenCC.** A BepInEx 5 + Harmony plugin hooks the Unity Localization string lookup. When the game locale is Simplified Chinese and the mod's zh-TW option is on, each string is converted with [OpenCC](https://github.com/BYVoid/OpenCC) using the `s2twp` config (Simplified to Taiwan standard, including Taiwan phrases). Results are cached.
2. **Taiwan glyphs with Noto Sans TC.** The game's Noto Sans SC font already covers nearly all CJK ideographs, but with PRC glyph shapes. When the locale is `zh`, the mod repoints the game's dynamic `NotoSansSC-Regular SDF` TMP font asset at [Noto Sans TC](https://fonts.google.com/noto/specimen/Noto+Sans+TC) and clears its glyph tables, so all glyphs (including the 657 pre-baked ones) are regenerated from TC. The asset, atlas texture and the game's custom text materials stay the same.
3. **Glossary.** After OpenCC, a small glossary fixes game-specific terms that automatic conversion gets wrong (see [Glossary](#glossary)).

**For the developers:** this is a cheap path to official Traditional Chinese. Run your Simplified Chinese tables through OpenCC `s2twp` (Apache-2.0), add Noto Sans TC (OFL-1.1, free to ship), and have a native speaker review key terms. A plain conversion alone was apparently tried before, so this mod shows the fuller variant: Taiwan phrasing, TC glyphs and a glossary. We'd be happy to share anything we learn.

## Install

1. Download BepInEx 5.4.x `BepInEx_win_x64_*.zip` from the [official releases](https://github.com/BepInEx/BepInEx/releases) and extract it into the game folder (next to `Luma Island.exe`). This adds `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` and a `BepInEx/` folder. The game's exe and DLLs are not modified.
2. Copy `LumaZhTw.dll`, `fonts/NotoSansTC-Regular.otf` and `fonts/OFL.txt` into `BepInEx/plugins/LumaZhTw/`.
3. In the game, set the language to **简体中文** (Simplified Chinese). The mod converts it to Traditional Chinese (Taiwan).

Config (`BepInEx/config/iblislin.luma.zhtw.cfg`, created on first run):

- `[General] Enabled` (default `true`): turn conversion on or off.
- `[General] UseTraditionalFont` (default `true`): render zh text with Noto Sans TC (Taiwan glyph shapes). Restart to apply.
- `[Debug] DebugLogSamples` (default `false`): log the first 20 conversions to `BepInEx/LogOutput.log`.

If the font file is missing or the swap fails, the mod logs one error and keeps the original font.

### Glossary

Rules run on the **Traditional** text after OpenCC `s2twp`. The text is scanned left to right. At each position the longest matching key wins, and scanning continues after the matched text. Replacements are never scanned again, so rules do not cascade.

Built-in rules ([`src/LumaZhTw/Glossary.default.json`](src/LumaZhTw/Glossary.default.json), embedded in the DLL):

| Source | s2twp | Glossary | Why |
|---|---|---|---|
| 视频 | 影片 | 影像 | the Options menu "Video" (display) tab |
| 视频游戏 | 影片遊戲 | 電子遊戲 | "video game" in the credits; the longer key keeps the 影片 rule out |
| 图纸 | 圖紙 | 藍圖 | blueprints (the game also uses 蓝图) |

To add your own rules or override the built-in ones, create `iblislin.luma.zhtw.glossary.json` in `BepInEx/config/` or next to `LumaZhTw.dll`. When both files exist, both are loaded and the config one wins. Keys and values are in Traditional Chinese (the OpenCC output):

```json
{
  "影片": "視訊",
  "圖紙": { "to": "設計圖", "note": "an optional comment, ignored by the mod" },
  "//": "keys starting with // are comments",
  "某詞": ""
}
```

A value is either a string or an object with `"to"` (and an optional `"note"`). An empty `"to"` deletes that built-in rule. The file is UTF-8 and is read at game start, so restart the game after editing it. The log shows the entry count and a fingerprint. An invalid file is skipped with an error in the log.

At startup the plugin runs self-checks, e.g. 视频设置→影像設定 and 图纸→藍圖, and logs `Self-check: N/N passed`. A user glossary can make them fail on purpose.

### Uninstall

Delete these from the game folder: `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` and the `BepInEx/` folder. Or use Steam's "Verify integrity of game files" after deleting `winhttp.dll`.

## Build (developers)

Requirements: .NET SDK 6+ (tested with 8), the game installed.

`GameDir` defaults to the standard Steam location, `C:\Program Files (x86)\Steam\steamapps\common\Luma Island`. If your game is in another Steam library, find its folder in Steam (right-click Luma Island → Manage → Browse local files) and pass it with `-p:GameDir=...`:

```sh
dotnet build src/LumaZhTw -c Release
# game in another Steam library
dotnet build src/LumaZhTw -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\Luma Island"
# also copy into <GameDir>/BepInEx/plugins/LumaZhTw/
dotnet build src/LumaZhTw -c Release -p:DeployToGame=true -p:GameDir="D:\SteamLibrary\steamapps\common\Luma Island"
```

`tools/audit_terms.py` uses the same default; override it with the `LUMA_GAME_DIR` environment variable.

- Game DLLs are referenced from `<GameDir>/Luma Island_Data/Managed`, or from a git-ignored `GameRefs/` folder at the repo root if it contains `Unity.Localization.dll`. They are never copied or committed.
- BepInEx/Harmony and Unity module references come from NuGet (`nuget.bepinex.dev`).
- OpenCC dictionaries (`src/LumaZhTw/Dictionaries/*.txt`) are embedded in the DLL.
- `STPhrases_GeneratedFromRegionalPhrases.txt` is regenerated with `python tools/gen_regional_st_phrases.py` (needs the `opencc` Python package), approximating OpenCC's build step.
- Self-check against Python OpenCC: `python tools/selfcheck.py`. With the glossary: `dotnet run --project tools/SelfCheck -- --glossary`.
- Glossary audit (dev only): `python tools/audit_terms.py 视频 图纸` reads the zh string tables from the local game install (needs UnityPy and opencc). It counts the strings that contain each term and lists them with their s2twp output. Output goes to the git-ignored `out/`. It contains game text, so never commit it.

### How the converter works

`OpenCcConverter` follows OpenCC's `s2twp.json`: maximum-forward-match segmentation over STPhrases (+ generated regional phrases), then per segment stage 1 (STPhrases → STCharacters, short-circuit, longest match) and stage 2 (TWPhrases → TWVariantsPhrases → TWVariants). CJK compatibility-ideograph normalization is skipped.

### Hook

Harmony postfixes on `StringTableEntry.GetLocalizedString(IFormatProvider, IList<object>, PseudoLocale)` (all other overloads, `LocalizedStringDatabase` and `LocalizedString` go through it) and `TableEntry.LocalizedValue` getter (read directly by the game's dialogue line provider). Only strings from `zh` string tables are converted; results are cached. Errors are logged once and the original string is returned.

## Licenses and credits

- Our code: [MIT](LICENSE).
- OpenCC dictionaries: Apache-2.0 ([license](third_party/OpenCC/LICENSE)). Noto Sans TC Regular (`fonts/`, unmodified, from [notofonts/noto-cjk](https://github.com/notofonts/noto-cjk) `Sans/SubsetOTF/TC`, © Google LLC / Adobe): SIL Open Font License 1.1 ([fonts/OFL.txt](fonts/OFL.txt)). See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
- This repo contains no game text, DLLs, decompiled code or assets.

## Disclaimer

Unofficial and not affiliated with or endorsed by Feel Free Games. Luma Island is their trademark and property. We will take this down on request.

Research and planning were done with help from Claude, Anthropic's AI 🤖; we (humans) build, test and maintain it.
