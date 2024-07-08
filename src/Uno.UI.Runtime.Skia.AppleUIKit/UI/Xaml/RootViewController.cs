using System;
using Microsoft.UI.Xaml;
using SkiaSharp;
using SkiaSharp.Views.iOS;
using UIKit;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal class RootViewController : UINavigationController, IXamlRootHost
{
	private SKCanvasView _skCanvasView;
	private XamlRoot _xamlRoot;

	public RootViewController(XamlRoot xamlRoot)
	{
		_xamlRoot = xamlRoot;
		View = _skCanvasView = new SKCanvasView();
		_skCanvasView.BackgroundColor = UIColor.Red;

		_skCanvasView.PaintSurface += OnPaintSurface;
	}

	public SKColor BackgroundColor { get; set; } = SKColors.White;

	private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		if (_xamlRoot?.VisualTree.RootElement is { } rootElement)
		{
			var surface = e.Surface;
			surface.Canvas.Clear(BackgroundColor);
			surface.Canvas.SetMatrix(SKMatrix.CreateScale((float)_xamlRoot.RasterizationScale, (float)_xamlRoot.RasterizationScale));
			if (rootElement.Visual is { } rootVisual)
			{
				rootVisual.Compositor.RenderRootVisual(surface, rootVisual, null);
			}
		}
	}

	public void InvalidateRender() => _skCanvasView?.LayoutSubviews();

	public UIElement? RootElement => _xamlRoot?.VisualTree.RootElement;
}
