using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

// Hand-written interop for the shell auto-hide taskbar query: CsWin32 cannot generate SHAppBarMessage /
// APPBARDATA for an AnyCPU assembly (PInvoke005), so they are declared manually here.
internal static class Win32AutoHideTaskbar
{
	public const uint ABE_LEFT = 0;
	public const uint ABE_TOP = 1;
	public const uint ABE_RIGHT = 2;
	public const uint ABE_BOTTOM = 3;

	private const uint ABM_GETAUTOHIDEBAREX = 0x0000000b;

	[StructLayout(LayoutKind.Sequential)]
	private struct APPBARDATA
	{
		public uint cbSize;
		public IntPtr hWnd;
		public uint uCallbackMessage;
		public uint uEdge;
		public RECT rc;
		public IntPtr lParam;
	}

	[DllImport("shell32.dll")]
	private static extern nuint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

	// Returns true when an auto-hide taskbar occupies the given edge of the monitor described by monitorRect.
	public static bool ExistsOnEdge(uint edge, RECT monitorRect)
	{
		var data = new APPBARDATA
		{
			cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
			uEdge = edge,
			rc = monitorRect
		};

		// ABM_GETAUTOHIDEBAREX returns the auto-hide appbar's HWND for that monitor edge, or null when none.
		return SHAppBarMessage(ABM_GETAUTOHIDEBAREX, ref data) != 0;
	}
}
