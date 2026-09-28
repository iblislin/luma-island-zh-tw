// Usage: dotnet run --project tools/SelfCheck -- <dictDir> < input.txt  (one line in, one converted line out)
using System;
using System.IO;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = new UTF8Encoding(false);
string dir = args.Length > 0 ? args[0] : Path.Combine("src", "LumaZhTw", "Dictionaries");
var conv = new LumaZhTw.OpenCcConverter(n => new StreamReader(Path.Combine(dir, n + ".txt"), Encoding.UTF8));
string line;
while ((line = Console.ReadLine()) != null) Console.WriteLine(conv.Convert(line));
