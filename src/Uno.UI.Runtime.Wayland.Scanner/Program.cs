#nullable enable

using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Uno.UI.Runtime.Wayland.Scanner;

// Generates the protocol bindings of Uno.UI.Runtime.Wayland from the protocol XMLs next to them:
// dotnet run --project src/Uno.UI.Runtime.Wayland.Scanner -- src/Uno.UI.Runtime.Wayland/Protocols src/Uno.UI.Runtime.Wayland/Protocols

public static class Program
{
	// A new_id without an interface is marshalled as (interface name, version, id).
	private static readonly string[] DynamicNewIdTypes = ["null", "null", "null"];

	// Not generated because the host does not use them: wl_shell is superseded by xdg-shell, the tablet protocol is
	// not vendored, popups are drawn by Uno itself and foreign toplevels are only exported. Object arguments of these
	// types are typed as WaylandProxy and left untyped in the metadata; requests creating them are not generated.
	public static readonly IReadOnlySet<string> OmittedInterfaces = new HashSet<string>(StringComparer.Ordinal)
	{
		"wl_fixes",
		"wl_shell",
		"wl_shell_surface",
		"xdg_popup",
		"xdg_positioner",
		"zwp_tablet_tool_v2",
		"zxdg_imported_v2",
		"zxdg_importer_v2",
	};

	public static int Main(string[] args)
	{
		if (args.Length != 2)
		{
			Console.Error.WriteLine("Usage: Uno.UI.Runtime.Wayland.Scanner <protocol-directory> <output-directory>");
			return 2;
		}
		Generate(args[0], args[1]);
		return 0;
	}

	private static readonly HashSet<string> _proxyMembers = new(StringComparer.Ordinal) { "Display", "Handle", "Version", "InterfaceName", "Dispose" };

	public static string Name(string value) => string.Concat(value.Split('_', '-').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
	private static string Attr(XElement node, string name, string fallback = "") => (string?)node.Attribute(name) ?? fallback;
	private static int Since(XElement node) => int.Parse(Attr(node, "since", "1"), CultureInfo.InvariantCulture);
	private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
	private static string Type(XElement arg)
	{
		var nullable = Attr(arg, "allow-null") == "true" ? "?" : "";
		return Attr(arg, "type") switch
		{
			"int" or "fd" => "int",
			"uint" => "uint",
			"fixed" => "WaylandFixed",
			"string" => "string" + nullable,
			"array" => "byte[]",
			"object" or "new_id" => (Attr(arg, "interface") is { Length: > 0 } name && !OmittedInterfaces.Contains(name) ? Name(name) : "WaylandProxy") + nullable,
			var type => throw new InvalidDataException($"Unsupported argument type {type}")
		};
	}

	public static string Signature(XElement message)
	{
		var signature = Since(message) > 1 ? Since(message).ToString(CultureInfo.InvariantCulture) : "";
		foreach (var arg in message.Elements("arg"))
		{
			signature += Attr(arg, "allow-null") == "true" ? "?" : "";
			signature += Attr(arg, "type") switch
			{
				"int" => "i",
				"uint" => "u",
				"fixed" => "f",
				"string" => "s",
				"object" => "o",
				"new_id" => Attr(arg, "interface") == "" ? "sun" : "n",
				"array" => "a",
				"fd" => "h",
				var type => throw new InvalidDataException($"Unsupported argument type {type}")
			};
		}
		return signature;
	}

	public static void Generate(string directory, string output)
	{
		var interfaces = Directory.GetFiles(directory, "*.xml").Order(StringComparer.Ordinal)
			.SelectMany(path => XDocument.Load(path).Root!.Elements("interface"))
			.Where(node => !OmittedInterfaces.Contains(Attr(node, "name")))
			.ToArray();
		var names = interfaces.Select(node => Attr(node, "name")).ToHashSet(StringComparer.Ordinal);
		if (names.Count != interfaces.Length)
		{
			throw new InvalidDataException("Duplicate protocol interface.");
		}
		foreach (var arg in interfaces.SelectMany(node => node.Descendants("arg")))
		{
			if (Attr(arg, "interface") is { Length: > 0 } reference && !names.Contains(reference))
			{
				// An event creating an omitted object would need its interface to build the proxy.
				if (!OmittedInterfaces.Contains(reference) || (Attr(arg, "type") == "new_id" && arg.Parent!.Name != "request"))
				{
					throw new InvalidDataException($"Unresolved interface {reference}");
				}
			}
		}
		Directory.CreateDirectory(output);
		foreach (var node in interfaces)
		{
			File.WriteAllText(Path.Combine(output, Name(Attr(node, "name")) + ".g.cs"), TrimLines(Proxy(node)));
		}
		var catalog = Header();
		catalog.AppendLine("internal static unsafe class ProtocolCatalog\n{\n\tinternal static readonly InterfaceSpec[] Interfaces =\n\t[");
		foreach (var node in interfaces)
		{
			catalog.AppendLine($"\t\tnew({Quote(Attr(node, "name"))}, {Attr(node, "version")},");
			foreach (var kind in new[] { "request", "event" })
			{
				catalog.AppendLine("\t\t[");
				foreach (var message in node.Elements(kind))
				{
					var types = message.Elements("arg").SelectMany(arg => Attr(arg, "type") == "new_id" && Attr(arg, "interface") == "" ? DynamicNewIdTypes : new[] { Attr(arg, "interface") is { Length: > 0 } name && !OmittedInterfaces.Contains(name) ? Quote(name) : "null" });
					catalog.AppendLine($"\t\t\tnew({Quote(Attr(message, "name"))}, {Quote(Signature(message))}, [{string.Join(", ", types)}]),");
				}
				catalog.AppendLine(kind == "request" ? "\t\t]," : "\t\t]),");
			}
		}
		catalog.AppendLine("\t];\n\tinternal static WaylandProxy Create(string name, WaylandDisplay display, nint handle) => name switch\n\t{");
		foreach (var node in interfaces.Where(node => Attr(node, "name") != "wl_display"))
		{
			catalog.AppendLine($"\t\t{Quote(Attr(node, "name"))} => new {Name(Attr(node, "name"))}(display, handle),");
		}
		catalog.AppendLine("\t\t_ => throw new ArgumentException(\"Unknown protocol interface.\", nameof(name))\n\t};\n}");
		File.WriteAllText(Path.Combine(output, "ProtocolCatalog.g.cs"), TrimLines(catalog.ToString()));
	}

	private static string TrimLines(string source) => string.Join("\n", source.Split('\n').Select(line => line.TrimEnd()));

	private static StringBuilder Header() => new("// <auto-generated by Uno.UI.Runtime.Wayland.Scanner />\n#nullable enable\nusing System;\nusing System.Runtime.CompilerServices;\nusing System.Runtime.InteropServices;\nusing Uno.UI.Runtime.Wayland.Native;\n\nnamespace Uno.UI.Runtime.Wayland.Protocols;\n\n");

	private static string Proxy(XElement node)
	{
		var name = Name(Attr(node, "name"));
		var requests = node.Elements("request").ToArray();
		var events = node.Elements("event").ToArray();
		var builder = new StringBuilder();
		if (node.Parent?.Element("copyright") is { } copyright)
		{
			foreach (var line in copyright.Value.Trim().Split('\n'))
			{
				builder.AppendLine("// " + line.Trim());
			}
		}
		builder.Append(Header());
		builder.AppendLine($"internal sealed unsafe class {name} : WaylandProxy\n{{\n\tpublic const string ProtocolName = {Quote(Attr(node, "name"))};\n\tpublic const uint MaximumVersion = {Attr(node, "version")};\n\tpublic override string InterfaceName => ProtocolName;");
		var destructor = Array.FindIndex(requests, request => Attr(request, "type") == "destructor");
		if (destructor >= 0)
		{
			builder.AppendLine($"\tprotected override uint? DestructorOpcode => {destructor};\n\tprotected override uint DestructorSince => {Since(requests[destructor])};");
		}
		builder.AppendLine($"\tinternal {name}(WaylandDisplay display, nint handle) : base(display, handle)\n\t{{");
		if (events.Length > 0 && name != "WlDisplay")
		{
			builder.AppendLine("\t\tAddListener(Listener); ");
		}
		builder.AppendLine("\t}");
		foreach (var enumeration in node.Elements("enum"))
		{
			if (Attr(enumeration, "bitfield") == "true")
			{
				builder.AppendLine("\t[Flags]");
			}
			builder.AppendLine($"\tpublic enum {Name(Attr(enumeration, "name"))}Values : uint\n\t{{");
			foreach (var entry in enumeration.Elements("entry"))
			{
				var member = Name(Attr(entry, "name"));
				if (char.IsDigit(member[0]))
				{
					member = "Value" + member;
				}
				builder.AppendLine($"\t\t{member} = {Attr(entry, "value")},");
			}
			builder.AppendLine("\t}");
		}
		for (var opcode = 0; opcode < requests.Length; opcode++)
		{
			var request = requests[opcode];
			var args = request.Elements("arg").ToArray();
			var newId = args.SingleOrDefault(arg => Attr(arg, "type") == "new_id");
			if (newId is not null && OmittedInterfaces.Contains(Attr(newId, "interface")))
			{
				continue;
			}
			var dynamic = newId is not null && Attr(newId, "interface") == "";
			var parameters = args.Where(arg => arg != newId).Select(arg => Type(arg) + " @" + Attr(arg, "name")).ToList();
			if (dynamic)
			{
				parameters.Add("string @interface");
				parameters.Add("uint version");
			}
			var resultType = newId is null ? "void" : Type(newId);
			var requestStart = builder.Length;
			builder.AppendLine($"\tpublic {resultType} {Name(Attr(request, "name"))}({string.Join(", ", parameters)})\n\t{{\n\t\tlock (Display.SyncRoot)\n\t\t{{");
			if (dynamic)
			{
				builder.AppendLine("\t\tif (version == 0 || version > (uint)ProtocolMetadata.Get(@interface)->Version)\n\t\t{\n\t\t\tthrow new ArgumentOutOfRangeException(nameof(version));\n\t\t}");
			}
			builder.AppendLine($"\t\tusing var args = new RequestArguments({args.Length + (dynamic ? 2 : 0)});");
			var index = 0;
			foreach (var arg in args)
			{
				var variable = "@" + Attr(arg, "name");
				if (arg != newId && Attr(arg, "allow-null") != "true" && Attr(arg, "type") is "object" or "string" or "array")
				{
					builder.AppendLine($"\t\tArgumentNullException.ThrowIfNull({variable});");
				}
				if (Attr(arg, "type") == "object")
				{
					builder.AppendLine($"\t\tif ({variable} is not null && {variable}.Display != Display)\n\t\t{{\n\t\t\tthrow new ArgumentException(\"The object belongs to another Wayland display.\", nameof({variable}));\n\t\t}}");
				}
				var type = Attr(arg, "type");
				if (arg == newId)
				{
					if (dynamic)
					{
						builder.AppendLine($"\t\targs.String({index++}, @interface);\n\t\targs.Uint({index++}, version);");
					}
					index++;
					continue;
				}
				var setter = type switch { "int" or "fixed" or "fd" => "Int", "uint" => "Uint", "string" => "String", "array" => "Array", _ => "Object" };
				builder.AppendLine($"\t\targs.{setter}({index++}, {variable}{(type == "fixed" ? ".RawValue" : "")});");
			}
			var resultInterface = newId is null ? "null" : dynamic ? "@interface" : Quote(Attr(newId, "interface"));
			var version = newId is null ? "Version" : dynamic ? "version" : $"Math.Min(Version, {Type(newId)}.MaximumVersion)";
			builder.AppendLine($"\t\t{(newId is null ? "" : "var proxyHandle = ")}base.MarshalRequest({opcode}, {resultInterface}, {version}, {Since(request)}, {(Attr(request, "type") == "destructor" ? "true" : "false")}, args);");
			if (newId is not null)
			{
				builder.AppendLine(dynamic ? "\t\treturn ProtocolCatalog.Create(@interface, Display, proxyHandle);" : $"\t\treturn new {Type(newId)}(Display, proxyHandle);");
			}
			builder.AppendLine("\t\t}\n\t}");
			var requestText = builder.ToString(requestStart, builder.Length - requestStart);
			var lines = requestText.Split('\n');
			for (var line = 4; line < lines.Length - 3; line++)
			{
				lines[line] = "\t" + lines[line];
			}
			builder.Length = requestStart;
			builder.Append(string.Join("\n", lines));
		}
		if (events.Length > 0)
		{
			builder.AppendLine("\tprivate static readonly nint* Listener = CreateListener();\n\tprivate static nint* CreateListener()\n\t{");
			builder.AppendLine($"\t\tvar table = (nint*)NativeMemory.Alloc((nuint){events.Length}, (nuint)sizeof(nint));");
			for (var opcode = 0; opcode < events.Length; opcode++)
			{
				var types = events[opcode].Elements("arg").Select(NativeType).Prepend("nint").Prepend("nint").Append("void");
				builder.AppendLine($"\t\ttable[{opcode}] = (nint)(delegate* unmanaged[Cdecl]<{string.Join(", ", types)}>)&On{opcode};");
			}
			builder.AppendLine("\t\treturn table;\n\t}");
		}
		for (var opcode = 0; opcode < events.Length; opcode++)
		{
			var evt = events[opcode];
			var eventName = Name(Attr(evt, "name"));
			// Events named like a WaylandProxy member (xdg_exported.handle) get a suffix to avoid hiding it.
			var eventMember = _proxyMembers.Contains(eventName) ? eventName + "Event" : eventName;
			var args = evt.Elements("arg").ToArray();
			builder.AppendLine($"\tpublic sealed class {eventName}EventArgs : EventArgs\n\t{{");
			foreach (var arg in args)
			{
				builder.AppendLine($"\t\tpublic {Type(arg)} {Name(Attr(arg, "name"))} {{ get; }}");
			}
			builder.AppendLine($"\t\tinternal {eventName}EventArgs({string.Join(", ", args.Select((arg, i) => Type(arg) + " arg" + i))})\n\t\t{{");
			for (var i = 0; i < args.Length; i++)
			{
				builder.AppendLine($"\t\t\t{Name(Attr(args[i], "name"))} = arg{i};");
			}
			builder.AppendLine($"\t\t}}\n\t}}\n\tpublic event EventHandler<{eventName}EventArgs>? {eventMember};");
			builder.AppendLine($"\t[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]\n\tprivate static void On{opcode}(nint data, nint proxy{string.Concat(args.Select((arg, i) => ", " + NativeType(arg) + " arg" + i))})\n\t{{\n\t\tvar target = Target<{name}>(data);\n\t\ttry\n\t\t{{");
			var values = args.Select((arg, i) => Attr(arg, "type") switch
			{
				"string" => Attr(arg, "allow-null") == "true" ? $"arg{i} == 0 ? null : ReadString(arg{i})" : $"ReadString(arg{i})",
				"array" => $"ReadArray(arg{i})",
				"fixed" => $"new WaylandFixed(arg{i})",
				"new_id" => $"new {Type(arg)}(target.Display, arg{i})",
				"object" => $"({Type(arg)})target.Display.FindProxy(arg{i})!",
				_ => $"arg{i}"
			});
			foreach (var pair in args.Select((arg, i) => (arg, i)).Where(pair => Attr(pair.arg, "type") == "fd"))
			{
				builder.AppendLine($"\t\t\tif (target.{eventMember} is null)\n\t\t\t{{\n\t\t\t\tPosix.close(arg{pair.i});\n\t\t\t\treturn;\n\t\t\t}}");
			}
			builder.AppendLine($"\t\t\tvar args = new {eventName}EventArgs({string.Join(", ", values)});\n\t\t\ttarget.{eventMember}?.Invoke(target, args);\n\t\t}}\n\t\tcatch (Exception error)\n\t\t{{\n\t\t\ttarget.Display.CaptureCallbackError(error, target, \"{eventName}\");\n\t\t}}");
			if (Attr(evt, "type") == "destructor")
			{
				builder.AppendLine("\t\tfinally\n\t\t{\n\t\t\ttarget.DisposeLocal();\n\t\t}");
			}
			builder.AppendLine("\t}");
		}
		builder.AppendLine("}");
		return builder.ToString();
	}

	private static string NativeType(XElement arg) => Attr(arg, "type") switch
	{
		"uint" => "uint",
		"int" or "fd" or "fixed" => "int",
		_ => "nint"
	};
}
