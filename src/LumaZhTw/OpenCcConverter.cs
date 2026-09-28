using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace LumaZhTw
{
    /// <summary>
    /// Small re-implementation of OpenCC's s2twp pipeline on top of OpenCC's text dictionaries:
    ///   segmentation: maximum forward match over STPhrases (+ generated regional phrases)
    ///   stage 1 (short-circuit group): STPhrases, then STCharacters
    ///   stage 2 (short-circuit group): TWPhrases, TWVariantsPhrases, then TWVariants
    /// Each stage converts each segment by longest-prefix matching; the first listed value wins.
    /// STPhrases_GeneratedFromRegionalPhrases is approximated by tools/gen_regional_st_phrases.py.
    /// Not implemented: CJK compatibility ideograph normalization.
    /// </summary>
    public sealed class OpenCcConverter
    {
        private sealed class Dict
        {
            public readonly Dictionary<string, string> Map = new Dictionary<string, string>(StringComparer.Ordinal);
            public int MaxLen;

            public void Add(string key, string value)
            {
                if (!Map.ContainsKey(key)) Map[key] = value;
                if (key.Length > MaxLen) MaxLen = key.Length;
            }

            /// <summary>Length of the longest key that prefixes text[pos..], or 0.</summary>
            public int MatchPrefix(string text, int pos, out string value)
            {
                int max = Math.Min(MaxLen, text.Length - pos);
                for (int len = max; len > 0; len--)
                {
                    if (pos + len < text.Length && char.IsLowSurrogate(text[pos + len])) continue;
                    if (Map.TryGetValue(text.Substring(pos, len), out value)) return len;
                }
                value = null;
                return 0;
            }
        }

        private readonly Dict _stPhrases;
        private readonly Dict[] _stage1, _stage2;

        public OpenCcConverter(Func<string, TextReader> open)
        {
            // union group: STPhrases plus the generated regional phrases (STPhrases wins on duplicates)
            _stPhrases = Load(open("STPhrases"));
            Load(open("STPhrases_GeneratedFromRegionalPhrases"), _stPhrases);
            _stage1 = new[] { _stPhrases, Load(open("STCharacters")) };
            _stage2 = new[] { Load(open("TWPhrases")), Load(open("TWVariantsPhrases")), Load(open("TWVariants")) };
        }

        public static OpenCcConverter FromEmbeddedResources()
        {
            var asm = Assembly.GetExecutingAssembly();
            return new OpenCcConverter(name =>
            {
                var s = asm.GetManifestResourceStream("LumaZhTw.Dictionaries." + name + ".txt")
                        ?? throw new FileNotFoundException("Missing embedded dictionary " + name);
                return new StreamReader(s, Encoding.UTF8);
            });
        }

        private static Dict Load(TextReader reader, Dict d = null)
        {
            d = d ?? new Dict();
            using (reader)
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == '#') continue;
                    int tab = line.IndexOf('\t');
                    if (tab <= 0) continue;
                    string vals = line.Substring(tab + 1).Trim();
                    int sp = vals.IndexOf(' ');
                    d.Add(line.Substring(0, tab), sp < 0 ? vals : vals.Substring(0, sp));
                }
            }
            return d;
        }

        private static int CharLen(string s, int pos) =>
            char.IsHighSurrogate(s[pos]) && pos + 1 < s.Length && char.IsLowSurrogate(s[pos + 1]) ? 2 : 1;

        private static string ConvertSegment(string seg, Dict[] group)
        {
            var sb = new StringBuilder(seg.Length);
            int i = 0;
            while (i < seg.Length)
            {
                int len = 0;
                string value = null;
                foreach (var d in group)
                {
                    len = d.MatchPrefix(seg, i, out value);
                    if (len > 0) break; // short_circuit: the first dictionary with a match wins
                }
                if (len > 0) { sb.Append(value); i += len; }
                else { int c = CharLen(seg, i); sb.Append(seg, i, c); i += c; }
            }
            return sb.ToString();
        }

        public string Convert(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length);
            // Like OpenCC's MaxMatchSegmentation: a dictionary match is its own segment, and runs of
            // unmatched characters are buffered into one segment between matches.
            int i = 0, bufStart = 0;
            while (i < text.Length)
            {
                int len = _stPhrases.MatchPrefix(text, i, out _);
                if (len == 0) { i += CharLen(text, i); continue; }
                if (i > bufStart) AppendSegment(sb, text.Substring(bufStart, i - bufStart));
                AppendSegment(sb, text.Substring(i, len));
                i += len;
                bufStart = i;
            }
            if (i > bufStart) AppendSegment(sb, text.Substring(bufStart, i - bufStart));
            return sb.ToString();
        }

        private void AppendSegment(StringBuilder sb, string seg)
        {
            sb.Append(ConvertSegment(ConvertSegment(seg, _stage1), _stage2));
        }
    }
}
