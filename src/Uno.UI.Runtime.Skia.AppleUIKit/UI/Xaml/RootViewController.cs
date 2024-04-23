using SkiaSharp;
using SkiaSharp.Views.iOS;
using UIKit;

namespace Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal class RootViewController : UINavigationController
{
	private SKCanvasView _skCanvasView;

	public RootViewController()
	{
		View = _skCanvasView = new SKCanvasView();
		_skCanvasView.BackgroundColor = UIColor.Red;

		_skCanvasView.PaintSurface += OnPaintSurface;
	}

	private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		if (Microsoft.UI.Xaml.Window.CurrentSafe is { RootElement: { } root } window)
		{
			var canvas = e.Surface.Canvas;
			canvas.Clear(SKColors.Red);
			window.Compositor.RenderRootVisual(e.Surface, root.Visual);
		}

		//InvalidateRender();
	}

	internal void InvalidateRender() => _skCanvasView?.LayoutSubviews();
}
