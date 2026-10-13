#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland.Native;

internal sealed record MessageSpec(string Name, string Signature, string?[] Types);
internal sealed record InterfaceSpec(string Name, int Version, MessageSpec[] Requests, MessageSpec[] Events);

internal static unsafe class ProtocolMetadata
{
	// libwayland retains these pointers; interface tables have process lifetime.
	private static readonly Dictionary<string, nint> Interfaces = Create();
	internal static WlInterface* Get(string name) => (WlInterface*)Interfaces[name];

	private static Dictionary<string, nint> Create()
	{
		var result = new Dictionary<string, nint>(StringComparer.Ordinal);
		foreach (var spec in ProtocolCatalog.Interfaces)
		{
			result.Add(spec.Name, (nint)NativeMemory.AllocZeroed((nuint)sizeof(WlInterface)));
		}
		foreach (var spec in ProtocolCatalog.Interfaces)
		{
			var pointer = (WlInterface*)result[spec.Name];
			pointer->Name = Utf8(spec.Name);
			pointer->Version = spec.Version;
			pointer->MethodCount = spec.Requests.Length;
			pointer->Methods = Messages(spec.Requests, result);
			pointer->EventCount = spec.Events.Length;
			pointer->Events = Messages(spec.Events, result);
		}
		return result;
	}

	private static WlMessage* Messages(MessageSpec[] specs, Dictionary<string, nint> interfaces)
	{
		var messages = (WlMessage*)NativeMemory.AllocZeroed((nuint)specs.Length, (nuint)sizeof(WlMessage));
		for (var i = 0; i < specs.Length; i++)
		{
			messages[i].Name = Utf8(specs[i].Name);
			messages[i].Signature = Utf8(specs[i].Signature);
			messages[i].Types = (WlInterface**)NativeMemory.AllocZeroed((nuint)specs[i].Types.Length, (nuint)sizeof(nint));
			for (var j = 0; j < specs[i].Types.Length; j++)
			{
				if (specs[i].Types[j] is { } name)
				{
					messages[i].Types[j] = (WlInterface*)interfaces[name];
				}
			}
		}
		return messages;
	}

	internal static byte* Utf8(string value)
	{
		var bytes = Encoding.UTF8.GetBytes(value + '\0');
		var pointer = (byte*)NativeMemory.Alloc((nuint)bytes.Length);
		bytes.CopyTo(new Span<byte>(pointer, bytes.Length));
		return pointer;
	}
}

internal sealed unsafe class RequestArguments : IDisposable
{
	private readonly List<nint> _allocations = new();
	internal WlArgument* Pointer { get; }
	internal RequestArguments(int count)
	{
		Pointer = (WlArgument*)NativeMemory.AllocZeroed((nuint)Math.Max(count, 1), (nuint)sizeof(WlArgument));
	}
	internal void Int(int index, int value) => Pointer[index].Int = value;
	internal void Uint(int index, uint value) => Pointer[index].Uint = value;
	internal void Object(int index, WaylandProxy? value) => Pointer[index].Pointer = value?.Handle ?? 0;
	internal void String(int index, string? value)
	{
		if (value is not null)
		{
			var pointer = ProtocolMetadata.Utf8(value);
			_allocations.Add((nint)pointer);
			Pointer[index].Pointer = (nint)pointer;
		}
	}
	internal void Array(int index, byte[] value)
	{
		var data = NativeMemory.Alloc((nuint)Math.Max(value.Length, 1));
		value.CopyTo(new Span<byte>(data, value.Length));
		var array = (WlArray*)NativeMemory.AllocZeroed((nuint)sizeof(WlArray));
		array->Size = array->Alloc = (nuint)value.Length;
		array->Data = (nint)data;
		_allocations.Add((nint)data);
		_allocations.Add((nint)array);
		Pointer[index].Pointer = (nint)array;
	}
	public void Dispose()
	{
		foreach (var pointer in _allocations)
		{
			NativeMemory.Free((void*)pointer);
		}
		NativeMemory.Free(Pointer);
	}
}
