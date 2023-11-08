using System;
using System.Runtime.InteropServices;
using System.Text;
namespace Uno.UI.Helpers;

internal static class InputHelper
{
	[DllImport("user32.dll")]
	private static extern bool GetKeyboardState(byte[] lpKeyState);

	[DllImport("user32.dll")]
	private static extern uint MapVirtualKey(uint uCode, uint uMapType);

	[DllImport("user32.dll")]
	private static extern IntPtr GetKeyboardLayout(uint idThread);

	[DllImport("user32.dll")]
	private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

	public static string WindowsKeyCodeToUnicode(uint keyCode)
	{
		var keyboardState = new byte[255];
		var keyboardStateStatus = GetKeyboardState(keyboardState);

		if (!keyboardStateStatus)
		{
			return "";
		}

		var scanCode = MapVirtualKey(keyCode, 0);
		var inputLocaleIdentifier = GetKeyboardLayout((uint)Environment.CurrentManagedThreadId);

		var result = new StringBuilder();
		var conversionStatus = ToUnicodeEx(keyCode, scanCode, keyboardState, result, (int)5, (uint)0, inputLocaleIdentifier);
		if (conversionStatus <= 0 || char.IsControl(result[0]))
		{
			return "";
		}

		return result.ToString();
	}
}
