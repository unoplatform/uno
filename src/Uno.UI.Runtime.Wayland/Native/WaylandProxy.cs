#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.Runtime.Wayland.Native;

namespace Uno.UI.Runtime.Wayland;

internal abstract unsafe class WaylandProxy : IDisposable
{
	private GCHandle _self;
	private nint _handle;
	public WaylandDisplay Display { get; }
	public nint Handle
	{
		get
		{
			ObjectDisposedException.ThrowIf(_handle == 0, this);
			return _handle;
		}
	}
	public uint Version { get; }
	public abstract string InterfaceName { get; }
	protected virtual uint? DestructorOpcode => null;
	protected virtual uint DestructorSince => 1;

	protected WaylandProxy(WaylandDisplay display, nint handle)
	{
		if (handle == 0)
		{
			throw new InvalidOperationException("libwayland failed to create a proxy.");
		}
		Display = display;
		_handle = handle;
		Version = WaylandNative.wl_proxy_get_version(handle);
		_self = GCHandle.Alloc(this);
		WaylandNative.wl_proxy_set_user_data(handle, GCHandle.ToIntPtr(_self));
		display.Register(this);
	}

	protected void AddListener(nint* listener)
	{
		if (WaylandNative.wl_proxy_add_listener(Handle, listener, GCHandle.ToIntPtr(_self)) != 0)
		{
			DisposeLocal();
			throw new InvalidOperationException("The Wayland proxy already has a listener.");
		}
	}

	internal nint MarshalRequest(uint opcode, string? resultInterface, uint resultVersion, uint since, bool destructor, RequestArguments args)
	{
		using (Display.Lock())
		{
			Display.ThrowIfUnavailable();
			if (Version < since)
			{
				throw new NotSupportedException($"{InterfaceName} request requires version {since}; bound version is {Version}.");
			}
			var result = WaylandNative.wl_proxy_marshal_array_flags(Handle, opcode,
				resultInterface is null ? null : ProtocolMetadata.Get(resultInterface), resultVersion, destructor ? 1u : 0u, args.Pointer);
			if (destructor)
			{
				Release();
			}
			Display.RequestSent();
			return result;
		}
	}

	public void Dispose()
	{
		using (Display.Lock())
		{
			if (_handle == 0)
			{
				return;
			}
			if (DestructorOpcode is { } opcode && Version >= DestructorSince && !Display.IsDisconnected)
			{
				using var args = new RequestArguments(0);
				WaylandNative.wl_proxy_marshal_array_flags(_handle, opcode, null, Version, 1, args.Pointer);
				Release();
				Display.RequestSent();
			}
			else
			{
				DisposeLocal();
			}
		}
	}

	internal void DisposeLocal()
	{
		if (_handle != 0)
		{
			WaylandNative.wl_proxy_destroy(_handle);
			Release();
		}
	}

	private void Release()
	{
		Display.Unregister(_handle);
		_handle = 0;
		if (_self.IsAllocated)
		{
			_self.Free();
		}
	}

	protected static T Target<T>(nint data) where T : WaylandProxy => (T)GCHandle.FromIntPtr(data).Target!;
	protected static byte[] ReadArray(nint pointer)
	{
		var array = (WlArray*)pointer;
		return new ReadOnlySpan<byte>((void*)array->Data, checked((int)array->Size)).ToArray();
	}
	protected static string ReadString(nint pointer) => Marshal.PtrToStringUTF8(pointer) ?? string.Empty;
}

internal readonly record struct WaylandFixed(int RawValue)
{
	public double ToDouble() => RawValue / 256.0;
	public static WaylandFixed FromDouble(double value) => new(checked((int)Math.Round(value * 256.0)));
}
