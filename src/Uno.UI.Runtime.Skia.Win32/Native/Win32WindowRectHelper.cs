using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

internal static class Win32WindowRectHelper
{
	internal static string GetWindowRectSnapshot(HWND hwnd)
	{
		if (hwnd == HWND.Null)
		{
			return "null";
		}

		if (PInvoke.GetWindowRect(hwnd, out var rect))
		{
			return $"{rect.left},{rect.top},{rect.right - rect.left}x{rect.bottom - rect.top}";
		}

		return $"error={Marshal.GetLastWin32Error()}";
	}
}
