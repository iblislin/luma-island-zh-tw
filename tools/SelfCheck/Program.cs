// Usage: dotnet run --project tools/SelfCheck -- [--glossary] [<dictDir>] < input.txt  (one line in, one converted line out)
// --glossary also applies src/LumaZhTw/Glossary.default.json after OpenCC.
using System;
using System.IO;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = new UTF8Encoding(false);
bool useGlossary = Array.IndexOf(args, "--glossary") >= 0;
string dir = Array.Find(args, a => a != "--glossary") ?? Path.Combine("src", "LumaZhTw", "Dictionaries");
var glossary = new LumaZhTw.Glossary();
if (useGlossary) glossary.Merge(File.ReadAllText(Path.Combine("src", "LumaZhTw", "Glossary.default.json"), Encoding.UTF8));
var conv = new LumaZhTw.OpenCcConverter(n => new StreamReader(Path.Combine(dir, n + ".txt"), Encoding.UTF8));
string line;
while ((line = Console.ReadLine()) != null) Console.WriteLine(glossary.Apply(conv.Convert(line)));
