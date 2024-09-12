using System;
using System.Runtime.InteropServices;

namespace SkiaSharp;

internal static class UnoSkiaApi
{
	private const string SKIA = "libSkiaSharp";
	private const string SKIA_Apple = "@rpath/libSkiaSharp.framework/libSkiaSharp";

	[DllImport(SKIA_Apple, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(sk_textblob_builder_alloc_run_pos))]
	unsafe internal static extern void sk_textblob_builder_alloc_run_pos_apple(IntPtr builder, IntPtr font, int count, SKRect* bounds, UnoSKRunBufferInternal* runbuffer);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(sk_textblob_builder_alloc_run_pos))]
	unsafe internal static extern void sk_textblob_builder_alloc_run_pos_others(IntPtr builder, IntPtr font, int count, SKRect* bounds, UnoSKRunBufferInternal* runbuffer);

	internal unsafe static void sk_textblob_builder_alloc_run_pos(IntPtr builder, IntPtr font, int count, SKRect* bounds, UnoSKRunBufferInternal* runbuffer)
	{
		if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
		{
			sk_textblob_builder_alloc_run_pos_apple(builder, font, count, bounds, runbuffer);
		}
		else
		{
			sk_textblob_builder_alloc_run_pos_others(builder, font, count, bounds, runbuffer);
		}
	}

	[DllImport(SKIA_Apple, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(sk_rrect_set_rect_radii))]
	unsafe internal static extern void sk_rrect_set_rect_radii_apple(IntPtr rrect, SKRect* rect, SKPoint* radii);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(sk_rrect_set_rect_radii))]
	unsafe internal static extern void sk_rrect_set_rect_radii_others(IntPtr rrect, SKRect* rect, SKPoint* radii);

	/// <summary>
	/// We use this instead of the equivalent SKRoundRect.SetRectRadii because it take the array with a
	/// length of _exactly_ 4. If we rent the SKPoint array to reduce allocations, we're not guaranteed to
	/// get the exact length we need.
	/// </summary>
	internal unsafe static void sk_rrect_set_rect_radii(IntPtr rrect, SKRect* rect, SKPoint* radii)
	{
		if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
		{
			sk_rrect_set_rect_radii_apple(rrect, rect, radii);
		}
		else
		{
			sk_rrect_set_rect_radii_others(rrect, rect, radii);
		}
	}
}
