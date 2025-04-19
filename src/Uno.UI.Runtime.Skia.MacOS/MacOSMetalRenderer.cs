using System.Reflection;

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
		var get = typeof(GRContext).GetMethod("GetObject", BindingFlags.Static | BindingFlags.NonPublic);
		var context = (GRContext?)get?.Invoke(null, [ctx, true, true]);
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
}
