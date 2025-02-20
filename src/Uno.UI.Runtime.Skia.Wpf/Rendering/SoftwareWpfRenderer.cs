#nullable enable

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.UI.Xaml;
using SkiaSharp;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Helpers;
using Visibility = System.Windows.Visibility;
using WinUI = Windows.UI.Xaml;
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


		int width, height;

		_xamlRoot ??= WpfManager.XamlRootMap.GetRootForHost(_host) ?? throw new InvalidOperationException("XamlRoot must not be null when renderer is initialized");
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
			var canvas = surface.Canvas;
			canvas.Clear(BackgroundColor);
			canvas.SetMatrix(SKMatrix.CreateScale((float)dpiScaleX, (float)dpiScaleY));
			if (_host.RootElement?.Visual is { } rootVisual)
			{
				SkiaRenderHelper.RenderRootVisualAndClearNativeAreas(width, height, rootVisual, surface);

				if (rootVisual.Compositor.IsSoftwareRenderer is null)
				{
					rootVisual.Compositor.IsSoftwareRenderer = true;
				}
			}
		}

		// draw the bitmap to the screen
		_bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
		_bitmap.Unlock();
		drawingContext.DrawImage(_bitmap, new Rect(0, 0, _hostControl.ActualWidth, _hostControl.ActualHeight));
	}
}
