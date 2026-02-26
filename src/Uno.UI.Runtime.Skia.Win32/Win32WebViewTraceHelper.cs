using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

internal static class Win32WebViewTraceHelper
{
	internal static bool IsVerboseWin32WebViewTraceEnabled()
	{
		var value = Environment.GetEnvironmentVariable("UNO_WIN32_WEBVIEW2_TRACE");
		return string.Equals(value, "1", StringComparison.Ordinal) || (bool.TryParse(value, out var enabled) && enabled);
	}

	internal static void LogVerboseTrace(
		bool isVerboseTraceEnabled,
		bool loggerEnabled,
		Action<string>? logWarning,
		Func<string> messageFactory)
	{
		if (!isVerboseTraceEnabled)
		{
			return;
		}

		var debugOutputEnabled = Debugger.IsAttached;
		if (!loggerEnabled && !debugOutputEnabled)
		{
			return;
		}

		var message = $"[WebView2Trace] {DateTime.UtcNow:O} {messageFactory()}";
		if (loggerEnabled)
		{
			logWarning?.Invoke(message);
		}

		if (debugOutputEnabled)
		{
			Debug.WriteLine(message);
		}
	}

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
