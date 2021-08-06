using System.Collections.Generic;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.Core.Internal
{
	internal static class KeyboardStateTracker
	{
		private static readonly Dictionary<VirtualKey, CoreVirtualKeyStates> _keyStates = new Dictionary<VirtualKey, CoreVirtualKeyStates>();

		internal static CoreVirtualKeyStates GetKeyState(VirtualKey key)
		{
			if (_keyStates.TryGetValue(key, out var state))
			{
				return state;
			}

			return CoreVirtualKeyStates.None;
		}

		internal static void OnKeyDown(VirtualKey key)
		{
			if (!_keyStates.ContainsKey(key))
			{
				_keyStates[key] = CoreVirtualKeyStates.None;
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
	}
}
