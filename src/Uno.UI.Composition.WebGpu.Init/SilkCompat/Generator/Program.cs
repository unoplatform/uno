#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Uno.UI.Composition.WebGpu.SilkCompat.Generator;

/// <summary>
/// Generates SilkCompat.Generated.cs: Silk.NET.WebGPU 2.23 (bound to the 2024 webgpu.h) translated onto the stable
/// webgpu.h our natives implement. Each old function becomes a passthrough (same ABI: the native export itself), a
/// rename (the old name of an unchanged function), or a generated [UnmanagedCallersOnly] translator that converts the
/// old structs, enums and strings and calls the stable raw binding. Functions involving callbacks or output structs are
/// listed in <see cref="Hand"/> and written in SilkCompat.Hand.cs.
/// </summary>
/// <remarks>Run: <c>dotnet run</c> from this folder.</remarks>
internal static class Program
{
	// Functions whose translation needs judgement: callbacks, output structs, reshaped descriptors.
	private static readonly HashSet<string> Hand = new()
	{
		"wgpuInstanceRequestAdapter", "wgpuAdapterRequestDevice", "wgpuBufferMapAsync", "wgpuQueueOnSubmittedWorkDone",
		"wgpuDevicePopErrorScope", "wgpuShaderModuleGetCompilationInfo", "wgpuDeviceCreateComputePipelineAsync",
		"wgpuDeviceCreateRenderPipelineAsync", "wgpuDeviceSetUncapturedErrorCallback", "wgpuAdapterRequestAdapterInfo",
		"wgpuAdapterGetLimits", "wgpuDeviceGetLimits", "wgpuAdapterGetProperties", "wgpuAdapterEnumerateFeatures",
		"wgpuDeviceEnumerateFeatures", "wgpuSurfaceGetCapabilities", "wgpuSurfaceCapabilitiesFreeMembers",
		"wgpuSurfaceGetCurrentTexture", "wgpuSurfaceGetPreferredFormat", "wgpuGetProcAddress",
		"wgpuInstanceHasWGSLLanguageFeature",
		// wgpu-native extras whose shapes changed
		"wgpuDevicePoll", "wgpuSetLogCallback", "wgpuGenerateReport", "wgpuRenderPassEncoderSetPushConstants",
		// wgpu-native doesn't implement wgpuBufferGetMapState, so SilkCompat tracks map states through these.
		"wgpuBufferGetMapState", "wgpuDeviceCreateBuffer", "wgpuBufferUnmap", "wgpuBufferDestroy", "wgpuBufferRelease",
	};

	// Old structs filled back from their stable counterpart, for functions that hand one out.
	private static readonly HashSet<string> Unfilled = new() { "Limits" };

	// Old fields with no stable counterpart, computed from the stable struct `s`.
	private static readonly Dictionary<(string Struct, string Field), string> OldFieldFromNew = new()
	{
		// WebGPU defines the component limit as four per inter-stage variable.
		[("Limits", "maxInterStageShaderComponents")] = "s->MaxInterStageShaderVariables * 4",
	};

	// Structs converted by hand-written code.
	private static readonly HashSet<string> HandStructs = new()
	{
		"DeviceDescriptor", "RequiredLimits", "SupportedLimits", "AdapterProperties", "AdapterInfo", "SurfaceCapabilities",
		"SurfaceTexture", "CompilationInfo", "CompilationMessage", "ChainedStructOut", "PrimitiveDepthClipControl",
		"SurfaceDescriptorFromCanvasHTMLSelector", "ShaderModuleCompilationHint", "PrimitiveState", "InstanceExtras", "ChainedStruct",
	};

	// New fields whose zero is not their default.
	private static readonly Dictionary<(string Struct, string Field), string> NewFieldDefaults = new()
	{
		[("Limits", "maxImmediateSize")] = "uint.MaxValue",
	};

	private static readonly Dictionary<string, string> CsPrim = new()
	{
		["uint32_t"] = "uint", ["int32_t"] = "int", ["uint64_t"] = "ulong", ["int64_t"] = "long", ["uint16_t"] = "ushort",
		["size_t"] = "nuint", ["float"] = "float", ["double"] = "double", ["void"] = "void", ["char"] = "byte", ["uint8_t"] = "byte",
	};

	private static Header Old = null!, New = null!;
	private static Dictionary<string, List<(string Type, string Name)>> RawStructs = null!;
	private static Dictionary<string, (string Ret, List<(string Type, string Name)> Params)> RawFuncs = null!;
	// wgpu-native's own functions: bound at run time on native targets, absent from the browser.
	private static HashSet<string> Extras = null!;
	private static readonly Dictionary<string, (SortedDictionary<long, long> O2N, SortedDictionary<long, long> N2O)> EnumMaps = new(StringComparer.Ordinal);
	private static readonly HashSet<string> Renum = new(), FlagEnums = new();
	private static readonly Dictionary<string, bool> Needs = new();
	private static readonly List<string> Problems = new(), Unsupported = new();

	private static string Here([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

	private static int Main(string[] args)
	{
		var generator = Here();
		var project = Path.GetDirectoryName(generator)!;
		var src = Path.GetDirectoryName(Path.GetDirectoryName(project))!;
		var output = args.Length > 0 ? args[0] : Path.Combine(project, "SilkCompat.Generated.cs");

		var headers = Path.Combine(src, "Uno.UI.Composition.WebGpu", "Native", "webgpu-headers");
		Old = new Header(Path.Combine(generator, "headers", "silk-webgpu.h"), Path.Combine(generator, "headers", "silk-wgpu.h"));
		New = new Header(Path.Combine(headers, "webgpu.h"), Path.Combine(headers, "wgpu.h"));

		// The stable-header binding the translators call, then the translators themselves.
		var native = NativeBindings.Emit("Uno.UI.Composition.WebGpu.SilkCompat", File.ReadAllText(Path.Combine(headers, "webgpu.h")), File.ReadAllText(Path.Combine(headers, "wgpu.h")), out RawFuncs, out Extras);
		var nativeOutput = Path.Combine(project, "SilkCompat.Native.Generated.cs");
		File.WriteAllText(nativeOutput, native.Replace("\n", "\r\n") + "\r\n");
		Console.WriteLine($"wrote {nativeOutput}");

		var raw = native + File.ReadAllText(Path.Combine(project, "SilkCompat.Native.cs")).Replace("\r\n", "\n");
		RawStructs = new();
		foreach (Match m in Regex.Matches(raw, @"(?:public|internal) unsafe struct (WGPU\w+)\s*\{(.*?)\n\}", RegexOptions.Singleline))
		{
			RawStructs[m.Groups[1].Value] = Regex.Matches(m.Groups[2].Value, @"public\s+(?:fixed\s+)?([\w\*\.]+)\s+(\w+)(?:\[\d+\])?\s*;")
				.Select(f => (f.Groups[1].Value, f.Groups[2].Value)).ToList();
		}

		foreach (var e in Old.Enums.Keys)
		{
			if (EnumNeedsMap(e)) { Renum.Add(e); }
			if (New.Flags.ContainsKey(e)) { FlagEnums.Add(e); }
		}
		foreach (var n in Old.Structs.Keys)
		{
			StructNeeds(n, new());
		}

		var structLines = new List<string>();
		var converted = 0;
		foreach (var n in Old.Structs.Keys)
		{
			if (!Needs.GetValueOrDefault(n) || HandStructs.Contains(n) || NewStruct(n) is not { } nn)
			{
				continue;
			}
			converted++;
			if (!RawStructs.ContainsKey("WGPU" + nn) || SilkType(n) is null)
			{
				Unsupported.Add(n);
				continue;
			}
			EmitStruct(n, structLines);
		}

		var (passthrough, renamed, translated, hand, unmapped, fnLines) = EmitFunctions();

		var o = new List<string>
		{
			"// <auto-generated/> Generated by Generator/Program.cs: Silk.NET.WebGPU 2.23 (2024 header) onto the stable webgpu.h.",
			"#nullable disable", "#pragma warning disable CS0169, IDE0051", "using System;", "using System.Collections.Generic;",
			"using System.Runtime.CompilerServices;", "using System.Runtime.InteropServices;", "",
			"namespace Uno.UI.Composition.WebGpu.SilkCompat;", "", "internal static unsafe partial class Xlate", "{",
		};
		foreach (var (e, (o2n, n2o)) in EnumMaps.OrderBy(p => p.Key, StringComparer.Ordinal))
		{
			o.Add($"\tinternal static int O2N_{e}(int v) => v switch {{ " + string.Join(", ", o2n.Select(p => $"{p.Key} => {p.Value}")) + ", _ => v };");
			o.Add($"\tinternal static int N2O_{e}(int v) => v switch {{ " + string.Join(", ", n2o.Select(p => $"{p.Key} => {p.Value}")) + ", _ => v };");
			// Whether a stable value has an old counterpart: N2O passes others through, which can alias another member.
			o.Add($"\tinternal static bool N2OKnown_{e}(int v) => v is {string.Join(" or ", n2o.Keys)};");
		}
		o.Add("");
		o.AddRange(structLines);
		foreach (var n in Unfilled)
		{
			EmitUnfill(n, o);
		}
		o.Add("\tinternal static WGPUChainedStruct* Chain(Silk.NET.WebGPU.ChainedStruct* s, ref Arena a)");
		o.Add("\t{");
		o.Add("\t\tif (s == null) { return null; }");
		o.Add("\t\tswitch ((int)s->SType)");
		o.Add("\t\t{");
		foreach (var n in Old.Structs.Keys)
		{
			if (!IsChained(Old.Structs[n]) || HandStructs.Contains(n) || Unsupported.Contains(n) || NewStruct(n) is not { } nn || OldSType(n) is not { } ov)
			{
				continue;
			}
			var nt = "WGPU" + nn;
			o.Add($"\t\t\tcase {ov}: {{ var d = ({nt}*)a.Alloc(sizeof({nt})); Fill(({SilkType(n)}*)s, d, ref a); return (WGPUChainedStruct*)d; }}");
		}
		o.Add("\t\t\tdefault: return ChainHand(s, ref a);");
		o.Add("\t\t}");
		o.Add("\t}");
		o.Add("");
		o.AddRange(fnLines);
		o.AddRange(BrowserForwarders(passthrough.Select(n => (n, n)).Concat(renamed).Where(f => !Extras.Contains(f.Item2))));
		o.Add("\tprivate static void AddGenerated(Dictionary<string, nint> t, Func<string, nint> native)");
		o.Add("\t{");
		o.AddRange(passthrough.Select(n => $"\t\tt[\"{n}\"] = OperatingSystem.IsBrowser() ? {BrowserEntry(n)} : native(\"{n}\");"));
		o.AddRange(renamed.Select(p => $"\t\tt[\"{p.Old}\"] = OperatingSystem.IsBrowser() ? {BrowserEntry(p.New)} : native(\"{p.New}\");"));
		foreach (var n in translated)
		{
			var (ret, ps) = Old.Funcs[n];
			var oret = ret == "void" ? "void" : UcoType(CField.Return(ret));
			var entry = $"(nint)(delegate* unmanaged[Cdecl]<{string.Join(", ", ps.Select(UcoType).Append(oret))}>)&{n}";
			o.Add(Extras.Contains(n) ? $"\t\tt[\"{n}\"] = OperatingSystem.IsBrowser() ? 0 : {entry};" : $"\t\tt[\"{n}\"] = {entry};");
		}
		o.Add("\t}");
		o.Add("}");
		File.WriteAllText(output, string.Join("\r\n", o) + "\r\n");

		Console.WriteLine($"functions: passthrough {passthrough.Count}, renamed {renamed.Count}, translated {translated.Count}, hand {hand.Count}, unmapped {unmapped.Count}");
		Console.WriteLine($"unmapped: [{string.Join(", ", unmapped)}]");
		Console.WriteLine($"enum maps: {EnumMaps.Count}; structs converted: {converted}");
		Console.WriteLine($"problems: [{string.Join("; ", Problems)}]");
		Console.WriteLine($"unsupported structs: [{string.Join(", ", Unsupported)}]");
		Console.WriteLine($"wrote {output}");
		return unmapped.Count == 0 && Problems.Count == 0 && Unsupported.Count == 0 ? 0 : 1;
	}

	// ---------------------------------------------------------------- names on each side

	private static string? NewStruct(string old)
	{
		var n = Renames.Structs.GetValueOrDefault(old, old);
		return New.Structs.ContainsKey(n) ? n : null;
	}

	private static string? SilkType(string name)
	{
		foreach (var ns in new[] { "Silk.NET.WebGPU.", "Silk.NET.WebGPU.Extensions.WGPU." })
		{
			if (SilkAssemblies.Select(a => a.GetType(ns + name)).FirstOrDefault(t => t is not null) is { } t)
			{
				return t.FullName!;
			}
		}
		return null;
	}

	private static readonly Assembly[] SilkAssemblies =
	{
		typeof(Silk.NET.WebGPU.WebGPU).Assembly,
		typeof(Silk.NET.WebGPU.Extensions.WGPU.Wgpu).Assembly,
	};

	private static string SilkField(string structName, string cname)
	{
		var type = SilkAssemblies.Select(a => a.GetType(SilkType(structName)!)).First(t => t is not null)!;
		return type.GetFields(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(f => string.Equals(f.Name, cname, StringComparison.OrdinalIgnoreCase))?.Name
			?? throw new InvalidOperationException($"{structName}.{cname} not in Silk.NET");
	}

	private static (string Name, string Type) RawField(string structName, string cname)
	{
		foreach (var (type, name) in RawStructs["WGPU" + structName])
		{
			if (string.Equals(name, cname, StringComparison.OrdinalIgnoreCase))
			{
				return (name, type);
			}
		}
		throw new InvalidOperationException($"raw {structName}.{cname}");
	}

	// ---------------------------------------------------------------- what needs converting

	private static string Norm(string s) => s.Replace("_", "").ToLowerInvariant();

	private static bool EnumNeedsMap(string e)
	{
		var nv = New.Enums.GetValueOrDefault(e) ?? New.Flags.GetValueOrDefault(e);
		if (nv is null)
		{
			return false;
		}
		var byName = new Dictionary<string, long>();
		foreach (var (k, v) in nv)
		{
			byName[Norm(k)] = v;
		}
		var o2n = new SortedDictionary<long, long>();
		foreach (var (k, v) in Old.Enums[e])
		{
			if (k == "Force32")
			{
				continue;
			}
			if (Renames.EnumMembers.GetValueOrDefault(e)?.GetValueOrDefault(k) is { } target)
			{
				o2n[v] = nv[target];
			}
			else if (byName.TryGetValue(Norm(k), out var nvalue))
			{
				o2n[v] = nvalue;
			}
		}
		if (!o2n.Any(p => p.Key != p.Value))
		{
			return false;
		}
		// The first old value wins where several map to one new value, in old declaration order.
		var n2o = new SortedDictionary<long, long>();
		foreach (var (k, v) in Old.Enums[e])
		{
			if (o2n.TryGetValue(v, out var nvalue) && !n2o.ContainsKey(nvalue))
			{
				n2o[nvalue] = v;
			}
		}
		EnumMaps[e] = (o2n, n2o);
		return true;
	}

	private static bool StructNeeds(string n, HashSet<string> stack)
	{
		if (Needs.TryGetValue(n, out var known))
		{
			return known;
		}
		if (stack.Contains(n))
		{
			return false;
		}
		var nn = NewStruct(n);
		var r = HandStructs.Contains(n) || nn is null || nn != n;
		if (!r)
		{
			r = !Old.Structs[n].Select(f => (f.Name, f.Type, f.Ptr)).SequenceEqual(New.Structs[nn!].Select(f => (f.Name, f.Type, f.Ptr)));
		}
		foreach (var f in Old.Structs[n])
		{
			var t = f.Type.StartsWith("WGPU", StringComparison.Ordinal) ? f.Type[4..] : null;
			if (t is not null && (Renum.Contains(t) || FlagEnums.Contains(t) || (Old.Structs.ContainsKey(t) && StructNeeds(t, new(stack) { n }))))
			{
				r = true;
			}
		}
		Needs[n] = r;
		return r;
	}

	private static long? OldSType(string structName)
	{
		foreach (var e in new[] { "SType", "NativeSType" })
		{
			if (Old.Enums.GetValueOrDefault(e)?.TryGetValue(structName, out var v) == true)
			{
				return v;
			}
		}
		return null;
	}

	private static long NewSType(string structName)
	{
		foreach (var e in new[] { "SType", "NativeSType" })
		{
			if (New.Enums.GetValueOrDefault(e)?.TryGetValue(structName, out var v) == true)
			{
				return v;
			}
		}
		throw new InvalidOperationException($"no new SType for {structName}");
	}

	private static bool IsChained(List<CField> fields) => fields.Count > 0 && fields[0].Name == "chain";

	private static string? CountField(List<CField> fields, string name)
	{
		var names = fields.Select(f => f.Name).ToHashSet();
		var candidates = new[] { name[..^1] + "Count", name.EndsWith("ies", StringComparison.Ordinal) ? name[..^3] + "yCount" : null, name + "Count" };
		return candidates.FirstOrDefault(c => c is not null && names.Contains(c));
	}

	// ---------------------------------------------------------------- struct converters

	private static string ConvValue(string oldType, string src, string newType)
	{
		switch (Old.Kind(oldType))
		{
			case "enum":
				var e = oldType[4..];
				return $"({newType})" + (Renum.Contains(e) ? $"O2N_{e}((int){src})" : $"(int){src}");
			case "flags":
				return $"({newType})(ulong)(uint){src}";
			case "bool":
				return newType != "uint" ? $"({newType})({src} ? 1u : 0u)" : $"({src} ? 1u : 0u)";
			case "handle":
				return $"(IntPtr){src}";
			default:
				return src;
		}
	}

	private static void EmitStruct(string n, List<string> lines)
	{
		var nn = NewStruct(n)!;
		var of = Old.Structs[n];
		lines.Add($"\tinternal static void Fill({SilkType(n)}* s, WGPU{nn}* d, ref Arena a)");
		lines.Add("\t{");
		var nfields = New.Structs[nn].ToDictionary(f => f.Name);
		var handled = new HashSet<string>();
		foreach (var f in of)
		{
			if (handled.Contains(f.Name))
			{
				continue;
			}
			if (!nfields.TryGetValue(f.Name, out var g))
			{
				if (!f.Name.EndsWith("Count", StringComparison.Ordinal))
				{
					lines.Add($"\t\t// dropped: {f}");
				}
				continue;
			}
			var sf = SilkField(n, f.Name);
			var (rf, rtype) = RawField(nn, g.Name);
			var src = $"s->{sf}";
			var kind = Old.Kind(f.Type);
			var t = f.Type.Length > 4 ? f.Type[4..] : f.Type;
			if (f.Name == "chain")
			{
				lines.Add($"\t\td->{rf}.Next = (IntPtr)Chain(s->{sf}.Next, ref a);");
				lines.Add($"\t\td->{rf}.SType = (WGPUSType){NewSType(nn)};");
			}
			else if (f.Name == "nextInChain")
			{
				lines.Add($"\t\td->{rf} = ({rtype})Chain((Silk.NET.WebGPU.ChainedStruct*){src}, ref a);");
			}
			else if (f.Type == "char" && f.Ptr == 1 && g.Type == "WGPUStringView")
			{
				lines.Add($"\t\td->{rf} = Str({src});");
			}
			else if (f.Ptr == 0 && kind == "struct")
			{
				lines.Add(Needs.GetValueOrDefault(t) ? $"\t\tFill(&s->{sf}, &d->{rf}, ref a);" : $"\t\td->{rf} = *({rtype}*)&s->{sf};");
			}
			else if (f.Ptr == 0)
			{
				lines.Add($"\t\td->{rf} = {ConvValue(f.Type, src, rtype)};");
			}
			else if (f.Ptr == 1 && kind is "struct" or "enum" or "handle")
			{
				var cnt = CountField(of, f.Name);
				var elemNeeds = (kind == "struct" && Needs.GetValueOrDefault(t)) || (kind == "enum" && Renum.Contains(t));
				if (cnt is not null)
				{
					var scnt = SilkField(n, cnt);
					if (nfields.ContainsKey(cnt))
					{
						lines.Add($"\t\td->{RawField(nn, cnt).Name} = s->{scnt};");
						handled.Add(cnt);
					}
					if (!elemNeeds)
					{
						lines.Add($"\t\td->{rf} = ({rtype})s->{sf};");
					}
					else
					{
						var et = "WGPU" + (kind == "struct" ? NewStruct(t) : t);
						var convert = kind == "struct" ? $"Fill(&s->{sf}[i], &arr[i], ref a);" : $"arr[i] = ({et})O2N_{t}((int)s->{sf}[i]);";
						lines.Add($"\t\tif (s->{sf} != null)");
						lines.Add("\t\t{");
						lines.Add($"\t\t\tvar arr = ({et}*)a.Alloc(sizeof({et}) * (int)s->{scnt});");
						lines.Add($"\t\t\tfor (var i = 0; i < (int)s->{scnt}; i++) {{ {convert} }}");
						lines.Add($"\t\t\td->{rf} = arr;");
						lines.Add("\t\t}");
					}
				}
				else if (kind == "handle")
				{
					lines.Add($"\t\td->{rf} = (IntPtr)s->{sf};");
				}
				else if (!elemNeeds)
				{
					lines.Add($"\t\td->{rf} = ({rtype})s->{sf};");
				}
				else
				{
					var et = "WGPU" + NewStruct(t);
					lines.Add($"\t\tif (s->{sf} != null) {{ var p = ({et}*)a.Alloc(sizeof({et})); Fill(s->{sf}, p, ref a); d->{rf} = p; }}");
				}
			}
			else if (f.Ptr == 1 && (f.Type == "void" || kind == "prim"))
			{
				lines.Add($"\t\td->{rf} = ({rtype})s->{sf};");
			}
			else
			{
				Problems.Add($"{n}.{f.Name}: {f} -> {g}");
				lines.Add($"\t\t// TODO {f} -> {g}");
			}
		}
		var oldNames = of.Select(f => f.Name).ToHashSet();
		foreach (var g in New.Structs[nn])
		{
			if (!oldNames.Contains(g.Name) && NewFieldDefaults.TryGetValue((nn, g.Name), out var value))
			{
				lines.Add($"\t\td->{RawField(nn, g.Name).Name} = {value};");
			}
		}
		lines.Add("\t}");
		lines.Add("");
	}

	// ---------------------------------------------------------------- browser forwarders

	// In the browser the natives are linked into the module and only reachable through [DllImport]s, so a function
	// table can't hold their addresses: each passthrough gets an [UnmanagedCallersOnly] forwarder there instead.
	private static IEnumerable<string> BrowserForwarders(IEnumerable<(string Old, string New)> functions)
	{
		foreach (var name in functions.Select(f => f.New).Distinct())
		{
			var (ret, ps) = RawFuncs[name];
			yield return "\t[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]";
			yield return $"\tprivate static {ret} Browser_{name}({string.Join(", ", ps.Select(p => $"{p.Type} {p.Name}"))})";
			yield return $"\t\t=> WGPU.{name}({string.Join(", ", ps.Select(p => p.Name))});";
			yield return "";
		}
	}

	// The browser has none of wgpu-native's own functions, so they get no entry there.
	private static string BrowserEntry(string name) => Extras.Contains(name) ? "0" : BrowserForwarderPointer(name);

	private static string BrowserForwarderPointer(string name)
	{
		var (ret, ps) = RawFuncs[name];
		return $"(nint)(delegate* unmanaged[Cdecl]<{string.Join(", ", ps.Select(p => p.Type).Append(ret))}>)&Browser_{name}";
	}

	// The reverse of a Fill, for an output struct of plain fields: copies a stable struct back into its old shape.
	private static void EmitUnfill(string n, List<string> lines)
	{
		var nn = NewStruct(n)!;
		var nfields = New.Structs[nn].ToDictionary(f => f.Name);
		lines.Add($"\tinternal static void Unfill(WGPU{nn}* s, {SilkType(n)}* d)");
		lines.Add("\t{");
		foreach (var f in Old.Structs[n])
		{
			var sf = SilkField(n, f.Name);
			if (OldFieldFromNew.TryGetValue((n, f.Name), out var expression))
			{
				lines.Add($"\t\td->{sf} = {expression};");
			}
			else if (nfields.TryGetValue(f.Name, out var g) && (g.Type, g.Ptr) == (f.Type, f.Ptr) && Old.Kind(f.Type) == "prim")
			{
				lines.Add($"\t\td->{sf} = s->{RawField(nn, g.Name).Name};");
			}
			else
			{
				Problems.Add($"Unfill {n}.{f.Name}: {f}");
			}
		}
		lines.Add("\t}");
		lines.Add("");
	}

	// ---------------------------------------------------------------- function translators

	/// <summary>The C# type an [UnmanagedCallersOnly] parameter takes for an old-ABI parameter.</summary>
	private static string UcoType(CField f)
	{
		if (f.Ptr > 0)
		{
			return "nint";
		}
		return Old.Kind(f.Type) switch
		{
			"enum" => "int",
			"flags" => "uint",
			"bool" => "uint",
			"handle" => "nint",
			"struct" => SilkType(f.Type[4..])!,
			_ => CsPrim.GetValueOrDefault(f.Type, f.Type),
		};
	}

	private static bool ParamNeeds(CField f)
	{
		var t = f.Type.StartsWith("WGPU", StringComparison.Ordinal) ? f.Type[4..] : null;
		return (f.Type == "char" && f.Ptr > 0)
			|| (t is not null && (Renum.Contains(t) || FlagEnums.Contains(t) || (Old.Structs.ContainsKey(t) && Needs.GetValueOrDefault(t)) || Old.Callbacks.ContainsKey(t)));
	}

	private static (List<string> Passthrough, List<(string Old, string New)> Renamed, List<string> Translated, List<string> Hand, List<string> Unmapped, List<string> Lines) EmitFunctions()
	{
		var passthrough = new List<string>();
		var renamed = new List<(string, string)>();
		var translated = new List<string>();
		var hand = new List<string>();
		var unmapped = new List<string>();
		var lines = new List<string>();
		foreach (var (name, (ret, ps)) in Old.Funcs)
		{
			if (Hand.Contains(name))
			{
				hand.Add(name);
				continue;
			}
			if (name.EndsWith("Reference", StringComparison.Ordinal) && New.Funcs.ContainsKey(name.Replace("Reference", "AddRef")))
			{
				renamed.Add((name, name.Replace("Reference", "AddRef")));
				continue;
			}
			if (!New.Funcs.TryGetValue(name, out var nf))
			{
				unmapped.Add(name);
				continue;
			}
			var sameSig = ps.Select(f => (f.Type, f.Ptr)).SequenceEqual(nf.Params.Select(f => (f.Type, f.Ptr))) && ret == nf.Ret;
			if (sameSig && !ps.Any(ParamNeeds) && !ParamNeeds(CField.Return(ret)))
			{
				passthrough.Add(name);
				continue;
			}
			if (!RawFuncs.TryGetValue(name, out var raw) || nf.Params.Count != ps.Count)
			{
				unmapped.Add(name);
				continue;
			}
			translated.Add(name);
			var oret = ret == "void" ? "void" : UcoType(CField.Return(ret));
			lines.Add("\t[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]");
			lines.Add($"\tprivate static {oret} {name}({string.Join(", ", ps.Select(f => $"{UcoType(f)} {f.Name}"))})");
			lines.Add("\t{");
			if (name.EndsWith("SetLabel", StringComparison.Ordinal) && oret == "void")
			{
				// wgpu-native doesn't implement labels and aborts on them; only the browser's emdawnwebgpu gets them.
				lines.Add("\t\tif (!OperatingSystem.IsBrowser()) { return; }");
			}
			lines.Add("\t\tvar a = Arena.Rent();");
			var callArgs = new List<string>();
			for (var i = 0; i < ps.Count; i++)
			{
				var f = ps[i];
				var rtype = raw.Params[i].Type;
				var t = f.Type.StartsWith("WGPU", StringComparison.Ordinal) ? f.Type[4..] : null;
				if (f.Type == "char" && f.Ptr == 1)
				{
					callArgs.Add($"Str((byte*){f.Name})");
				}
				else if (f.Ptr == 1 && t is not null && Old.Structs.ContainsKey(t) && Needs.GetValueOrDefault(t))
				{
					var nt = "WGPU" + NewStruct(t);
					lines.Add($"\t\t{nt}* c_{f.Name} = null;");
					lines.Add($"\t\tif ({f.Name} != 0) {{ c_{f.Name} = ({nt}*)a.Alloc(sizeof({nt})); Fill(({SilkType(t)}*){f.Name}, c_{f.Name}, ref a); }}");
					callArgs.Add($"c_{f.Name}");
				}
				else if (f.Ptr >= 1)
				{
					callArgs.Add(rtype != "IntPtr" ? $"({rtype}){f.Name}" : $"(IntPtr){f.Name}");
				}
				else if (Old.Kind(f.Type) == "enum")
				{
					callArgs.Add(Renum.Contains(t!) ? $"({rtype})O2N_{t}({f.Name})" : $"({rtype}){f.Name}");
				}
				else if (Old.Kind(f.Type) == "flags")
				{
					callArgs.Add($"({rtype})(ulong){f.Name}");
				}
				else if (Old.Kind(f.Type) == "struct")
				{
					callArgs.Add($"/* TODO by-value {f} */ default");
					Problems.Add($"{name}: by-value struct param {f}");
				}
				else
				{
					callArgs.Add(rtype is "uint" or "int" or "ulong" or "nuint" or "float" or "double" or "long" ? f.Name : $"({rtype}){f.Name}");
				}
			}
			var call = $"WGPU.{name}({string.Join(", ", callArgs)})";
			if (oret == "void")
			{
				lines.Add($"\t\t{call};");
				lines.Add("\t\ta.Dispose();");
			}
			else
			{
				var r = CField.Return(ret);
				var rk = Old.Kind(r.Type);
				var rt = r.Type.Length > 4 ? r.Type[4..] : r.Type;
				lines.Add($"\t\tvar r = {call};");
				lines.Add("\t\ta.Dispose();");
				lines.Add(rk == "enum" && Renum.Contains(rt) ? $"\t\treturn N2O_{rt}((int)r);"
					: rk == "enum" ? "\t\treturn (int)r;"
					: rk == "flags" ? "\t\treturn (uint)r;"
					: rk == "handle" || r.Ptr > 0 ? "\t\treturn (nint)r;"
					: "\t\treturn r;");
			}
			lines.Add("\t}");
			lines.Add("");
		}
		return (passthrough, renamed, translated, hand, unmapped, lines);
	}
}
