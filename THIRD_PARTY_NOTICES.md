# Third-party notices

This project bundles the following third-party components under their own licenses.

## OpenCC

- Project: https://github.com/BYVoid/OpenCC
- Copyright (c) BYVoid and contributors
- License: Apache License 2.0 — https://www.apache.org/licenses/LICENSE-2.0
- Used: dictionary files `STPhrases.txt`, `STCharacters.txt`, `TWPhrases.txt`, `TWVariantsPhrases.txt`, `TWVariants.txt` from `data/dictionary` (commit 2939943b, 2026-09), unmodified, in `src/LumaZhTw/Dictionaries/` and embedded in `LumaZhTw.dll`. `STPhrases_GeneratedFromRegionalPhrases.txt` is derived from `TWPhrases.txt` by our script. The conversion logic mirrors OpenCC's `s2twp.json` but is our own code.
- Full license text: [third_party/OpenCC/LICENSE](third_party/OpenCC/LICENSE). OpenCC ships no NOTICE file.

## Noto Sans TC (planned, Phase 2)

- Project: https://github.com/notofonts/noto-cjk / https://fonts.google.com/noto/specimen/Noto+Sans+TC
- Copyright (c) Google LLC and Adobe (Source Han Sans), with Reserved Font Name "Noto" as stated in the font's license
- License: SIL Open Font License 1.1 — https://openfontlicense.org
- Used unmodified. The OFL permits redistribution; the full OFL text ships with the font. Reserved Font Name rules only apply if the font is modified (a modified version must not use the reserved name).

## Not included

No Luma Island text, binaries, decompiled code or assets are included. Luma Island belongs to Feel Free Games.
