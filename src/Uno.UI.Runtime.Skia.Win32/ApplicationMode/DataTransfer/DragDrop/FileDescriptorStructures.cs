using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// File descriptor structure for virtual files (used by Outlook and other applications)
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct FILEDESCRIPTORW
{
	public uint dwFlags;
	public System.Guid clsid;
	public System.Drawing.Size sizel;
	public System.Drawing.Point pointl;
	public uint dwFileAttributes;
	public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
	public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
	public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
	public uint nFileSizeHigh;
	public uint nFileSizeLow;
	public fixed char cFileName[260]; // MAX_PATH
}

/// <summary>
/// File group descriptor structure for virtual files
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FILEGROUPDESCRIPTORW
{
	public uint cItems;
	public FILEDESCRIPTORW fgd; // First element, access others via pointer arithmetic
}
