using System;
using System.Runtime.InteropServices;

namespace Uno.UI.RuntimeTests.Skia.WPF.Helpers
{
	internal class NativeMethods
	{
		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool SendMessage(IntPtr hWnd, uint Msg, uint wParam, IntPtr lParam);

		const uint WM_CHAR = 0x0102;
		const uint WM_KEYDOWN = 0x0100;
		const uint WM_KEYUP = 0x0101;
		const uint VK_RETURN = 0x0D;

		public static void SendKey(IntPtr hWnd, char key)
		{
			var val = (uint)key;
			SendMessage(hWnd, WM_KEYDOWN, val, IntPtr.Zero);
			SendMessage(hWnd, WM_CHAR, val, IntPtr.Zero);
			SendMessage(hWnd, WM_KEYUP, val, IntPtr.Zero);
		}
	}
}
