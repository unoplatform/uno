// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.UI.Runtime.Skia.Vulkan.Interop;

internal unsafe class Utf8BufferArray : IDisposable
{
	private readonly List<IntPtr> _buffers = new();
	private byte** _bufferArray;

	public Utf8BufferArray(IEnumerable<string> strings)
	{
		var list = strings.ToList();
		_bufferArray = (byte**)Marshal.AllocHGlobal(list.Count * IntPtr.Size);
		for (var c = 0; c < list.Count; c++)
		{
			var bytes = Encoding.UTF8.GetBytes(list[c] + '\0');
			var ptr = Marshal.AllocHGlobal(bytes.Length);
			Marshal.Copy(bytes, 0, ptr, bytes.Length);
			_buffers.Add(ptr);
			_bufferArray[c] = (byte*)ptr;
		}
	}

	public static implicit operator byte**(Utf8BufferArray a) => a._bufferArray;

	public int Count => _buffers.Count;
	public uint UCount => (uint)Count;

	public void Dispose() => Dispose(true);

	private void Dispose(bool disposing)
	{
		if (_bufferArray != null)
			Marshal.FreeHGlobal(new IntPtr(_bufferArray));
		_bufferArray = null;
		if (disposing)
		{
			foreach (var b in _buffers)
				Marshal.FreeHGlobal(b);
			_buffers.Clear();
			GC.SuppressFinalize(this);
		}
	}

	~Utf8BufferArray()
	{
		Dispose(false);
	}
}
