#nullable enable

using System;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Windows.System;

namespace Uno.UI.Runtime.Linux.Input;

/// <summary>
/// The libxkbcommon keyboard shared by the Linux hosts: keysym and text lookup, modifiers, dead keys and Compose.
/// A Wayland host feeds it the compositor's modifier masks (<see cref="UpdateMask"/>); a host reading raw devices
/// feeds it every key (<see cref="UpdateKey"/>). Keys are evdev codes. Not thread-safe.
/// </summary>
internal sealed unsafe class XkbKeyboard : IDisposable
{
	// xkb keycodes are evdev codes offset by 8.
	private const uint EvdevOffset = 8;

	private nint _context;
	private nint _keymap;
	private nint _state;
	private nint _compose;

	private XkbKeyboard(nint context, nint keymap)
	{
		_context = context;
		_keymap = keymap;
		try
		{
			_state = XkbNative.StateNew(keymap);
			if (_state == 0)
			{
				throw new InvalidOperationException("Cannot create the XKB state.");
			}
			var table = XkbNative.ComposeTableNewFromLocale(context, GetComposeLocale(), 0);
			if (table != 0)
			{
				_compose = XkbNative.ComposeStateNew(table, 0);
				XkbNative.ComposeTableUnref(table);
			}
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	/// <summary>Compiles a keymap sent as text (xkb_v1), as a Wayland compositor does.</summary>
	internal static XkbKeyboard FromString(string keymap)
	{
		var context = CreateContext();
		var compiled = XkbNative.KeymapNewFromString(context, keymap, XkbNative.KeymapFormatTextV1, 0);
		return compiled != 0 ? new XkbKeyboard(context, compiled) : Fail(context, "Cannot compile the keymap.");
	}

	/// <summary>Compiles a keymap from RMLVO names; null or empty names use the XKB defaults.</summary>
	internal static XkbKeyboard FromNames(string? rules, string? model, string? layout, string? variant, string? options)
	{
		var context = CreateContext();
		var names = new XkbNative.RuleNames
		{
			Rules = Utf8StringMarshaller.ConvertToUnmanaged(rules),
			Model = Utf8StringMarshaller.ConvertToUnmanaged(model),
			Layout = Utf8StringMarshaller.ConvertToUnmanaged(layout),
			Variant = Utf8StringMarshaller.ConvertToUnmanaged(variant),
			Options = Utf8StringMarshaller.ConvertToUnmanaged(options),
		};
		try
		{
			var compiled = XkbNative.KeymapNewFromNames(context, &names, 0);
			return compiled != 0 ? new XkbKeyboard(context, compiled) : Fail(context, "Cannot compile the keymap from its names.");
		}
		finally
		{
			Utf8StringMarshaller.Free(names.Rules);
			Utf8StringMarshaller.Free(names.Model);
			Utf8StringMarshaller.Free(names.Layout);
			Utf8StringMarshaller.Free(names.Variant);
			Utf8StringMarshaller.Free(names.Options);
		}
	}

	public VirtualKeyModifiers Modifiers { get; private set; }

	/// <summary>Whether AltGr (level three) is active, which keeps text for Control or Alt combinations.</summary>
	public bool LevelThree { get; private set; }

	/// <summary>Applies the modifier and layout state the compositor reports.</summary>
	internal void UpdateMask(uint depressed, uint latched, uint locked, uint group)
	{
		_ = XkbNative.StateUpdateMask(_state, depressed, latched, locked, 0, 0, group);
		UpdateModifiers();
	}

	/// <summary>Applies a raw key press or release, for hosts that track the state themselves.</summary>
	internal void UpdateKey(uint key, bool down)
	{
		_ = XkbNative.StateUpdateKey(_state, key + EvdevOffset, down ? XkbNative.KeyDown : XkbNative.KeyUp);
		UpdateModifiers();
	}

	internal uint GetKeysym(uint key) => XkbNative.StateKeyGetOneSym(_state, key + EvdevOffset);

	internal VirtualKey GetVirtualKey(uint key) => LinuxKeyMap.FromKeysym(GetKeysym(key));

	/// <summary>The key's text as a UTF-32 code point, or zero.</summary>
	internal uint GetUtf32(uint key) => XkbNative.StateKeyGetUtf32(_state, key + EvdevOffset);

	internal bool Repeats(uint key) => XkbNative.KeyRepeats(_keymap, key + EvdevOffset) != 0;

	/// <summary>
	/// Feeds a pressed key to the dead-key and Compose state. Null when the key is consumed by a pending or cancelled
	/// sequence; otherwise the text to report, which a completed sequence replaces. A completed sequence also gives its
	/// whole text, which may not fit one code point.
	/// </summary>
	internal uint? Compose(uint keysym, uint unicode, out string? composedText)
	{
		composedText = null;
		if (_compose == 0 || XkbNative.ComposeStateFeed(_compose, keysym) == 0) // XKB_COMPOSE_FEED_IGNORED: modifiers
		{
			return unicode;
		}
		switch (XkbNative.ComposeStateGetStatus(_compose))
		{
			case 1: // XKB_COMPOSE_COMPOSING
				return null;
			case 2: // XKB_COMPOSE_COMPOSED
				var length = XkbNative.ComposeStateGetUtf8(_compose, null, 0);
				var buffer = new byte[checked(length + 1)];
				fixed (byte* bytes = buffer)
				{
					_ = XkbNative.ComposeStateGetUtf8(_compose, bytes, (nuint)buffer.Length);
				}
				var text = Encoding.UTF8.GetString(buffer, 0, length);
				ResetCompose();
				if (text.Length == 0)
				{
					return null;
				}
				composedText = text;
				return text.Length == 1 ? text[0] : 0u;
			case 3: // XKB_COMPOSE_CANCELLED
				ResetCompose();
				return null;
			default:
				return unicode;
		}
	}

	internal void ResetCompose()
	{
		if (_compose != 0)
		{
			XkbNative.ComposeStateReset(_compose);
		}
	}

	private void UpdateModifiers()
	{
		var modifiers = VirtualKeyModifiers.None;
		if (IsActive("Shift"))
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}
		if (IsActive("Control"))
		{
			modifiers |= VirtualKeyModifiers.Control;
		}
		if (IsActive("Mod1"))
		{
			modifiers |= VirtualKeyModifiers.Menu;
		}
		if (IsActive("Mod4"))
		{
			modifiers |= VirtualKeyModifiers.Windows;
		}
		Modifiers = modifiers;
		LevelThree = IsActive("Mod5");
	}

	private bool IsActive(string modifier) => XkbNative.StateModNameIsActive(_state, modifier, XkbNative.StateModsEffective) > 0;

	private static nint CreateContext()
	{
		var context = XkbNative.ContextNew(0);
		return context != 0 ? context : throw new InvalidOperationException("Cannot create the XKB context.");
	}

	private static XkbKeyboard Fail(nint context, string message)
	{
		XkbNative.ContextUnref(context);
		throw new InvalidOperationException(message);
	}

	private static string GetComposeLocale()
	{
		foreach (var variable in new[] { "LC_ALL", "LC_CTYPE", "LANG" })
		{
			if (Environment.GetEnvironmentVariable(variable) is { Length: > 0 } locale)
			{
				return locale;
			}
		}
		return "C";
	}

	public void Dispose()
	{
		if (_compose != 0)
		{
			XkbNative.ComposeStateUnref(_compose);
			_compose = 0;
		}
		if (_state != 0)
		{
			XkbNative.StateUnref(_state);
			_state = 0;
		}
		if (_keymap != 0)
		{
			XkbNative.KeymapUnref(_keymap);
			_keymap = 0;
		}
		if (_context != 0)
		{
			XkbNative.ContextUnref(_context);
			_context = 0;
		}
	}
}
