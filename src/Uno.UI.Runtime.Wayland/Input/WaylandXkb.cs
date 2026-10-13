#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace Uno.UI.Runtime.Wayland.Input;

// Minimal libxkbcommon binding for the Wayland keyboard.
internal static partial class WaylandXkb
{
	private const string Library = "libxkbcommon.so.0";
	[LibraryImport(Library, EntryPoint = "xkb_context_new")] internal static partial nint ContextNew(uint flags);
	[LibraryImport(Library, EntryPoint = "xkb_context_unref")] internal static partial void ContextUnref(nint context);
	[LibraryImport(Library, EntryPoint = "xkb_keymap_new_from_string", StringMarshalling = StringMarshalling.Utf8)] internal static partial nint KeymapNew(nint context, string text, uint format, uint flags);
	[LibraryImport(Library, EntryPoint = "xkb_keymap_unref")] internal static partial void KeymapUnref(nint keymap);
	[LibraryImport(Library, EntryPoint = "xkb_state_new")] internal static partial nint StateNew(nint keymap);
	[LibraryImport(Library, EntryPoint = "xkb_state_unref")] internal static partial void StateUnref(nint state);
	[LibraryImport(Library, EntryPoint = "xkb_state_update_mask")] internal static partial uint UpdateMask(nint state, uint depressed, uint latched, uint locked, uint depressedLayout, uint latchedLayout, uint lockedLayout);
	[LibraryImport(Library, EntryPoint = "xkb_state_mod_name_is_active", StringMarshalling = StringMarshalling.Utf8)] internal static partial int ModActive(nint state, string name, uint components);
	[LibraryImport(Library, EntryPoint = "xkb_state_key_get_one_sym")] internal static partial uint GetSym(nint state, uint key);
	[LibraryImport(Library, EntryPoint = "xkb_state_key_get_utf32")] internal static partial uint GetUtf32(nint state, uint key);
	[LibraryImport(Library, EntryPoint = "xkb_keymap_key_repeats")] internal static partial int Repeats(nint keymap, uint key);

	[LibraryImport(Library, EntryPoint = "xkb_compose_table_new_from_locale", StringMarshalling = StringMarshalling.Utf8)] internal static partial nint ComposeTableNew(nint context, string locale, uint flags);
	[LibraryImport(Library, EntryPoint = "xkb_compose_table_unref")] internal static partial void ComposeTableUnref(nint table);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_new")] internal static partial nint ComposeStateNew(nint table, uint flags);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_unref")] internal static partial void ComposeStateUnref(nint state);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_feed")] internal static partial int ComposeFeed(nint state, uint keysym);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_get_status")] internal static partial int ComposeStatus(nint state);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_get_utf8")] internal static partial int ComposeGetUtf8(nint state, [Out] byte[] buffer, nuint size);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_reset")] internal static partial void ComposeReset(nint state);

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
