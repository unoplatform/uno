using System;
using System.Runtime.InteropServices;

namespace SkiaSharp;

internal static class UnoSkiaApi
{
	private const string SKIA = "libSkiaSharp";

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal unsafe static extern void sk_textblob_builder_alloc_run_pos(IntPtr builder, IntPtr font, int count, SKRect* bounds, UnoSKRunBufferInternal* runbuffer);
}
