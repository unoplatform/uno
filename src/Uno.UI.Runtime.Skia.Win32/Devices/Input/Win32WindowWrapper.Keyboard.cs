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

		var modifiers = Win32Helper.GetKeyModifiers();

		// If a key press results in a "character" input, it gets sent separately as a WM_CHAR message right after,
		// so we check for the next message in the queue to see if it's WM_CHAR, and if so, grab the char.
		// This is not laid out in the docs and is technically an implementation detail that might change in the
		// future, but since win32 is ancient at this point, this is rather unlikely.
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
