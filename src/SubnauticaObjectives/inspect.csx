using System;
using System.Reflection;
using System.Linq;

var asm = Assembly.LoadFrom(@"D:\Games\Steam\steamapps\common\Subnautica\Subnautica_Data\Managed\Assembly-CSharp.dll");
var types = asm.GetTypes();

Console.WriteLine("=== Subtitle types ===");
foreach (var t in types.Where(t => t.Name.Contains("Subtitle", StringComparison.OrdinalIgnoreCase)))
    Console.WriteLine(t.FullName);

Console.WriteLine("\n=== PDAEncyclopedia members ===");
var pda = types.FirstOrDefault(t => t.Name == "PDAEncyclopedia");
if (pda != null) {
    foreach (var m in pda.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
        Console.WriteLine($"  {m.MemberType}: {m.Name}");
}

Console.WriteLine("\n=== Language members ===");
var lang = types.FirstOrDefault(t => t.Name == "Language");
if (lang != null) {
    foreach (var m in lang.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
        Console.WriteLine($"  {m.MemberType}: {m.Name}");
}
