using System;
using System.Runtime.InteropServices;
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
			0,
			default);
		var message = *messagePtr;
		using var messageDisposable = new DisposableStruct<IntPtr>(static m => PInvoke.LocalFree(new HLOCAL(m.ToPointer())), message);
		return Marshal.PtrToStringUni(message, (int)messageLength);
	}
}
