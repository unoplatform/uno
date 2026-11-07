#nullable enable

using System;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace Uno.UI.Runtime.Wayland.Input;

// Reads the keymap a compositor shares through wl_keyboard.keymap.
internal static class WaylandXkb
{
	internal static string ReadKeymap(int fd, uint size)
	{
		using var handle = new SafeFileHandle(fd, ownsHandle: true);
		if (size == 0 || size > 16 * 1024 * 1024)
		{
			throw new InvalidDataException("Invalid Wayland keymap size.");
		}
		var bytes = new byte[checked((int)size)];
		var offset = 0;
		while (offset < bytes.Length)
		{
			var read = RandomAccess.Read(handle, bytes.AsSpan(offset), offset);
			if (read == 0)
			{
				throw new EndOfStreamException("Truncated Wayland keymap.");
			}
			offset += read;
		}
		if (bytes[^1] != 0)
		{
			throw new InvalidDataException("Wayland keymap is not terminated.");
		}
		return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length - 1);
	}
}
