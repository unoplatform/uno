#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Uno.UI.Composition.WebGpu.SilkCompat.Generator;

/// <summary>A struct field or function parameter of a C header: its type name, pointer depth and name.</summary>
internal sealed record CField(string Type, int Ptr, string Name)
{
	public static CField Parse(string decl)
	{
		decl = Regex.Replace(decl, @"\bWGPU_NULLABLE\b|\bconst\b|\bstruct\b", " ");
		var ptr = decl.Count(c => c == '*');
		var toks = decl.Replace('*', ' ').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		return new CField(toks.Length >= 2 ? toks[^2] : "?", ptr, toks[^1]);
	}

	/// <summary>A return type, which has no name.</summary>
	public static CField Return(string ret) => new(ret.Replace("*", "").Trim(), ret.Count(c => c == '*'), "");

	public override string ToString() => $"{Type}{new string('*', Ptr)} {Name}";
}

/// <summary>The structs, enums, flags, callbacks and functions one webgpu.h ABI declares.</summary>
internal sealed class Header
{
	public readonly Dictionary<string, List<CField>> Structs = new();
	public readonly Dictionary<string, Dictionary<string, long>> Enums = new();
	public readonly Dictionary<string, Dictionary<string, long>> Flags = new();
	public readonly Dictionary<string, (string Ret, List<CField> Params)> Callbacks = new();
	public readonly Dictionary<string, (string Ret, List<CField> Params)> Funcs = new();

	public Header(params string[] paths)
	{
		foreach (var path in paths)
		{
			Parse(File.ReadAllText(path));
		}
	}

	private static long Number(string v)
		=> v.Contains("<<") ? 1L << int.Parse(v.Split("<<")[1].Trim())
			: v.StartsWith("0x", StringComparison.Ordinal) ? Convert.ToInt64(v[2..], 16)
			: long.Parse(v);

	private static List<CField> Params(string args)
		=> args.Split(',').Select(a => a.Trim()).Where(a => a.Length > 0 && a != "void").Select(CField.Parse).ToList();

	private void Parse(string h)
	{
		h = Regex.Replace(h, @"/\*.*?\*/", "", RegexOptions.Singleline);
		h = Regex.Replace(h, @"//[^\n]*", "");
		foreach (Match m in Regex.Matches(h, @"typedef struct WGPU(\w+)\s*\{(.*?)\}\s*WGPU\1\s*(?:WGPU_STRUCTURE_ATTRIBUTE)?\s*;", RegexOptions.Singleline))
		{
			Structs[m.Groups[1].Value] = m.Groups[2].Value.Split(';').Where(d => d.Trim().Length > 0).Select(CField.Parse).ToList();
		}
		foreach (Match m in Regex.Matches(h, @"typedef enum WGPU(\w+)\s*\{(.*?)\}\s*WGPU\1\s*(?:WGPU_ENUM_ATTRIBUTE)?\s*;", RegexOptions.Singleline))
		{
			var values = new Dictionary<string, long>();
			// wgpu.h writes its flag-like enums as shifts; members composed of other members are skipped.
			foreach (Match v in Regex.Matches(m.Groups[2].Value, @"WGPU[A-Za-z0-9]+?_(\w+)\s*=\s*(0x[0-9A-Fa-f]+|1\s*<<\s*\d+|\d+)\s*[,}\n]"))
			{
				values[v.Groups[1].Value] = Number(v.Groups[2].Value);
			}
			Enums[m.Groups[1].Value] = values;
		}
		// The stable header declares flag types as a WGPUFlags typedef plus static consts.
		foreach (Match m in Regex.Matches(h, @"static const WGPU(\w+) WGPU\1_(\w+) = (0x[0-9A-Fa-f]+|\d+|1\s*<<\s*\d+)\s*;"))
		{
			if (!Flags.TryGetValue(m.Groups[1].Value, out var values))
			{
				Flags[m.Groups[1].Value] = values = new();
			}
			values[m.Groups[2].Value] = Number(m.Groups[3].Value);
		}
		foreach (Match m in Regex.Matches(h, @"typedef\s+([\w\s\*]+?)\s*\(\s*\*\s*WGPU(\w+)\s*\)\s*\((.*?)\)\s*WGPU_FUNCTION_ATTRIBUTE\s*;", RegexOptions.Singleline))
		{
			Callbacks[m.Groups[2].Value] = (m.Groups[1].Value.Trim(), Params(m.Groups[3].Value));
		}
		var decls = Regex.Matches(h, @"WGPU_EXPORT\s+([\w\s\*]+?)\s*\b(wgpu\w+)\s*\((.*?)\)\s*WGPU_FUNCTION_ATTRIBUTE\s*;", RegexOptions.Singleline);
		if (decls.Count == 0)
		{
			// wgpu.h declares its functions without the export/attribute macros.
			decls = Regex.Matches(h, @"^\s*((?:WGPU_NULLABLE\s+)?[\w]+(?:\s*\*)?)\s+(wgpu\w+)\s*\(([^;{]*?)\)\s*;", RegexOptions.Singleline | RegexOptions.Multiline);
		}
		foreach (Match m in decls)
		{
			var ret = Regex.Replace(m.Groups[1].Value, @"\bWGPU_NULLABLE\b|\bconst\b", "").Trim();
			Funcs[m.Groups[2].Value] = (ret, Params(m.Groups[3].Value));
		}
	}

	/// <summary>What a WGPU type name is: struct, enum, flags, callback, bool, handle, or prim.</summary>
	public string Kind(string t)
	{
		if (!t.StartsWith("WGPU", StringComparison.Ordinal))
		{
			return "prim";
		}
		var n = t[4..];
		if (Structs.ContainsKey(n)) { return "struct"; }
		if (Enums.ContainsKey(n)) { return "enum"; }
		if (Flags.ContainsKey(n) || n.EndsWith("Flags", StringComparison.Ordinal)) { return "flags"; }
		if (Callbacks.ContainsKey(n)) { return "callback"; }
		if (n is "Bool" or "OptionalBool") { return "bool"; }
		return "handle";
	}
}

/// <summary>Where the 2024 header (Silk.NET 2.23's) and the stable header differ by name or meaning.</summary>
internal static class Renames
{
	/// <summary>Old struct -> its stable-header name, where the header renamed it.</summary>
	public static readonly Dictionary<string, string> Structs = new()
	{
		["ImageCopyTexture"] = "TexelCopyTextureInfo",
		["ImageCopyBuffer"] = "TexelCopyBufferInfo",
		["TextureDataLayout"] = "TexelCopyBufferLayout",
		["ShaderModuleWGSLDescriptor"] = "ShaderSourceWGSL",
		["ShaderModuleSPIRVDescriptor"] = "ShaderSourceSPIRV",
		["SurfaceDescriptorFromWindowsHWND"] = "SurfaceSourceWindowsHWND",
		["SurfaceDescriptorFromXlibWindow"] = "SurfaceSourceXlibWindow",
		["SurfaceDescriptorFromXcbWindow"] = "SurfaceSourceXCBWindow",
		["SurfaceDescriptorFromWaylandSurface"] = "SurfaceSourceWaylandSurface",
		["SurfaceDescriptorFromMetalLayer"] = "SurfaceSourceMetalLayer",
		["SurfaceDescriptorFromAndroidNativeWindow"] = "SurfaceSourceAndroidNativeWindow",
		["RenderPassDescriptorMaxDrawCount"] = "RenderPassMaxDrawCount",
		["ProgrammableStageDescriptor"] = "ComputeState",
		["ComputePassTimestampWrites"] = "PassTimestampWrites",
		["RenderPassTimestampWrites"] = "PassTimestampWrites",
		["AdapterProperties"] = "AdapterInfo",
	};

	/// <summary>
	/// Members whose meaning moved to a different name. In the 2024 header a binding layout's unused parts were zero and
	/// Undefined = 0 meant "not this kind of binding"; the stable header calls that BindingNotUsed = 0, and its Undefined
	/// (1) means "use the default" - so a name match would turn every unused part into a used one.
	/// </summary>
	public static readonly Dictionary<string, Dictionary<string, string>> EnumMembers = new()
	{
		["BufferBindingType"] = new() { ["Undefined"] = "BindingNotUsed" },
		["SamplerBindingType"] = new() { ["Undefined"] = "BindingNotUsed" },
		["TextureSampleType"] = new() { ["Undefined"] = "BindingNotUsed" },
		["StorageTextureAccess"] = new() { ["Undefined"] = "BindingNotUsed" },
		["VertexStepMode"] = new() { ["VertexBufferNotUsed"] = "Undefined" },
	};
}
