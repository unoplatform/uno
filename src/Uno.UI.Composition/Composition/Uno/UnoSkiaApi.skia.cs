using System;
using System.Runtime.InteropServices;

namespace SkiaSharp;

internal static class UnoSkiaApi
{
	private const string SKIA = "libSkiaSharp";

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	unsafe internal static extern void sk_textblob_builder_alloc_run_pos(IntPtr builder, IntPtr font, int count, SKRect* bounds, UnoSKRunBufferInternal* runbuffer);

	/// <summary>
	/// We use this instead of the equivalent SKRoundRect.SetRectRadii because it takes an array with a
	/// length of _exactly_ 4. If we rent the SKPoint array to reduce allocations, we're not guaranteed to
	/// get the exact length we need.
	/// </summary>
	[DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
	unsafe internal static extern void sk_rrect_set_rect_radii(IntPtr rrect, SKRect* rect, SKPoint* radii);
}
