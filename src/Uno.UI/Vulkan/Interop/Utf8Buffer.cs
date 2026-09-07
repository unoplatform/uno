#nullable enable
// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.UI.Runtime.Skia.Vulkan.Interop;

/// <summary>
/// A simple disposable wrapper around a pinned UTF-8 string buffer.
/// Replaces Avalonia.Platform.Interop.Utf8Buffer.
/// </summary>
internal unsafe class Utf8Buffer : IDisposable
{
	private GCHandle _gcHandle;
	private byte[]? _data;
	private byte* _pointer;

	public Utf8Buffer(string? s)
	{
		if (s == null)
			return;
		_data = Encoding.UTF8.GetBytes(s + '\0');
		_gcHandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
		_pointer = (byte*)_gcHandle.AddrOfPinnedObject();
	}

	public static implicit operator byte*(Utf8Buffer buf) => buf._pointer;
	public static implicit operator IntPtr(Utf8Buffer buf) => (IntPtr)buf._pointer;

	public void Dispose()
	{
		if (_gcHandle.IsAllocated)
			_gcHandle.Free();
		_pointer = null;
		_data = null;
	}
}
