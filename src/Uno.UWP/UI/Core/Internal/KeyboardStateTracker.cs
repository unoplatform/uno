using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.Core;

/// <summary>
/// Tracks keyboard key state.
/// </summary>
/// <remarks>
///	The behavior is based on description in https://docs.microsoft.com/en-us/uwp/api/windows.ui.core.corevirtualkeystates.
///	In UWP/WinUI, every key has a locked state (not only Caps Lock, etc.). The sequence of states is as follows:
///	(None) -> (Down) -> (None) -> (Down + Locked) -> (None + Locked) -> (Down) -> (None) -> etc.
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

	internal static void OnKeyDown(VirtualKey key)
	{
		if (!_keyStates.ContainsKey(key))
		{
			// The first key press should not cause Locked state.
			_keyStates[key] = CoreVirtualKeyStates.Down;
		}

		if (!_keyStates[key].HasFlag(CoreVirtualKeyStates.Locked))
		{
			_keyStates[key] = CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked;
		}
		else
		{
			_keyStates[key] = CoreVirtualKeyStates.Down;
		}

		SetStateOnNonSideKeys(key);
	}

	internal static void OnKeyUp(VirtualKey key)
	{
		if (!_keyStates.ContainsKey(key))
		{
			// Edge case - key is released without previous press.
			_keyStates[key] = CoreVirtualKeyStates.None;
		}

		if (_keyStates[key].HasFlag(CoreVirtualKeyStates.Locked))
		{
			_keyStates[key] = CoreVirtualKeyStates.None | CoreVirtualKeyStates.Locked;
		}
		else
		{
			_keyStates[key] = CoreVirtualKeyStates.None;
		}

		SetStateOnNonSideKeys(key);
	}

	private static void SetStateOnNonSideKeys(VirtualKey key)
	{
		if (key == VirtualKey.LeftShift || key == VirtualKey.RightShift)
		{
			_keyStates[VirtualKey.Shift] = _keyStates[key];
		}

		if (key == VirtualKey.LeftControl || key == VirtualKey.RightControl)
		{
			_keyStates[VirtualKey.Control] = _keyStates[key];
		}

		if (key == VirtualKey.LeftMenu || key == VirtualKey.RightMenu)
		{
			_keyStates[VirtualKey.Menu] = _keyStates[key];
		}
	}

	internal static void Reset()
	{
		// Clearing state when debugger is attached would
		// make it hard to debug key events.
		if (!System.Diagnostics.Debugger.IsAttached)
		{
			_keyStates.Clear();
		}
	}

#if __WASM__
#pragma warning disable IDE0051 // Remove unused private members
	[JSExport]
	private static void UpdateKeyStateNative(string key, bool down)
#pragma warning restore IDE0051 // Remove unused private members
	{
		if (down)
		{
			OnKeyDown(VirtualKeyHelper.FromKey(key));
		}
		else
		{
			OnKeyUp(VirtualKeyHelper.FromKey(key));
		}
	}
#endif
}
