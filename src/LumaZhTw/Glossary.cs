using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LumaZhTw
{
    /// <summary>
    /// Traditional-to-Traditional phrase replacements applied after OpenCC.
    /// Scans left to right; at each position the longest matching key wins, its replacement is emitted
    /// and scanning resumes after the matched source text (no cascading, replacements are never re-scanned).
    /// JSON format: { "from": "to", "from2": { "to": "...", "note": "..." } }. Keys starting with "//" are ignored.
    /// </summary>
    public sealed class Glossary
    {
        private readonly Dictionary<string, string> _map = new Dictionary<string, string>(StringComparer.Ordinal);
        private int _maxLen;

        public int Count => _map.Count;

        /// <summary>Add or override entries from JSON text. An entry with an empty "to" removes the key.</summary>
        public void Merge(string json)
        {
            var root = new JsonReader(json).ReadValue() as Dictionary<string, object>
                       ?? throw new FormatException("glossary root must be a JSON object");
            foreach (var kv in root)
            {
                if (kv.Key.Length == 0 || kv.Key.StartsWith("//", StringComparison.Ordinal)) continue;
                string to = kv.Value as string;
                if (kv.Value is Dictionary<string, object> obj && obj.TryGetValue("to", out var t)) to = t as string;
                if (to == null) throw new FormatException($"glossary entry '{kv.Key}' needs a string or {{\"to\": ...}}");
                if (to.Length == 0) _map.Remove(kv.Key);
                else _map[kv.Key] = to;
            }
            _maxLen = 0;
            foreach (var k in _map.Keys) _maxLen = Math.Max(_maxLen, k.Length);
        }

        /// <summary>Stable fingerprint of the rules, e.g. for logging which glossary is active.</summary>
        public string Fingerprint()
        {
            var keys = new List<string>(_map.Keys);
            keys.Sort(StringComparer.Ordinal);
            uint h = 2166136261;
            foreach (var k in keys)
                foreach (char c in k + "\u0001" + _map[k] + "\u0002") { h ^= c; h *= 16777619; }
            return h.ToString("x8");
        }

        public string Apply(string text)
        {
            if (_map.Count == 0 || string.IsNullOrEmpty(text)) return text;
            StringBuilder sb = null;
            int i = 0, copied = 0;
            while (i < text.Length)
            {
                int max = Math.Min(_maxLen, text.Length - i), len = max;
                string to = null;
                for (; len > 0; len--)
                {
                    if (i + len < text.Length && char.IsLowSurrogate(text[i + len])) continue;
                    if (_map.TryGetValue(text.Substring(i, len), out to)) break;
                }
                if (len == 0) { i++; continue; }
                (sb ??= new StringBuilder(text.Length + 8)).Append(text, copied, i - copied).Append(to);
                i += len;
                copied = i;
            }
            return sb == null ? text : sb.Append(text, copied, text.Length - copied).ToString();
        }

        /// <summary>Minimal JSON reader (objects, arrays, strings, numbers, true/false/null).</summary>
        private sealed class JsonReader
        {
            private readonly string _s; private int _p;
            public JsonReader(string s) { _s = s; }

            private void Ws() { while (_p < _s.Length && char.IsWhiteSpace(_s[_p])) _p++; }
            private Exception Err(string m) => new FormatException($"JSON: {m} at offset {_p}");

            public object ReadValue()
            {
                Ws();
                if (_p < _s.Length && _s[_p] == '﻿') { _p++; Ws(); }
                if (_p >= _s.Length) throw Err("unexpected end");
                char c = _s[_p];
                if (c == '{')
                {
                    _p++; var d = new Dictionary<string, object>(StringComparer.Ordinal);
                    Ws(); if (_s[_p] == '}') { _p++; return d; }
                    while (true)
                    {
                        Ws(); string k = ReadString(); Ws();
                        if (_s[_p++] != ':') throw Err("expected ':'");
                        d[k] = ReadValue(); Ws();
                        char n = _s[_p++];
                        if (n == '}') return d;
                        if (n != ',') throw Err("expected ',' or '}'");
                    }
                }
                if (c == '[')
                {
                    _p++; var l = new List<object>();
                    Ws(); if (_s[_p] == ']') { _p++; return l; }
                    while (true)
                    {
                        l.Add(ReadValue()); Ws();
                        char n = _s[_p++];
                        if (n == ']') return l;
                        if (n != ',') throw Err("expected ',' or ']'");
                    }
                }
                if (c == '"') return ReadString();
                int st = _p;
                while (_p < _s.Length && ",}] \t\r\n".IndexOf(_s[_p]) < 0) _p++;
                string w = _s.Substring(st, _p - st);
                if (w == "true") return true;
                if (w == "false") return false;
                if (w == "null") return null;
                if (double.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out var num)) return num;
                throw Err("bad token '" + w + "'");
            }

            private string ReadString()
            {
                if (_p >= _s.Length || _s[_p] != '"') throw Err("expected string");
                _p++; var sb = new StringBuilder();
                while (true)
                {
                    if (_p >= _s.Length) throw Err("unterminated string");
                    char c = _s[_p++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    char e = _s[_p++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u': sb.Append((char)Convert.ToInt32(_s.Substring(_p, 4), 16)); _p += 4; break;
                        default: sb.Append(e); break;
                    }
                }
            }
        }
    }
}
