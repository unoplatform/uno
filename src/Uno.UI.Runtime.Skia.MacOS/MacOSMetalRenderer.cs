#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using SkiaSharp;

using Windows.Foundation;
using Microsoft.UI.Xaml;

using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal static class MacOSMetalRenderer
{
	internal static GRContext? Context { get; set; }


	public static unsafe void Register()
	{
		// FIXME: ugly but this loads libSkiaSharp into memory
		NativeSkia.gr_direct_context_make_metal(0, 0);

		NativeUno.uno_set_draw_callback(&Draw);
		NativeUno.uno_set_resize_callback(&Resize);
	}

	static public void CreateContext(IntPtr ctx)
	{
		// Sadly only the `net6.0-[mac][ios]` version of SkiaSharp supports Metal and depends on Microsoft.[macOS|iOS].dll
		// IOW neither `net6.0` or `netstandard2.0` have the required API to create a Metal context for Skia
		// This force us to initialize things manually... so we reflect to create a metal-based GRContext
		// FIXME: contribute some extra API (e.g. using `nint` or `IntPtr`) to SkiaSharp to avoid reflection
		// net8+ alternative -> https://steven-giesel.com/blogPost/05ecdd16-8dc4-490f-b1cf-780c994346a4
		var get = typeof(GRContext).GetMethod("GetObject", BindingFlags.Static | BindingFlags.NonPublic)!;
		Context = (GRContext?) get?.Invoke(null, new object [] { ctx, true });
		if (Context is null)
		{
			Console.WriteLine("Failed to initialize Metal.");
			// FIXME: is it worth fallback to software ? 
			// Macs since 2012 have Metal 2 support and macOS 10.14 Mojave (2018) requires Metal
			// List of Mac supporting Metal https://support.apple.com/en-us/HT205073
		}
	}

	private static readonly ConstructorInfo? _rt = typeof(GRBackendRenderTarget).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(nint), typeof(bool)], null);

	private static bool _firstDraw = true;

	[UnmanagedCallersOnly(CallConvs = new[] {typeof(CallConvCdecl)})]
	private static unsafe void Draw(CGSize size, nint texture)
	{
		// HACK: this init something we need for the first draw to display properly
		if (_firstDraw)
		{
			Window.Current.OnNativeSizeChanged(new Size(size.Width, size.Height));
			_firstDraw = false;
		}
		Console.WriteLine($"MacSkiaHost.Draw {size.Width}x{size.Height} texture: {texture} FullScreen: {NativeUno.uno_application_is_full_screen()}");

		// note: size is doubled for retina displays
		var info = new GRMtlTextureInfoNative() { Texture = texture };
		var nt = NativeSkia.gr_backendrendertarget_new_metal((int)size.Width, (int)size.Height, 1, &info);
		if (nt == IntPtr.Zero)
		{
			// TODO: log error or handle software-based fallback
		}
		// FIXME: contribute some extra API (e.g. using `nint` or `IntPtr`) to SkiaSharp to avoid reflection
       	using var target = (GRBackendRenderTarget)_rt?.Invoke(new object[] { nt, true })!;
		using var surface = SKSurface.Create(Context, target, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888);
		using var canvas = surface.Canvas;

		using (new SKAutoCanvasRestore(canvas, true))
		{
			canvas.Clear(SKColors.White);
			// canvas.Clear(BackgroundColor);

			if (Window.Current.RootElement?.Visual is { } rootVisual)
			{
				Window.Current.Compositor.RenderRootVisual(surface, rootVisual);
			}
		}

		canvas.Flush();
		surface.Flush();
		Context?.Flush();
	}

	[UnmanagedCallersOnly(CallConvs = new[] {typeof(CallConvCdecl)})]
	private static void Resize(CGSize size) // FIXME: remove CGSize from interop code
	{
		Console.WriteLine($"MacSkiaHost.Resize {size.Width}x{size.Height}");
		Window.Current.OnNativeSizeChanged(new Size(size.Width, size.Height));
	}
}
