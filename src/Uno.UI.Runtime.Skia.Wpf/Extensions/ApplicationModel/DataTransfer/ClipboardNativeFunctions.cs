#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Uno.Extensions.ApplicationModel.DataTransfer
{
	internal static class ClipboardNativeFunctions
	{
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AddClipboardFormatListener(IntPtr hwnd);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
	}
}
