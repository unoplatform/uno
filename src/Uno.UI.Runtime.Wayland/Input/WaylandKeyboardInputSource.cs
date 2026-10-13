#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Linux;
using Microsoft.Win32.SafeHandles;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Wayland.Input;

internal sealed class WaylandKeyboardInputSource : IUnoKeyboardInputSource, IDisposable
{
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;
	// BMP text uses UnicodeKey; complete strings commit through the focused text session.
	event TypedEventHandler<object, CharacterReceivedEventArgs>? IUnoKeyboardInputSource.CharacterReceived { add { } remove { } }
	private readonly Action<Action> _dispatch;
	private readonly Action<bool> _activate;
	private readonly IWaylandInputConnection _connection;
	private readonly Dictionary<uint, PressedKey> _pressed = new();
	private WaylandKeyboardCompose? _compose;
	private nint _context, _keymap, _state;
	private Timer? _repeat;
	private uint _repeatKey;
	private int _generation, _rate = 25, _delay = 600;
	private bool _focused, _disposed, _levelThree;
	public VirtualKeyModifiers Modifiers
	{
		get; private set;
	}

	public WaylandKeyboardInputSource(IWaylandInputConnection connection, Action<Action> dispatch, Action<bool> activate)
	{
		_connection = connection;
		_dispatch = dispatch;
		_activate = activate;
	}

	public void Keymap(uint format, int fd, uint size)
	{
		StopRepeat();
		if (format != 1)
		{
			using var unused = new SafeFileHandle(fd, ownsHandle: true);
			ClearKeymap();
			return;
		}
		SetKeymap(WaylandXkb.ReadKeymap(fd, size));
	}

	private void SetKeymap(string text)
	{
		var context = WaylandXkb.ContextNew(0);
		nint keymap = 0, state = 0;
		WaylandKeyboardCompose? compose = null;
		try
		{
			if (context == 0)
			{
				throw new InvalidOperationException("Cannot create XKB context.");
			}
			keymap = WaylandXkb.KeymapNew(context, text, 1, 0);
			if (keymap == 0)
			{
				throw new InvalidOperationException("Cannot compile compositor keymap.");
			}
			state = WaylandXkb.StateNew(keymap);
			if (state == 0)
			{
				throw new InvalidOperationException("Cannot create XKB state.");
			}
			compose = new WaylandKeyboardCompose(context, GetComposeLocale());
			ClearKeymap();
			_context = context;
			_keymap = keymap;
			_state = state;
			_compose = compose;
		}
		catch
		{
			compose?.Dispose();
			if (state != 0)
			{
				WaylandXkb.StateUnref(state);
			}
			if (keymap != 0)
			{
				WaylandXkb.KeymapUnref(keymap);
			}
			if (context != 0)
			{
				WaylandXkb.ContextUnref(context);
			}
			throw;
		}
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

	public void UpdateModifiers(uint depressed, uint latched, uint locked, uint group)
	{
		if (_state == 0)
		{
			return;
		}
		// The compositor owns state: do not additionally call xkb_state_update_key.
		_ = WaylandXkb.UpdateMask(_state, depressed, latched, locked, 0, 0, group);
		Modifiers = VirtualKeyModifiers.None;
		_levelThree = WaylandXkb.ModActive(_state, "Mod5", 8) > 0;
		if (WaylandXkb.ModActive(_state, "Shift", 8) > 0)
		{
			Modifiers |= VirtualKeyModifiers.Shift;
		}
		if (WaylandXkb.ModActive(_state, "Control", 8) > 0)
		{
			Modifiers |= VirtualKeyModifiers.Control;
		}
		if (WaylandXkb.ModActive(_state, "Mod1", 8) > 0)
		{
			Modifiers |= VirtualKeyModifiers.Menu;
		}
		if (WaylandXkb.ModActive(_state, "Mod4", 8) > 0)
		{
			Modifiers |= VirtualKeyModifiers.Windows;
		}
	}

	public void Enter(ReadOnlySpan<uint> heldKeys)
	{
		_focused = true;
		_pressed.Clear();
		foreach (var key in heldKeys)
		{
			_pressed[key] = new(_state == 0 || key > uint.MaxValue - 8 ? VirtualKey.None : LinuxKeyMap.FromKeysym(WaylandXkb.GetSym(_state, key + 8)), null);
		}
		_dispatch(() => _activate(true));
	}
	public void Leave()
	{
		StopRepeat();
		_compose?.Reset();
		foreach (var key in _pressed)
		{
			if (!key.Value.CommittedText)
			{
				Emit(key.Key, key.Value.VirtualKey, false, false);
			}
		}
		_pressed.Clear();
		Modifiers = VirtualKeyModifiers.None;
		_levelThree = false;
		if (_focused)
		{
			_dispatch(() => _activate(false));
		}
		_focused = false;
	}
	public void RepeatInfo(int rate, int delay)
	{
		_rate = Math.Max(0, rate);
		_delay = Math.Max(0, delay);
		StopRepeat();
	}
	public void Key(uint key, uint state)
	{
		if (!_focused || _disposed || key > uint.MaxValue - 8)
		{
			return;
		}
		var down = state == 1;
		if (state == 2)
		{
			if (_rate == 0 && _pressed.TryGetValue(key, out var repeated))
			{
				EmitRepeat(key, repeated);
			}
			return;
		}
		if (state > 2)
		{
			return;
		}
		if (down)
		{
			if (_state == 0)
			{
				return;
			}
			if (_pressed.TryGetValue(key, out var pressed))
			{
				EmitRepeat(key, pressed);
				return;
			}
			var sym = WaylandXkb.GetSym(_state, key + 8);
			var vk = LinuxKeyMap.FromKeysym(sym);
			var keyText = WaylandXkb.GetUtf32(_state, key + 8);
			uint? composed = null;
			string? composedString = null;
			if (_compose is { } compose)
			{
				// Like keys taken by an input method, keys consumed by a dead-key or Compose sequence raise no key events.
				var fed = compose.Feed(sym, keyText, out var completeText);
				if (fed is null)
				{
					return;
				}
				composed = fed != keyText ? fed : null;
				composedString = completeText is { Length: > 1 } ? completeText : null;
			}
			var committedText = !Emit(key, vk, true, false, composed ?? keyText, composedString);
			_pressed[key] = new(vk, committedText ? composed ?? keyText : composed, composedString, committedText);
			if (_rate > 0 && WaylandXkb.Repeats(_keymap, key + 8) != 0)
			{
				StopRepeat();
				_repeatKey = key;
				var generation = _generation;
				_repeat = new Timer(_ => _connection.Post(() =>
				{
					if (!_disposed && _focused && generation == _generation && _pressed.TryGetValue(key, out var repeated))
					{
						EmitRepeat(key, repeated);
					}
				}), null, _delay, Math.Max(1, 1000 / _rate));
			}
		}
		else if (_pressed.Remove(key, out var released))
		{
			if (key == _repeatKey)
			{
				StopRepeat();
			}
			if (!released.CommittedText)
			{
				Emit(key, released.VirtualKey, false, true);
			}
		}
	}
	private void EmitRepeat(uint key, PressedKey pressed)
		=> Emit(key, pressed.VirtualKey, true, true, pressed.ComposedText, pressed.ComposedString, pressed.CommittedText);

	// Returns false when the press committed a complete string instead of raising KeyDown.
	private bool Emit(uint key, VirtualKey vk, bool down, bool previous, uint? text = null, string? composedString = null, bool? committedText = null)
	{
		var unicode = text ?? (down && _state != 0 ? WaylandXkb.GetUtf32(_state, key + 8) : 0);
		char? character = unicode is >= 32 and <= 0xffff && unicode is not (>= 0xd800 and <= 0xdfff) || unicode == 13 ? (char)unicode : null;
		var completeText = composedString ?? (unicode is > 0xffff and <= 0x10ffff ? char.ConvertFromUtf32((int)unicode) : null);
		if (!_levelThree && (Modifiers & (VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu | VirtualKeyModifiers.Windows)) != 0)
		{
			character = null;
			completeText = null;
		}
		if (committedText == true && completeText is null)
		{
			return false;
		}
		if (down && committedText != false && completeText is not null)
		{
			// Like input method text, complete strings raise no key events: a KeyDown without UnicodeKey would still
			// insert the key's own character. Without an input method, the composition callbacks insert the string
			// once, replacing the selection.
			_dispatch(() =>
			{
				if (ImeSessionCoordinator.ActiveHost is { CanAcceptTextInput: true } host
					&& host.XamlRoot is { } root && XamlRootMap.GetHostForRoot(root) is WaylandXamlRootHost rootHost
					&& rootHost.KeyboardSource == this)
				{
					CommitText(host, completeText);
				}
			});
			return false;
		}
		var args = new KeyEventArgs("keyboard", vk, Modifiers, new CorePhysicalKeyStatus
		{
			ScanCode = key,
			RepeatCount = 1,
			WasKeyDown = previous,
			IsKeyReleased = !down,
			IsMenuKeyDown = (Modifiers & VirtualKeyModifiers.Menu) != 0
		}, character);
		_dispatch(() =>
		{
			if (down)
			{
				KeyDown?.Invoke(this, args);
			}
			else
			{
				KeyUp?.Invoke(this, args);
			}
		});
		return true;
	}
	private static void CommitText(IImeSessionHost host, string text)
	{
		if (!host.IsComposing)
		{
			host.OnImeCompositionStarted();
		}
		host.OnImeCompositionCompleted(text, textAlreadyApplied: false);
		host.OnImeCompositionEnded();
	}

	private void StopRepeat()
	{
		_generation++;
		_repeat?.Dispose();
		_repeat = null;
	}
	private void ClearKeymap()
	{
		_compose?.Dispose();
		_compose = null;
		if (_state != 0)
		{
			WaylandXkb.StateUnref(_state);
		}
		if (_keymap != 0)
		{
			WaylandXkb.KeymapUnref(_keymap);
		}
		if (_context != 0)
		{
			WaylandXkb.ContextUnref(_context);
		}
		_context = _keymap = _state = 0;
		_levelThree = false;
		Modifiers = VirtualKeyModifiers.None;
	}
	// Dispatch thread.
	internal void ResetCompose()
	{
		if (!_disposed)
		{
			_compose?.Reset();
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		Leave();
		_disposed = true;
		ClearKeymap();
	}

	// Composed text is kept so repeats of a Compose sequence's final key repeat its result.
	private readonly record struct PressedKey(VirtualKey VirtualKey, uint? ComposedText, string? ComposedString = null, bool CommittedText = false);
}
