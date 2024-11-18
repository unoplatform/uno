using System;
using System.Runtime.InteropServices;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.Debug;
using Uno.Disposables;

namespace Uno.UI.Runtime.Skia.Win32;

internal static class Win32Helper
{
	public static unsafe string GetErrorMessage()
	{
		IntPtr* messagePtr = stackalloc IntPtr[1];
		var messageLength = PInvoke.FormatMessage(
			FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_IGNORE_INSERTS,
			default,
			(uint)Marshal.GetLastWin32Error(),
			0,
			new PWSTR((char*)messagePtr),
			0);
		var message = *messagePtr;
		using var messageDisposable = new DisposableStruct<IntPtr>(static m => PInvoke.LocalFree(new HLOCAL(m.ToPointer())), message);
		return Marshal.PtrToStringUni(message, (int)messageLength);
	}

	public static VirtualKeyModifiers GetKeyModifiers()
	{
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

		return modifiers;
	}
}
