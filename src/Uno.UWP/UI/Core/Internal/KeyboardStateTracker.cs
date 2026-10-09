#nullable enable

using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.Core;

/// <summary>
/// Tracks keyboard key state.
/// </summary>
/// <remarks>
/// Only the <see cref="CoreVirtualKeyStates.Down"/> flag is tracked. WinUI also exposes
/// <see cref="CoreVirtualKeyStates.Locked"/>, which mirrors the Win32 GetKeyState "toggled" bit
/// (a parity counter the OS maintains per key). It is deliberately not emulated here: deriving it
/// from the key events we receive requires each physical press to produce exactly one update, which
/// no platform guarantees (routed events are raised once per element while bubbling, OS auto-repeat
/// produces extra key downs, and a key released while the app is not focused produces none). For the
/// only key where the flag carries a meaning an app would consult - Caps Lock - a press counter is
/// the wrong source anyway, since the lock can be toggled before the app starts or while it is in
/// the background. Reporting it correctly requires querying the platform instead.
/// </remarks>
internal static partial class KeyboardStateTracker
{
	private static readonly Dictionary<VirtualKey, CoreVirtualKeyStates> _keyStates = new Dictionary<VirtualKey, CoreVirtualKeyStates>();

	/// <summary>
	/// Retrieves the current state for a given key.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <returns>Key state.</returns>
	internal static CoreVirtualKeyStates GetKeyState(VirtualKey key)
	{
		if (_keyStates.TryGetValue(key, out var state))
		{
			return state;
		}

		return CoreVirtualKeyStates.None;
	}

	/// <remarks>
	/// Currently this uses the same implementation as GetKeyState, 
	/// but kept separate to be able to differentiate between original calls
	/// to CoreWindow.GetKeyState and CoreWindow.GetAsyncKeyState.
	/// </remarks>
	internal static CoreVirtualKeyStates GetAsyncKeyState(VirtualKey key) => GetKeyState(key);

	internal static void OnKeyDown(VirtualKey key) => SetState(key, CoreVirtualKeyStates.Down);

	internal static void OnKeyUp(VirtualKey key) => SetState(key, CoreVirtualKeyStates.None);

	/// <summary>
	/// Forces the tracked state of a modifier key to a value obtained independently of the
	/// key down/key up pairing that <see cref="OnKeyDown"/>/<see cref="OnKeyUp"/> rely on.
	/// </summary>
	/// <remarks>
	/// A key up can be lost entirely - the browser steals focus mid-keystroke, or a mobile on-screen
	/// keyboard raises Shift during auto-capitalization without ever releasing it - which leaves the
	/// key reported as down for the rest of the session. Input events that carry the real state of the
	/// modifiers alongside their own payload (key events, and pointer events on some platforms) can
	/// call this to repair that drift. The correction is silent by design: the moment the key was
	/// actually released is unknown, so raising a key up here would misreport when it happened.
	/// </remarks>
	internal static void SyncModifierState(VirtualKey key, bool isDown)
	{
		var state = isDown ? CoreVirtualKeyStates.Down : CoreVirtualKeyStates.None;

		if (GetKeyState(key) == state)
		{
			return;
		}

		SetState(key, state);
	}

	private static void SetState(VirtualKey key, CoreVirtualKeyStates state)
	{
		_keyStates[key] = state;

		SetStateOnNonSideKeys(key, state);
		SetStateOnSideKeys(key, state);
	}

	private static void SetStateOnNonSideKeys(VirtualKey key, CoreVirtualKeyStates state)
	{
		if (key == VirtualKey.LeftShift || key == VirtualKey.RightShift)
		{
			_keyStates[VirtualKey.Shift] = state;
		}

		if (key == VirtualKey.LeftControl || key == VirtualKey.RightControl)
		{
			_keyStates[VirtualKey.Control] = state;
		}

		if (key == VirtualKey.LeftMenu || key == VirtualKey.RightMenu)
		{
			_keyStates[VirtualKey.Menu] = state;
		}
	}

	// Platforms disagree on which entry a modifier key press writes: X11 reports the side key and
	// lets SetStateOnNonSideKeys derive the combined one, while the browser reports the combined key
	// directly. Releasing the combined key therefore has to clear both sides, or a side key reported
	// by one platform would stay down forever once the other path corrected the combined one.
	private static void SetStateOnSideKeys(VirtualKey key, CoreVirtualKeyStates state)
	{
		if (state != CoreVirtualKeyStates.None)
		{
			// A press cannot be attributed to a side, so only releases propagate.
			return;
		}

		switch (key)
		{
			case VirtualKey.Shift:
				_keyStates[VirtualKey.LeftShift] = state;
				_keyStates[VirtualKey.RightShift] = state;
				break;
			case VirtualKey.Control:
				_keyStates[VirtualKey.LeftControl] = state;
				_keyStates[VirtualKey.RightControl] = state;
				break;
			case VirtualKey.Menu:
				_keyStates[VirtualKey.LeftMenu] = state;
				_keyStates[VirtualKey.RightMenu] = state;
				break;
		}
	}

	internal static void Reset() => _keyStates.Clear();

#pragma warning disable IDE0051 // Remove unused private members
	[JSExport]
	private static void UpdateKeyStateNative(string key, bool down)
#pragma warning restore IDE0051 // Remove unused private members
	{
		if (down)
		{
			OnKeyDown(BrowserVirtualKeyHelper.FromKey(key));
		}
		else
		{
			OnKeyUp(BrowserVirtualKeyHelper.FromKey(key));
		}
	}
}
