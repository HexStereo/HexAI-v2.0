using System;
using System.IO;
using System.Text.RegularExpressions;

string dir = @"c:\Users\ardab\.gemini\antigravity-ide\scratch\HexAI\src\HexAI.NeuralNetwork\Layers";
var files = Directory.GetFiles(dir, "*.cs");

string pattern = @"_(\w+)\s*=\s*_\1\s*-\s*optimizer\.ComputeUpdate\(([^,]+),\s*_\1\);";
string replacement = "_$1.SubtractInPlace(optimizer.ComputeUpdate($2, _$1));";

foreach (var file in files)
{
    string content = File.ReadAllText(file);
    if (Regex.IsMatch(content, pattern))
    {
        string newContent = Regex.Replace(content, pattern, replacement);
        File.WriteAllText(file, newContent);
        Console.WriteLine("Fixed " + Path.GetFileName(file));
    }
}
