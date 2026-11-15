#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Uno.UI.Composition.WebGpu.SilkCompat.Generator;

/// <summary>
/// The stable webgpu.h (+ wgpu.h) as C#: enums, flag enums, structs and the functions SilkCompat's translators and
/// browser forwarders call. Opaque handles, callbacks and void*/char* map to IntPtr; struct and primitive pointers stay
/// typed. A struct holding an anonymous union, or a by-value field of a type the headers never define, is skipped.
/// webgpu.h's functions are [DllImport]s. wgpu.h's are function pointers bound from the loaded library: the browser
/// links every [DllImport] into the module, and Dawn implements none of them.
/// </summary>
internal static class NativeBindings
{
	private static readonly Dictionary<string, string> Primitives = new()
	{
		["uint64_t"] = "ulong", ["int64_t"] = "long", ["uint32_t"] = "uint", ["int32_t"] = "int",
		["uint16_t"] = "ushort", ["int16_t"] = "short", ["uint8_t"] = "byte", ["int8_t"] = "sbyte",
		["size_t"] = "nuint", ["float"] = "float", ["double"] = "double", ["void"] = "void",
		["WGPUBool"] = "uint", ["WGPUFlags"] = "ulong", ["char"] = "byte", ["int"] = "int",
	};

	/// <param name="functions">Every function's C# return and parameters, by name.</param>
	/// <param name="extras">The functions only wgpu-native has (wgpu.h's), bound at run time.</param>
	public static string Emit(string ns, string webgpuH, string wgpuH, out Dictionary<string, (string Ret, List<(string Type, string Name)> Params)> functions, out HashSet<string> extras)
	{
		var text = string.Concat(new[] { webgpuH, wgpuH }.Select(h => StripComments(h.Replace("\r\n", "\n")) + "\n"));
		var prim = new Dictionary<string, string>(Primitives);
		var enums = new Dictionary<string, List<(string Member, string Value)>>();
		var flags = new Dictionary<string, List<(string Member, string Value)>>();
		var handles = new HashSet<string>();
		var structs = new Dictionary<string, List<(string CType, string Name)>>();
		var funcs = new List<(string Ret, string Name, List<(string CType, string Name)> Args)>();

		foreach (Match m in Regex.Matches(text, @"typedef (uint64_t|int64_t|uint32_t|int32_t|uint16_t|int16_t|uint8_t|int8_t|float|double)\s+(WGPU\w+)\s*;"))
		{
			prim.TryAdd(m.Groups[2].Value, prim[m.Groups[1].Value]);
		}
		foreach (Match m in Regex.Matches(text, @"typedef struct (\w+Impl)\s*\*\s*(\w+)(?:\s+WGPU_\w+)?\s*;"))
		{
			handles.Add(m.Groups[2].Value);
		}
		foreach (Match m in Regex.Matches(text, @"typedef\s+[\w\s\*]+\(\s*\*\s*(\w+)\s*\)\s*\("))
		{
			handles.Add(m.Groups[1].Value);
		}
		foreach (Match m in Regex.Matches(text, @"typedef enum (\w+)\s*\{(.*?)\}\s*\1", RegexOptions.Singleline))
		{
			var members = new List<(string, string)>();
			foreach (Match e in Regex.Matches(m.Groups[2].Value, @"(\w+)\s*=\s*([^,}]+)"))
			{
				if (Constant(e.Groups[2].Value) is { } v) { members.Add((e.Groups[1].Value, v)); }
			}
			enums[m.Groups[1].Value] = members;
		}
		foreach (Match m in Regex.Matches(text, @"typedef WGPUFlags (\w+)\s*;"))
		{
			flags[m.Groups[1].Value] = new();
		}
		foreach (var (name, members) in flags)
		{
			foreach (Match e in Regex.Matches(text, @"static const " + name + @"\s+(\w+)\s*=\s*([^;]+);"))
			{
				if (Constant(e.Groups[2].Value) is { } v) { members.Add((e.Groups[1].Value, v)); }
			}
		}
		foreach (Match m in Regex.Matches(text, @"typedef struct (\w+)\s*\{(.*?)\}\s*\1", RegexOptions.Singleline))
		{
			var (name, body) = (m.Groups[1].Value, m.Groups[2].Value);
			if (name.EndsWith("Impl", StringComparison.Ordinal) || body.Contains("union", StringComparison.Ordinal)) { continue; }
			var fields = new List<(string, string)>();
			foreach (var raw in body.Split(';'))
			{
				var line = raw.Trim();
				if (line.Length == 0) { continue; }
				var f = Regex.Match(line.Replace('\n', ' ').Trim(), @"^(.*?)(\**)\s*(\w+)$");
				if (f.Success) { fields.Add(((f.Groups[1].Value + f.Groups[2].Value).Trim(), f.Groups[3].Value)); }
			}
			structs[name] = fields;
		}

		// A struct with a by-value field of an unknown type is a union-bearing or platform-only chain: drop it, and
		// whatever embeds it.
		for (var changed = true; changed;)
		{
			changed = false;
			var known = new HashSet<string>(prim.Keys.Concat(enums.Keys).Concat(flags.Keys).Concat(handles).Concat(structs.Keys));
			foreach (var name in structs.Keys.ToList())
			{
				if (structs[name].Any(f => Base(f.CType) is (0, var b) && !known.Contains(b)))
				{
					structs.Remove(name);
					changed = true;
				}
			}
		}

		foreach (Match m in Regex.Matches(text, @"WGPU_EXPORT\s+(.+?)\s+(wgpu\w+)\s*\((.*?)\)\s*WGPU_FUNCTION_ATTRIBUTE", RegexOptions.Singleline))
		{
			funcs.Add((m.Groups[1].Value.Trim(), m.Groups[2].Value, Args(m.Groups[3].Value.Trim())));
		}
		// wgpu.h declares its extras without the export and attribute macros.
		var seen = funcs.Select(f => f.Name).ToHashSet();
		foreach (Match m in Regex.Matches(text, @"(?<=[;{}])\s*([A-Za-z_]\w*\s*\**)\s+(wgpu\w+)\s*\(([^;{}]*?)\)\s*;", RegexOptions.Singleline))
		{
			if (seen.Add(m.Groups[2].Value)) { funcs.Add((m.Groups[1].Value.Trim(), m.Groups[2].Value, Args(m.Groups[3].Value.Trim()))); }
		}

		string Map(string ctype)
		{
			var (ptr, b) = Base(ctype);
			if (ptr > 0)
			{
				if (structs.ContainsKey(b) || enums.ContainsKey(b) || flags.ContainsKey(b)) { return b + new string('*', ptr); }
				if (prim.TryGetValue(b, out var p) && b is not ("void" or "char")) { return p + new string('*', ptr); }
				return "IntPtr";
			}
			if (prim.TryGetValue(b, out var pv)) { return pv; }
			return handles.Contains(b) && !enums.ContainsKey(b) && !flags.ContainsKey(b) ? "IntPtr" : b;
		}

		var o = new List<string>
		{
			"// <auto-generated/> Generated by Generator/Program.cs from the stable webgpu.h and wgpu.h. Do not edit.",
			"#nullable disable",
			"using System;",
			"using System.Runtime.InteropServices;",
			$"namespace {ns};",
			"",
		};
		foreach (var (name, members) in enums)
		{
			o.Add($"internal enum {name} : uint {{");
			o.AddRange(members.Select(e => $"    {Short(name, e.Member)} = {e.Value},"));
			o.Add("}");
		}
		foreach (var (name, members) in flags)
		{
			o.Add("[Flags]");
			o.Add($"internal enum {name} : ulong {{");
			o.AddRange(members.Select(e => $"    {Short(name, e.Member)} = {e.Value},"));
			o.Add("}");
		}
		foreach (var (name, fields) in structs)
		{
			o.Add("[StructLayout(LayoutKind.Sequential)]");
			o.Add($"internal unsafe struct {name} {{");
			// PascalCase the camelCase C names, which gives Silk.NET's field names.
			o.AddRange(fields.Select(f => $"    public {Map(f.CType)} {char.ToUpperInvariant(f.Name[0])}{f.Name[1..]};"));
			o.Add("}");
		}
		var exported = Regex.Matches(StripComments(webgpuH.Replace("\r\n", "\n")), @"WGPU_EXPORT\s+.+?\s+(wgpu\w+)\s*\(", RegexOptions.Singleline)
			.Select(m => m.Groups[1].Value).ToHashSet();
		functions = funcs.ToDictionary(f => f.Name, f => (Map(f.Ret), f.Args.Select(a => (Map(a.CType), a.Name)).ToList()));
		extras = funcs.Select(f => f.Name).Where(n => !exported.Contains(n)).ToHashSet();

		o.Add("internal static unsafe partial class WGPU {");
		o.Add("    const string L = \"webgpu\";");
		foreach (var (ret, name, args) in funcs.Where(f => exported.Contains(f.Name)))
		{
			var ps = string.Join(", ", args.Select(a => $"{Map(a.CType)} {a.Name}"));
			o.Add($"    [DllImport(L, EntryPoint=\"{name}\")] public static extern {Map(ret)} {name}({ps});");
		}
		var bound = funcs.Where(f => !exported.Contains(f.Name)).Select(f => (f.Name, Type: Pointer(Map(f.Ret), f.Args.Select(a => Map(a.CType))))).ToList();
		o.AddRange(bound.Select(f => $"    public static {f.Type} {f.Name};"));
		o.Add("    /// <summary>Binds wgpu-native's own functions from the loaded library. Not on the browser, where none exist.</summary>");
		o.Add("    public static void BindExtras(Func<string, nint> export) {");
		o.AddRange(bound.Select(f => $"        {f.Name} = ({f.Type})export(\"{f.Name}\");"));
		o.Add("    }");
		o.Add("}");
		return string.Join("\n", o);
	}

	private static string Pointer(string ret, IEnumerable<string> args)
		=> $"delegate* unmanaged[Cdecl]<{string.Join(", ", args.Append(ret))}>";

	private static string StripComments(string s)
		=> Regex.Replace(Regex.Replace(s, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");

	private static (int Ptr, string Base) Base(string ctype)
	{
		var t = ctype.Replace("WGPU_NULLABLE", "").Replace("const", "").Trim();
		return (t.Count(c => c == '*'), t.Replace("*", "").Trim());
	}

	private static List<(string, string)> Args(string args)
	{
		var list = new List<(string, string)>();
		if (args.Length == 0 || args == "void") { return list; }
		foreach (var a in args.Split(','))
		{
			var m = Regex.Match(a.Trim(), @"^(.*?)(\**)\s*(\w+)$");
			if (m.Success) { list.Add(((m.Groups[1].Value + m.Groups[2].Value).Trim(), m.Groups[3].Value)); }
		}
		return list;
	}

	private static string Short(string type, string member)
	{
		var s = member.StartsWith(type + "_", StringComparison.Ordinal) ? member[(type.Length + 1)..] : member;
		return s.Length > 0 && char.IsDigit(s[0]) ? "_" + s : s;
	}

	/// <summary>A C integer constant: a literal as written, or an expression of literals, shifts and ORs as hex.</summary>
	private static string? Constant(string expr)
	{
		expr = expr.Trim();
		if (Regex.IsMatch(expr, @"^(0[xX][0-9a-fA-F]+|\d+)$")) { return expr; }
		if (!Regex.IsMatch(expr.Replace("<<", "<"), @"^[0-9\s()<|]+$")) { return null; }
		var pos = 0;
		var value = Or(expr, ref pos);
		return "0x" + value.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
	}

	private static long Or(string s, ref int i)
	{
		var v = Shift(s, ref i);
		while (Skip(s, ref i) < s.Length && s[i] == '|') { i++; v |= Shift(s, ref i); }
		return v;
	}

	private static long Shift(string s, ref int i)
	{
		var v = Atom(s, ref i);
		while (Skip(s, ref i) + 1 < s.Length && s[i] == '<' && s[i + 1] == '<') { i += 2; v <<= (int)Atom(s, ref i); }
		return v;
	}

	private static long Atom(string s, ref int i)
	{
		Skip(s, ref i);
		if (s[i] == '(') { i++; var v = Or(s, ref i); Skip(s, ref i); i++; return v; }
		var start = i;
		while (i < s.Length && char.IsDigit(s[i])) { i++; }
		return long.Parse(s[start..i], System.Globalization.CultureInfo.InvariantCulture);
	}

	private static int Skip(string s, ref int i)
	{
		while (i < s.Length && char.IsWhiteSpace(s[i])) { i++; }
		return i;
	}
}
