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
			return NativeLibrary.Load(SKIA_Apple, assembly, searchPath);
		}

		// Fallback to the default DllImportResolver
		return IntPtr.Zero;
	}

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void sk_canvas_draw_text_blob(IntPtr canvas, IntPtr textBlob, float x, float y, IntPtr paint);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern unsafe void sk_canvas_set_matrix(IntPtr canvas, SKMatrix44* matrix);

	[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
	internal static extern unsafe void sk_rrect_set_rect_radii(IntPtr rrect, SKRect* rect, SKPoint* radii);
}
