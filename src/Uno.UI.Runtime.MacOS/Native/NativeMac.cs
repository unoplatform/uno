using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.MacOS;

internal static partial class NativeMac
{
	[LibraryImport("/usr/lib/libc.dylib")]
	internal static unsafe partial void dispatch_async_f(/* dispatch_queue_t */ IntPtr queue, /* void */ IntPtr context, /* dispatch_function_t */ delegate* unmanaged<IntPtr, void> work);

	[LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
	internal static partial int NSApplicationMain(int argc, nint argv);

	[LibraryImport("/usr/lib/libobjc.A.dylib")]
	internal static partial IntPtr objc_autoreleasePoolPush();

	[LibraryImport("/usr/lib/libobjc.A.dylib")]
	internal static partial void objc_autoreleasePoolPop(IntPtr pool);
}
