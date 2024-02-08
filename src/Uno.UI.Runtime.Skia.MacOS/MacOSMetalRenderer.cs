#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.Foundation;

using Uno.Foundation.Logging;
using SkiaSharp;

namespace Uno.UI.Runtime.Skia.MacOS;

internal static class MacOSMetalRenderer
{
	public static GRContext? CreateContext(nint ctx)
	{
		// Sadly only the `net6.0-[mac][ios]` version of SkiaSharp supports Metal and depends on Microsoft.[macOS|iOS].dll
		// IOW neither `net6.0` or `netstandard2.0` have the required API to create a Metal context for Skia
		// This force us to initialize things manually... so we reflect to create a metal-based GRContext
		// FIXME: contribute some extra API (e.g. using `nint` or `IntPtr`) to SkiaSharp to avoid reflection
		// net8+ alternative -> https://steven-giesel.com/blogPost/05ecdd16-8dc4-490f-b1cf-780c994346a4
		var get = typeof(GRContext).GetMethod("GetObject", BindingFlags.Static | BindingFlags.NonPublic)!;
		var context = (GRContext?)get?.Invoke(null, new object[] { ctx, true });
		if (context is null)
		{
			// Macs since 2012 have Metal 2 support and macOS 10.14 Mojave (2018) requires Metal
			// List of Mac supporting Metal https://support.apple.com/en-us/HT205073
			if (typeof(MacOSMetalRenderer).Log().IsEnabled(LogLevel.Error))
			{
				typeof(MacOSMetalRenderer).Log().Error("Failed to initialize Metal from native code.");
			}
		}
		return context;
	}

	private static readonly ConstructorInfo? _rt = typeof(GRBackendRenderTarget).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(nint), typeof(bool)], null);

	public static unsafe GRBackendRenderTarget? CreateTarget(GRContext context, double nativeWidth, double nativeHeight, nint texture)
	{
		// note: size is doubled for retina displays
		var info = new GRMtlTextureInfoNative() { Texture = texture };
		var nt = NativeSkia.gr_backendrendertarget_new_metal((int)nativeWidth, (int)nativeHeight, 1, &info);
		if (nt == IntPtr.Zero)
		{
			if (typeof(MacOSMetalRenderer).Log().IsEnabled(LogLevel.Error))
			{
				typeof(MacOSMetalRenderer).Log().Error("Failed to initialize Skia with Metal backend.");
			}
			return null;
		}
		// FIXME: contribute some extra API (e.g. using `nint` or `IntPtr`) to SkiaSharp to avoid reflection
		return (GRBackendRenderTarget)_rt?.Invoke(new object[] { nt, true })!;
	}
}
