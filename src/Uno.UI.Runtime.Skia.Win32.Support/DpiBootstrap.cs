﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.Debug;
using Windows.Win32.UI.HiDpi;
using Uno.Foundation.Logging;

#pragma warning disable CA2255

internal static class DpiBootstrap
{
	[ModuleInitializer]
	internal static void Init()
	{
		// This call ensures that the application is properly marked as per-monitor DPI aware (PerMonitorV2).
		// When launched via `dotnet.exe` (e.g., `dotnet MyApp.dll` or `dotnet run`), the host process is the .NET CLI runtime,
		// which does NOT contain a DPI-awareness declaration in its embedded application manifest (RT_MANIFEST).
		// Consequently, Windows assumes the process is DPI-unaware, applies bitmap scaling, and reports a logical DPI of 96,
		// leading to incorrect pointer coordinates, layout glitches, and drag-and-drop offsets.
		//
		// In contrast, when launching a native application executable (e.g., `MyApp.exe`), the process inherits the
		// DPI-awareness setting defined in the manifest block:
		//
		// <assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0" xmlns:asmv3="urn:schemas-microsoft-com:asm.v3">
		//   <asmv3:application>
		//     <asmv3:windowsSettings>
		//       <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
		//     </asmv3:windowsSettings>
		//   </asmv3:application>
		// </assembly>
		//
		// More details on DPI Awareness contexts and behaviors:
		// - https://learn.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process
		//
		// This call must be made BEFORE any window is initialized.

		var success = PInvoke.SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
		if (!success) { typeof(DpiBootstrap).LogError()?.Error($"{nameof(PInvoke.AddClipboardFormatListener)} failed: {GetErrorMessage()}"); }
	}

	private static string GetErrorMessage() => GetErrorMessage((uint)Marshal.GetLastWin32Error());

	private static unsafe string GetErrorMessage(uint errorCode)
	{
		IntPtr* messagePtr = stackalloc IntPtr[1];
		var messageLength = PInvoke.FormatMessage(
			FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_IGNORE_INSERTS,
			default,
			errorCode,
			0,
			new PWSTR((char*)messagePtr),
			0);
		var message = *messagePtr;
		try
		{
			return Marshal.PtrToStringUni(message, (int)messageLength);
		}
		finally
		{
			PInvoke.LocalFree(new HLOCAL(message.ToPointer()));
		}
	}
}
