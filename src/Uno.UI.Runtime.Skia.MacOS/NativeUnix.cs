using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.MacOS;

internal static partial class NativeUnix
{

	[LibraryImport("/usr/lib/libSystem.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial /* void* */ nint dlopen(/* const char* */ string path, int mode);

	[LibraryImport("/usr/lib/libSystem.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial IntPtr dlsym(IntPtr handle, string symbol);
}
