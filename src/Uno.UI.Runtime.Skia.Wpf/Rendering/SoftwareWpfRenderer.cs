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
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
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

		int width, height;

		var dpi = _xamlRoot.RasterizationScale;
		double dpiScaleX = dpi;
		double dpiScaleY = dpi;
		if (_host.IgnorePixelScaling)
		{
			width = (int)_hostControl.ActualWidth;
			height = (int)_hostControl.ActualHeight;
		}
		else
		{
			var matrix = PresentationSource.FromVisual(_hostControl).CompositionTarget.TransformToDevice;
			dpiScaleX = matrix.M11;
			dpiScaleY = matrix.M22;
			width = (int)(_hostControl.ActualWidth * dpiScaleX);
			height = (int)(_hostControl.ActualHeight * dpiScaleY);
		}

		var info = new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

		// reset the bitmap if the size has changed
		if (_bitmap == null || info.Width != _bitmap.PixelWidth || info.Height != _bitmap.PixelHeight)
		{
			_bitmap = new WriteableBitmap(width, height, 96 * dpiScaleX, 96 * dpiScaleY, PixelFormats.Pbgra32, null);
		}

		// draw on the bitmap
		_bitmap.Lock();
		using (var surface = SKSurface.Create(info, _bitmap.BackBuffer, _bitmap.BackBufferStride))
		{
			if (_host.RootElement?.Visual is { } rootVisual)
			{
				var isSoftwareRenderer = rootVisual.Compositor.IsSoftwareRenderer;
				try
				{
					rootVisual.Compositor.IsSoftwareRenderer = true;

					SkiaRenderHelper.RenderPicture(
						surface,
						_host.Picture,
						BackgroundColor,
						_fpsHelper);

					if (_host.NativeOverlayLayer is { } nativeLayer)
					{
						nativeLayer.Clip ??= new PathGeometry();
						((PathGeometry)nativeLayer!.Clip).Figures = PathFigureCollection.Parse(_host.ClipPath?.ToSvgPathData());
					}
					else
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error($"Airspace clipping failed because ${nameof(_host.NativeOverlayLayer)} is null");
						}
					}
				}
				finally
				{
					rootVisual.Compositor.IsSoftwareRenderer = isSoftwareRenderer;
				}
			}
		}

		// draw the bitmap to the screen
		_bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
		_bitmap.Unlock();
		drawingContext.DrawImage(_bitmap, new Rect(0, 0, _hostControl.ActualWidth, _hostControl.ActualHeight));
	}
}
