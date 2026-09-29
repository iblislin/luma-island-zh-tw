"""Dev-only: count Simplified terms in the game's zh string tables and show their s2twp context.

Usage: python tools/audit_terms.py [term ...]      (default terms: 视频 图纸)
Needs UnityPy and opencc. Reads the local game install; writes ONLY to out/ (git-ignored).
Never commit the output: it contains game text.
"""
import collections, os, sys
import UnityPy
from opencc import OpenCC

GAME = os.environ.get("LUMA_GAME_DIR", r"C:\Program Files (x86)\Steam\steamapps\common\Luma Island")
BUNDLE = os.path.join(GAME, r"Luma Island_Data\StreamingAssets\aa\StandaloneWindows64",
                      "localization-string-tables-simplifiedchinese_assets_all.bundle")
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "out")

def entries():
    for o in UnityPy.load(BUNDLE).objects:
        if o.type.name != "MonoBehaviour":
            continue
        t = o.read_typetree()
        for e in t.get("m_TableData") or []:
            s = e.get("m_Localized") or ""
            if s:
                yield t.get("m_Name", "?"), e.get("m_Id"), s

def main():
    terms = sys.argv[1:] or ["视频", "图纸"]
    cc = OpenCC("s2twp")
    os.makedirs(OUT, exist_ok=True)
    all_entries = list(entries())
    lines = [f"{len(all_entries)} zh entries scanned"]
    for term in terms:
        conv_term = cc.convert(term)
        hits = [(n, i, s) for n, i, s in all_entries if term in s]
        occ = sum(s.count(term) for _, _, s in hits)
        conv_hits = collections.Counter()   # converted-form occurrences whose source is NOT this term
        for n, i, s in all_entries:
            c = cc.convert(s)
            if conv_term in c and term not in s:
                conv_hits[(n, i)] += 1
        lines.append(f"\n== {term} -> s2twp {conv_term}: {len(hits)} strings, {occ} occurrences; "
                     f"{len(conv_hits)} other strings contain {conv_term} without {term} in source")
        for n, i, s in hits:
            lines.append(f"  [{n} #{i}] {s!r}\n      -> {cc.convert(s)!r}")
        for (n, i) in conv_hits:
            s = next(x for a, b, x in all_entries if a == n and b == i)
            lines.append(f"  (other) [{n} #{i}] {s!r} -> {cc.convert(s)!r}")
    path = os.path.join(OUT, "audit_terms.txt")
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print(lines[0])
    for l in lines:
        if l.startswith("\n=="):
            print(l.strip())
    print("details:", os.path.abspath(path))

if __name__ == "__main__":
    main()
