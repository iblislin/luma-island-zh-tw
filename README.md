# Luma Island – Traditional Chinese (Taiwan) mod

An **unofficial** fan mod that adds Traditional Chinese (Taiwan, zh-TW) to [Luma Island](https://store.steampowered.com/app/2408820/) by Feel Free Games.

> Status: planning. No mod code yet. See [PLAN.md](PLAN.md).

## How it works

Luma Island already ships Simplified Chinese. This mod reuses it:

1. **Runtime conversion with OpenCC.** A BepInEx 5 + Harmony plugin hooks the Unity Localization string lookup. When the game locale is Simplified Chinese and the mod's zh-TW option is on, each string is converted with [OpenCC](https://github.com/BYVoid/OpenCC) using the `s2twp` config (Simplified to Taiwan standard, including Taiwan phrases). Results are cached.
2. **Taiwan glyphs with Noto Sans TC.** The game's Noto Sans SC font already covers nearly all CJK ideographs, but with PRC glyph shapes. The mod creates a dynamic TMP font asset from [Noto Sans TC](https://fonts.google.com/noto/specimen/Noto+Sans+TC) at runtime and makes it primary while zh-TW is active, keeping Noto Sans SC as a fallback.
3. **Glossary (planned).** An optional JSON/CSV file overrides game-specific terms that automatic conversion gets wrong.

**For the developers:** this is a cheap path to official Traditional Chinese. Run your Simplified Chinese tables through OpenCC `s2twp` (Apache-2.0), add Noto Sans TC (OFL-1.1, free to ship), and have a native speaker review key terms. A plain conversion alone was apparently tried before, so this mod shows the fuller variant: Taiwan phrasing, TC glyphs and a glossary. We'd be happy to share anything we learn.

## Install (planned)

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx) (x64, Mono) into the game folder.
2. Copy the plugin folder into `BepInEx/plugins/`.
3. Set the game language to Simplified Chinese; enable zh-TW in the mod config.

## Licenses and credits

- Our code: [MIT](LICENSE).
- OpenCC: Apache-2.0. Noto Sans TC: SIL Open Font License 1.1. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
- This repo contains no game text, DLLs, decompiled code or assets.

## Disclaimer

Unofficial and not affiliated with or endorsed by Feel Free Games. Luma Island is their trademark and property. We will take this down on request.

Research and planning were done with help from Claude, Anthropic's AI 🤖; we (humans) build, test and maintain it.
