#nullable enable

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Windows.Graphics.Display;
using WinUI = Microsoft.UI.Xaml;
using WpfControl = global::System.Windows.Controls.Control;

namespace Uno.UI.Runtime.Skia.Wpf.Rendering;

internal class SoftwareWpfRenderer : IWpfRenderer
{
	private WpfControl _hostControl;
	private DisplayInformation? _displayInformation;
	private WriteableBitmap? _bitmap;
	private IWpfXamlRootHost _host;

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

		if (_displayInformation == null)
		{
			_displayInformation = DisplayInformation.GetForCurrentView();
		}

		var dpi = _displayInformation.RawPixelsPerViewPixel;
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
			surface.Canvas.Clear(BackgroundColor);
			surface.Canvas.SetMatrix(SKMatrix.CreateScale((float)dpiScaleX, (float)dpiScaleY));
			if (_host.RootElement?.Visual is { } rootVisual)
			{
				rootVisual.Compositor.RenderRootVisual(surface, rootVisual);

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
