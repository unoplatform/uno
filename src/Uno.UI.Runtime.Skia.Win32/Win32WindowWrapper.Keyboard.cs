using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper : IUnoKeyboardInputSource
{
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;

	private void OnKey(WPARAM wParam, LPARAM lParam, bool down)
	{
		var key = (VirtualKey)wParam.Value;

		var modifiers = VirtualKeyModifiers.None;
		if (PInvoke.GetKeyState((int)VirtualKey.LeftMenu) < 0 || PInvoke.GetKeyState((int)VirtualKey.RightMenu) < 0 ||
			PInvoke.GetKeyState((int)VirtualKey.Menu) < 0)
		{
			modifiers |= VirtualKeyModifiers.Menu;
		}

		if (PInvoke.GetKeyState((int)VirtualKey.LeftControl) < 0 ||
			PInvoke.GetKeyState((int)VirtualKey.RightControl) < 0 || PInvoke.GetKeyState((int)VirtualKey.Control) < 0)
		{
			modifiers |= VirtualKeyModifiers.Control;
		}

		if (PInvoke.GetKeyState((int)VirtualKey.LeftShift) < 0 || PInvoke.GetKeyState((int)VirtualKey.RightShift) < 0 ||
			PInvoke.GetKeyState((int)VirtualKey.Shift) < 0)
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}

		if (PInvoke.GetKeyState((int)VirtualKey.LeftWindows) < 0 ||
			PInvoke.GetKeyState((int)VirtualKey.RightWindows) < 0)
		{
			modifiers |= VirtualKeyModifiers.Windows;
		}

		// If a key press results in a "character" input, it gets sent separately as a WM_CHAR message,
		// so we check for the next message in the queue to see if it's WM_CHAR, and if so, grab the char.
		char? unicodeKey = null;
		if (PInvoke.PeekMessage(out var msg, _hwnd, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE) != 0)
		{
			if (msg.message == PInvoke.WM_CHAR)
			{
				PInvoke.PeekMessage(out _, _hwnd, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE);
				if (key != VirtualKey.Tab)
				{
					// We don't treat Tab as a character key. For example, tabbing in a TextBox doesn't insert a '\t'
					unicodeKey = (char)msg.wParam;
				}
			}
		}

		var args = new KeyEventArgs(
			"keyboard",
			key,
			modifiers,
			new CorePhysicalKeyStatus { ScanCode = (uint)((lParam.Value & 0x00FF0000) >> 16), RepeatCount = 1, },
			unicodeKey);

		if (down)
		{
			KeyDown?.Invoke(this, args);
		}
		else
		{
			KeyUp?.Invoke(this, args);
		}
	}
}
