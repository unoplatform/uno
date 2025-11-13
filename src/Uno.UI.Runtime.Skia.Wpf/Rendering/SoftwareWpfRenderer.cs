#nullable enable

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.UI.Xaml;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Visibility = System.Windows.Visibility;
using WpfControl = global::System.Windows.Controls.Control;

namespace Uno.UI.Runtime.Skia.Wpf.Rendering;

internal class SoftwareWpfRenderer : IWpfRenderer
{
	private readonly WpfControl _hostControl;
	private readonly IWpfXamlRootHost _host;
	private WriteableBitmap? _bitmap;
	private XamlRoot? _xamlRoot;

	public SoftwareWpfRenderer(IWpfXamlRootHost host)
	{
		_hostControl = host as WpfControl ?? throw new InvalidOperationException("Host should be a WPF control");
		_host = host;
	}

	public SKColor BackgroundColor { get; set; } = SKColors.White;

	public bool TryInitialize() => true;

	public void Dispose() { }

	public void Render(DrawingContext drawingContext)
	{
		if (_hostControl.ActualWidth == 0
			|| _hostControl.ActualHeight == 0
			|| double.IsNaN(_hostControl.ActualWidth)
			|| double.IsNaN(_hostControl.ActualHeight)
			|| double.IsInfinity(_hostControl.ActualWidth)
			|| double.IsInfinity(_hostControl.ActualHeight)
			|| _hostControl.Visibility != Visibility.Visible)
		{
			return;
		}

		_xamlRoot ??= XamlRootMap.GetRootForHost(_host) ?? throw new InvalidOperationException("XamlRoot must not be null when renderer is initialized");

		_bitmap?.Lock();
		var surface = _bitmap is not null ? SKSurface.Create(new SKImageInfo(_bitmap.PixelWidth, _bitmap.PixelHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul), _bitmap.BackBuffer, _bitmap.BackBufferStride) : null;
		var nativeElementClipPath = ((Microsoft.UI.Xaml.Media.CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(surface?.Canvas, size =>
		{
			_bitmap?.Unlock();
			_bitmap = new WriteableBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32, null);
			_bitmap.Lock();
			surface = SKSurface.Create(new SKImageInfo(_bitmap.PixelWidth, _bitmap.PixelHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul), _bitmap.BackBuffer, _bitmap.BackBufferStride);
			return surface.Canvas;
		});

		if (_host.NativeOverlayLayer is { } nativeLayer)
		{
			nativeLayer.Clip ??= new PathGeometry();
			((PathGeometry)nativeLayer.Clip).Figures = PathFigureCollection.Parse(nativeElementClipPath.ToSvgPathData());
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Airspace clipping failed because ${nameof(_host.NativeOverlayLayer)} is null");
			}
		}

		// draw the bitmap to the screen
		if (_bitmap is not null)
		{
			_bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
			_bitmap.Unlock();
			drawingContext.DrawImage(_bitmap, new Rect(0, 0, _hostControl.ActualWidth, _hostControl.ActualHeight));
		}
	}
}
