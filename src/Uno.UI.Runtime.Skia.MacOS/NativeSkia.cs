using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.MacOS;

internal static partial class NativeSkia
{
	[LibraryImport("libSkiaSharp")]
	[UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
	internal static partial nint gr_direct_context_make_metal(nint device, nint queue);
}
