#nullable enable

using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.MacOS;

internal static partial class NativeSkia
{
	[LibraryImport("libSkiaSharp")]
	[UnmanagedCallConv(CallConvs = new System.Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
	internal static partial nint gr_direct_context_make_metal(nint device, nint queue);

	[LibraryImport("libSkiaSharp")]
	[UnmanagedCallConv(CallConvs = new System.Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
	internal static unsafe partial nint gr_backendrendertarget_new_metal(int width, int height, int sampleCount, GRMtlTextureInfoNative* mtlInfo);
}

internal struct GRMtlTextureInfoNative
{
	public nint Texture;
}
