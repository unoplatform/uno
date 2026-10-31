#nullable enable

using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Linux.Input;

// Minimal libxkbcommon binding for the Linux keyboard hosts.
internal static unsafe partial class XkbNative
{
	private const string Library = "libxkbcommon.so.0";

	internal const uint KeymapFormatTextV1 = 1;
	internal const uint StateModsEffective = 8;
	internal const uint KeyUp = 0;
	internal const uint KeyDown = 1;

	[StructLayout(LayoutKind.Sequential)]
	internal struct RuleNames
	{
		public byte* Rules;
		public byte* Model;
		public byte* Layout;
		public byte* Variant;
		public byte* Options;
	}

	[LibraryImport(Library, EntryPoint = "xkb_context_new")] internal static partial nint ContextNew(uint flags);
	[LibraryImport(Library, EntryPoint = "xkb_context_unref")] internal static partial void ContextUnref(nint context);
	[LibraryImport(Library, EntryPoint = "xkb_keymap_new_from_string", StringMarshalling = StringMarshalling.Utf8)] internal static partial nint KeymapNewFromString(nint context, string text, uint format, uint flags);
	[LibraryImport(Library, EntryPoint = "xkb_keymap_new_from_names")] internal static partial nint KeymapNewFromNames(nint context, RuleNames* names, uint flags);
	[LibraryImport(Library, EntryPoint = "xkb_keymap_unref")] internal static partial void KeymapUnref(nint keymap);
	[LibraryImport(Library, EntryPoint = "xkb_keymap_key_repeats")] internal static partial int KeyRepeats(nint keymap, uint key);
	[LibraryImport(Library, EntryPoint = "xkb_state_new")] internal static partial nint StateNew(nint keymap);
	[LibraryImport(Library, EntryPoint = "xkb_state_unref")] internal static partial void StateUnref(nint state);
	[LibraryImport(Library, EntryPoint = "xkb_state_update_mask")] internal static partial uint StateUpdateMask(nint state, uint depressed, uint latched, uint locked, uint depressedLayout, uint latchedLayout, uint lockedLayout);
	[LibraryImport(Library, EntryPoint = "xkb_state_update_key")] internal static partial uint StateUpdateKey(nint state, uint key, uint direction);
	[LibraryImport(Library, EntryPoint = "xkb_state_mod_name_is_active", StringMarshalling = StringMarshalling.Utf8)] internal static partial int StateModNameIsActive(nint state, string name, uint components);
	[LibraryImport(Library, EntryPoint = "xkb_state_key_get_one_sym")] internal static partial uint StateKeyGetOneSym(nint state, uint key);
	[LibraryImport(Library, EntryPoint = "xkb_state_key_get_utf32")] internal static partial uint StateKeyGetUtf32(nint state, uint key);

	[LibraryImport(Library, EntryPoint = "xkb_compose_table_new_from_locale", StringMarshalling = StringMarshalling.Utf8)] internal static partial nint ComposeTableNewFromLocale(nint context, string locale, uint flags);
	[LibraryImport(Library, EntryPoint = "xkb_compose_table_unref")] internal static partial void ComposeTableUnref(nint table);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_new")] internal static partial nint ComposeStateNew(nint table, uint flags);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_unref")] internal static partial void ComposeStateUnref(nint state);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_feed")] internal static partial int ComposeStateFeed(nint state, uint keysym);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_get_status")] internal static partial int ComposeStateGetStatus(nint state);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_get_utf8")] internal static partial int ComposeStateGetUtf8(nint state, byte* buffer, nuint size);
	[LibraryImport(Library, EntryPoint = "xkb_compose_state_reset")] internal static partial void ComposeStateReset(nint state);
}
