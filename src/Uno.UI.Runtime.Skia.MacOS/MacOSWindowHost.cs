#nullable enable

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using SkiaSharp;

using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

using Window = Microsoft.UI.Xaml.Window;

using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSWindowHost : IXamlRootHost
{
	private readonly MacOSWindowNative _nativeWindow;
	private readonly Window _winUIWindow;
	private readonly DisplayInformation _displayInformation;
	private readonly GRContext? _context;

	internal static XamlRootMap<IXamlRootHost> XamlRootMap { get; } = new();

	public MacOSWindowHost(MacOSWindowNative nativeWindow, Window winUIWindow)
	{
		_nativeWindow = nativeWindow ?? throw new ArgumentNullException(nameof(nativeWindow));
		_winUIWindow = winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow));

		_displayInformation = DisplayInformation.GetForCurrentView();

		// RegisterForBackgroundColor();

		var requestedSurfaceType = MacSkiaHost.Current?.RenderSurfaceType;
		if (requestedSurfaceType != RenderSurfaceType.Software)
		{
			// Automatic (Metal first) or Metal is requested
			var ctx = NativeUno.uno_window_get_metal(_nativeWindow.Handle);
			_context = MacOSMetalRenderer.CreateContext(ctx);
		}
		if ((requestedSurfaceType != RenderSurfaceType.Auto) && _context is null)
		{
			// Automatic (fallback) or software is requested
			// TODO: 
		}
	}

	public MacOSWindowNative NativeWindow => _nativeWindow;
	public Window Window => _winUIWindow;

	internal event EventHandler<Size>? SizeChanged;

	private void UpdateWindowSize(double nativeWidth, double nativeHeight)
	{
		var sizeAdjustment = _displayInformation.FractionalScaleAdjustment;
		SizeChanged?.Invoke(this, new Windows.Foundation.Size(nativeWidth / sizeAdjustment, nativeHeight / sizeAdjustment));
	}

	private static readonly ConstructorInfo? _rt = typeof(GRBackendRenderTarget).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(nint), typeof(bool)], null);

	private bool hackFirstDraw = true;

	private unsafe void Draw(double nativeWidth, double nativeHeight, nint texture)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Window {_nativeWindow.Handle} drawing {nativeWidth}x{nativeHeight} texture: {texture} FullScreen: {NativeUno.uno_application_is_full_screen()}");
		}

		// FIXME: we get the first update for windows sizes before we have completed the initialization
		if (hackFirstDraw)
		{
			UpdateWindowSize(nativeWidth, nativeHeight);
		}

		using var target = MacOSMetalRenderer.CreateTarget(_context!, nativeWidth, nativeHeight, texture);
		using var surface = SKSurface.Create(_context, target, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888);
		using var canvas = surface.Canvas;

		using (new SKAutoCanvasRestore(canvas, true))
		{
			canvas.Clear(SKColors.White);
			// canvas.Clear(BackgroundColor);

			if (RootElement?.Visual is { } rootVisual)
			{
				RootElement.XamlRoot?.Compositor.RenderRootVisual(surface, rootVisual);
			}
		}

		canvas.Flush();
		surface.Flush();
		_context?.Flush();
	}

	internal static Dictionary<nint, WeakReference<MacOSWindowHost>> windows = new();

	public static unsafe void Register()
	{
		// FIXME: ugly but this loads libSkiaSharp into memory (because it looks for @rpath/libSkiaSharp.dylib)
		NativeSkia.gr_direct_context_make_metal(0, 0);

		NativeUno.uno_set_draw_callback(&Draw);
		NativeUno.uno_set_resize_callback(&Resize);
	}

	public UIElement? RootElement => _winUIWindow.RootElement;

	void IXamlRootHost.InvalidateRender()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Window {_nativeWindow.Handle} invalidated.");
		}
		_winUIWindow.RootElement?.XamlRoot?.InvalidateOverlays();
		NativeUno.uno_window_invalidate(_nativeWindow.Handle);
	}

	public static void Register(nint handle, MacOSWindowHost host)
	{
		windows.Add(handle, new WeakReference<MacOSWindowHost>(host));
	}

	public static void Unregister(nint handle)
	{
		windows.Remove(handle);
	}

	private static MacOSWindowHost? GetWindowHost(nint handle)
	{
		if (windows.TryGetValue(handle, out var weak))
		{
			weak.TryGetTarget(out var window);
			return window;
		}
		return null;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void Draw(nint handle, double width, double height, nint texture)
	{
		var window = GetWindowHost(handle);
		if (window is not null)
		{
			window.Draw(width, height, texture);
		}
		else if (typeof(MacOSWindowHost).Log().IsEnabled(LogLevel.Error))
		{
			typeof(MacOSWindowHost).Log().Error($"MacOSWindowHost.Draw could not map {handle} with an NSWindow");
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void Resize(nint handle, double width, double height)
	{
		var window = GetWindowHost(handle);
		if (window is not null)
		{
			window.UpdateWindowSize(width, height);
		}
		else if (typeof(MacOSWindowHost).Log().IsEnabled(LogLevel.Error))
		{
			typeof(MacOSWindowHost).Log().Error($"MacOSWindowHost.Resize could not map {handle} with an NSWindow");
		}
	}
}
