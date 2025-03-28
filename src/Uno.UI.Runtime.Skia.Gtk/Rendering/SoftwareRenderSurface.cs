#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Cairo;
using Gtk;
using Windows.UI.Composition;
using Windows.UI.Xaml.Input;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Uno.UI.Runtime.Skia.Gtk.Hosting;
using Pango;
using Uno.UI.Helpers;
using Context = Cairo.Context;

namespace Uno.UI.Runtime.Skia.Gtk;

internal class SoftwareRenderSurface : DrawingArea, IGtkRenderer
{
	private SKSurface? _surface;
	private SKBitmap? _bitmap;
	private int _bheight, _bwidth;
	private ImageSurface? _gtkSurface;
	private int renderCount;

	private float _scale = 1;
	private XamlRoot _xamlRoot;
	private readonly SKColorType _colorType;
	private readonly IGtkXamlRootHost _host;

	public SKColor BackgroundColor { get; set; }
		= SKColors.White;

	public SoftwareRenderSurface(IGtkXamlRootHost host)
	{
		_xamlRoot = GtkManager.XamlRootMap.GetRootForHost(host) ?? throw new InvalidOperationException("XamlRoot must not be null when renderer is initialized");
		_xamlRoot.Changed += OnXamlRootChanged;
		UpdateDpi();

		_colorType = SKImageInfo.PlatformColorType;
		// R and B channels are inverted on macOS running on arm64 CPU and this is not detected by Skia
		if (OperatingSystem.IsMacOS())
		{
			if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
			{
				_colorType = SKColorType.Bgra8888;
			}
		}
		_host = host;
	}

	public Widget Widget => this;

	public void InvalidateRender() => QueueDrawArea(0, 0, 10000, 10000);

	protected override bool OnDrawn(Context cr)
	{
		Stopwatch? sw = null;

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			sw = Stopwatch.StartNew();
			this.Log().Trace($"Render {renderCount++}");
		}

		var scaledWidth = (int)(AllocatedWidth * _scale);
		var scaledHeight = (int)(AllocatedHeight * _scale);

		// reset the surfaces (skia/cairo) and bitmap if the size has changed
		if (_surface == null || scaledWidth != _bwidth || scaledHeight != _bheight)
		{
			_gtkSurface?.Dispose();
			_surface?.Dispose();
			_bitmap?.Dispose();

			var info = new SKImageInfo(scaledWidth, scaledHeight, _colorType, SKAlphaType.Premul);
			_bitmap = new SKBitmap(info);
			_bwidth = _bitmap.Width;
			_bheight = _bitmap.Height;
			var pixels = _bitmap.GetPixels(out _);
			_surface = SKSurface.Create(info, pixels);
			_gtkSurface = new ImageSurface(pixels, Format.Argb32, _bwidth, _bheight, _bwidth * 4);
		}

		var canvas = _surface.Canvas;

		using (new SKAutoCanvasRestore(canvas, true))
		{
			canvas.Clear(BackgroundColor);
			canvas.Scale(_scale);

			if (_host.RootElement?.Visual is { } rootVisual)
			{
				var compositor = Compositor.GetSharedCompositor();
				SkiaRenderHelper.RenderRootVisualAndClearNativeAreas(scaledWidth, scaledHeight, rootVisual, _surface);

				if (compositor.IsSoftwareRenderer is null)
				{
					compositor.IsSoftwareRenderer = true;
				}
			}
		}

		_gtkSurface!.MarkDirty();
		cr.Save();
		cr.SetSourceSurface(_gtkSurface, 0, 0);
		cr.Paint();
		cr.Restore();

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			sw?.Stop();
			this.Log().Trace($"Frame: {sw?.Elapsed}");
		}

		return true;
	}

	public void TakeScreenshot(string filePath)
	{
		using Stream memStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
		using SKManagedWStream wstream = new(memStream);

		_bitmap?.Encode(wstream, SKEncodedImageFormat.Png, 100);
	}

	private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateDpi();

	private void UpdateDpi()
	{
		var newScale = (float)_xamlRoot.RasterizationScale;
		if (_scale != newScale)
		{
			_scale = newScale;
			InvalidateRender();
		}
	}
}
