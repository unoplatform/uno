#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.Foundation;

using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal static class MacOSMetalRenderer
{
	internal static Dictionary<nint,WeakReference<MacOSWindowWrapper>> windows = new();

	public static unsafe void Register()
	{
		// FIXME: ugly but this loads libSkiaSharp into memory (because it looks for @rpath/libSkiaSharp.dylib)
		NativeSkia.gr_direct_context_make_metal(0, 0);

		NativeUno.uno_set_draw_callback(&Draw);
		NativeUno.uno_set_resize_callback(&Resize);
	}

	public static void Register(MacOSWindowWrapper window)
	{
		windows.Add(window.Handle, new WeakReference<MacOSWindowWrapper>(window));
	}

	public static void Unregister(MacOSWindowWrapper window)
	{
		windows.Remove(window.Handle);
	}

	private static MacOSWindowWrapper? GetWindow(nint handle)
	{
		if (windows.TryGetValue(handle, out var weak))
		{
			weak.TryGetTarget(out var window);
			return window;
		}
		return null;
	}

	[UnmanagedCallersOnly(CallConvs = new[] {typeof(CallConvCdecl)})]
	private static void Draw(nint handle, double width, double height, nint texture)
	{
		var window = GetWindow(handle);
		if (window is not null)
		{
			window.Draw(width, height, texture);
		}
		else if (typeof(MacOSMetalRenderer).Log().IsEnabled(LogLevel.Error))
		{
			typeof(MacOSMetalRenderer).Log().Error($"MacOSMetalRenderer.Draw could not map {handle} with an NSWindow");
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] {typeof(CallConvCdecl)})]
	private static void Resize(nint handle, double width, double height)
	{
		var window = GetWindow(handle);
		if (window is not null)
		{
			window.Bounds = window.VisibleBounds = new Rect(0, 0, width, height);
		}
		else if (typeof(MacOSMetalRenderer).Log().IsEnabled(LogLevel.Error))
		{
			typeof(MacOSMetalRenderer).Log().Error($"MacOSMetalRenderer.Resize could not map {handle} with an NSWindow");
		}
	}
}
