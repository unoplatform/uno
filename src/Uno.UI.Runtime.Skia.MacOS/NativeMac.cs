#nullable enable

using System;
using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.MacOS;

internal struct CGSize {
    public /* CGFloat */ double Width;
	public /* CGFloat */ double Height;

	public CGSize(double width, double height)
	{
		Width = width;
		Height = height;
	}
}

internal static partial class NativeMac
{
	[LibraryImport("/usr/lib/libc.dylib")]
	internal static unsafe partial void dispatch_async_f(/* dispatch_queue_t */ IntPtr queue, /* void */ IntPtr context, /* dispatch_function_t */ delegate* unmanaged<IntPtr, void> work);

	[LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
	internal static partial int NSApplicationMain(int argc, nint argv);
}
