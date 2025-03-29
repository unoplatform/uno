#if UNO_REFERENCE_API
using System;
namespace Uno.UI.Runtime.Skia;

public sealed class Win32NativeWindow
{
	public IntPtr Hwnd { get; }

	public Win32NativeWindow(IntPtr hwnd)
	{
		Hwnd = hwnd;
	}
}
#endif
