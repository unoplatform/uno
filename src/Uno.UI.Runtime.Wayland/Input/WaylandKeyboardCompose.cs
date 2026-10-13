#nullable enable

using System;
using System.Text;

namespace Uno.UI.Runtime.Wayland.Input;

// Dead-key and Compose sequences through libxkbcommon, for text not composed by a text-input-v3 input method
// (one consumes the keys of sequences it composes, so they never reach wl_keyboard).
internal sealed class WaylandKeyboardCompose : IDisposable
{
	private nint _state;

	internal WaylandKeyboardCompose(nint context, string locale)
	{
		var table = WaylandXkb.ComposeTableNew(context, locale, 0);
		if (table != 0)
		{
			try
			{
				_state = WaylandXkb.ComposeStateNew(table, 0);
			}
			finally
			{
				WaylandXkb.ComposeTableUnref(table);
			}
		}
	}

	// Pending and cancelled sequences consume the key; completed sequences also expose their full text.
	internal uint? Feed(uint keysym, uint unicode) => Feed(keysym, unicode, out _);

	internal uint? Feed(uint keysym, uint unicode, out string? composedText)
	{
		composedText = null;
		if (_state == 0 || WaylandXkb.ComposeFeed(_state, keysym) == 0) // XKB_COMPOSE_FEED_IGNORED: modifiers
		{
			return unicode;
		}
		switch (WaylandXkb.ComposeStatus(_state))
		{
			case 1: // XKB_COMPOSE_COMPOSING
				return null;
			case 2: // XKB_COMPOSE_COMPOSED
				var length = WaylandXkb.ComposeGetUtf8(_state, [], 0);
				var buffer = new byte[checked(length + 1)];
				WaylandXkb.ComposeGetUtf8(_state, buffer, (nuint)buffer.Length);
				var text = Encoding.UTF8.GetString(buffer, 0, length);
				Reset();
				if (text.Length == 0)
				{
					return null;
				}
				// Text that does not fit KeyEventArgs.UnicodeKey is only returned as a string.
				composedText = text;
				return text.Length == 1 ? text[0] : 0u;
			case 3: // XKB_COMPOSE_CANCELLED
				Reset();
				return null;
			default:
				return unicode;
		}
	}

	internal void Reset()
	{
		if (_state != 0)
		{
			WaylandXkb.ComposeReset(_state);
		}
	}

	public void Dispose()
	{
		if (_state != 0)
		{
			WaylandXkb.ComposeStateUnref(_state);
			_state = 0;
		}
	}
}
