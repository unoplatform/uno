using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SkiaSharp;

internal static class UnoSkiaApi
{
	private const string SKIA = "libSkiaSharp";
	private const string SKIA_Apple = "@rpath/libSkiaSharp.framework/libSkiaSharp";

	internal static void Initialize()
	{
		NativeLibrary.SetDllImportResolver(typeof(UnoSkiaApi).Assembly, DllImportResolver);
	}

	private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		if (libraryName == SKIA && (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS()))
		{
			return NativeLibrary.Load(SKIA_Apple, assembly, null);
		}

		// Fallback to the default DllImportResolver
		return IntPtr.Zero;
	}

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern unsafe void sk_canvas_draw_picture(IntPtr canvas, IntPtr picture, SKMatrix* matrix, IntPtr paint);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void sk_canvas_draw_text_blob(IntPtr canvas, IntPtr textBlob, float x, float y, IntPtr paint);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern unsafe void sk_canvas_set_matrix(IntPtr canvas, SKMatrix44* matrix);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr sk_picture_recorder_end_recording(IntPtr recorder);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void sk_refcnt_safe_unref(IntPtr refcnt);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern unsafe void sk_rrect_set_rect_radii(IntPtr rrect, SKRect* rect, SKPoint* radii);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern unsafe void sk_textblob_builder_alloc_run_pos(IntPtr builder, IntPtr font, int count, SKRect* bounds, UnoSKRunBufferInternal* runbuffer);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr sk_textblob_builder_make(IntPtr builder);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void sk_textblob_unref(IntPtr textBlob);
}
