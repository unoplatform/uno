#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Linux;
using Uno.UI.Runtime.Linux.Input;
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
	private readonly Action<string> _commitText;
	private readonly IWaylandInputConnection _connection;
	private readonly Dictionary<uint, PressedKey> _pressed = new();
	private readonly XkbKeyRepeat _repeat;
	private XkbKeyboard? _xkb;
	private bool _focused, _disposed, _levelThree;
	public VirtualKeyModifiers Modifiers
	{
		get; private set;
	}

	public WaylandKeyboardInputSource(IWaylandInputConnection connection, Action<Action> dispatch, Action<bool> activate, Action<string>? commitText = null)
	{
		_connection = connection;
		_dispatch = dispatch;
		_activate = activate;
		_commitText = commitText ?? CommitFocusedText;
		_repeat = new XkbKeyRepeat(OnRepeat);
	}

	// Timer thread.
	private void OnRepeat(uint key) => _connection.Send(() =>
	{
		if (!_disposed && _focused && _repeat.Key == key && _pressed.TryGetValue(key, out var repeated))
		{
			EmitRepeat(key, repeated);
		}
	});

	public void Keymap(uint format, int fd, uint size)
	{
		_repeat.Stop();
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
		var xkb = XkbKeyboard.FromString(text);
		ClearKeymap();
		_xkb = xkb;
	}

	public void UpdateModifiers(uint depressed, uint latched, uint locked, uint group)
	{
		if (_xkb is not { } xkb)
		{
			return;
		}
		// The compositor owns the state: keys are not fed to it as well.
		xkb.UpdateMask(depressed, latched, locked, group);
		Modifiers = xkb.Modifiers;
		_levelThree = xkb.LevelThree;
	}

	public void Enter(ReadOnlySpan<uint> heldKeys)
	{
		_focused = true;
		_pressed.Clear();
		foreach (var key in heldKeys)
		{
			_pressed[key] = new(_xkb is null || key > uint.MaxValue - 8 ? VirtualKey.None : _xkb.GetVirtualKey(key), null);
		}
		_dispatch(() => _activate(true));
	}
	public void Leave()
	{
		_repeat.Stop();
		_xkb?.ResetCompose();
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
		_repeat.Rate = Math.Max(0, rate);
		_repeat.Delay = Math.Max(0, delay);
		_repeat.Stop();
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
			if (_repeat.Rate == 0 && _pressed.TryGetValue(key, out var repeated))
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
			if (_xkb is not { } xkb)
			{
				return;
			}
			if (_pressed.TryGetValue(key, out var pressed))
			{
				EmitRepeat(key, pressed);
				return;
			}
			var sym = xkb.GetKeysym(key);
			var vk = LinuxKeyMap.FromKeysym(sym);
			var keyText = xkb.GetUtf32(key);
			// Like keys taken by an input method, keys consumed by a dead-key or Compose sequence raise no key events.
			var fed = xkb.Compose(sym, keyText, out var completeText);
			if (fed is null)
			{
				return;
			}
			uint? composed = fed != keyText ? fed : null;
			var composedString = completeText is { Length: > 1 } ? completeText : null;
			var committedText = !Emit(key, vk, true, false, composed ?? keyText, composedString);
			_pressed[key] = new(vk, committedText ? composed ?? keyText : composed, composedString, committedText);
			if (xkb.Repeats(key))
			{
				_repeat.Start(key);
			}
		}
		else if (_pressed.Remove(key, out var released))
		{
			if (key == _repeat.Key)
			{
				_repeat.Stop();
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
		var unicode = text ?? (down && _xkb is { } xkb ? xkb.GetUtf32(key) : 0);
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
			_dispatch(() => _commitText(completeText));
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

	private void CommitFocusedText(string text)
	{
		if (ImeSessionCoordinator.ActiveHost is { CanAcceptTextInput: true } host
			&& host.XamlRoot is { } root && XamlRootMap.GetHostForRoot(root) is WaylandXamlRootHost rootHost
			&& rootHost.KeyboardSource == this
			&& (ImeSessionCoordinator.GetExtension(host) is not WaylandImeTextBoxExtension ime || !ime.TryCommitKeyboardText(host, text)))
		{
			CommitText(host, text);
		}
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

	private void ClearKeymap()
	{
		_xkb?.Dispose();
		_xkb = null;
		_levelThree = false;
		Modifiers = VirtualKeyModifiers.None;
	}
	// Dispatch thread.
	internal void ResetCompose()
	{
		if (!_disposed)
		{
			_xkb?.ResetCompose();
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
		_repeat.Dispose();
		ClearKeymap();
	}

	// Composed text is kept so repeats of a Compose sequence's final key repeat its result.
	private readonly record struct PressedKey(VirtualKey VirtualKey, uint? ComposedText, string? ComposedString = null, bool CommittedText = false);
}
