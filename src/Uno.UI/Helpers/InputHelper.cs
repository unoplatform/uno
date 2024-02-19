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

	private static string InputToUnicode(uint keyCode, uint scanCode)
	{
		var keyboardState = new byte[255];
		var keyboardStateStatus = GetKeyboardState(keyboardState);

		if (!keyboardStateStatus)
		{
			return "";
		}

		var inputLocaleIdentifier = GetKeyboardLayout((uint)Environment.CurrentManagedThreadId);

		var result = new StringBuilder();
		var conversionStatus = ToUnicodeEx(keyCode, scanCode, keyboardState, result, (int)5, (uint)0, inputLocaleIdentifier);
		if (conversionStatus <= 0 || (char.IsControl(result[0]) && result[0] != '\r'))
		{
			return "";
		}

		return result.ToString();
	}

	public static string WindowsKeyCodeToUnicode(uint keyCode) => InputToUnicode(keyCode, MapVirtualKey(keyCode, 0));
	// For a scancode table, see https://www.win.tue.nl/~aeb/linux/kbd/scancodes-10.html
	public static string WindowsScancodeToUnicode(uint scanCode) => InputToUnicode(MapVirtualKey(scanCode, 1), scanCode);

	public static bool TryConvertKeyCodeToScanCode(uint keyCode, out uint scanCode)
	{
		if (OperatingSystem.IsWindows())
		{
			scanCode = MapVirtualKey(keyCode, 0);
			return true;
		}

		// Uno TODO: get scan codes on other platforms
		scanCode = keyCode;
		return false;
	}
}
