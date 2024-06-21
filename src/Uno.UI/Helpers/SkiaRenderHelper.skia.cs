using System;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using Uno.Extensions;

namespace Uno.UI.Helpers;

internal static class SkiaRenderHelper
{
	// reusable paths to avoid repetitive allocations
	private static SKPath _mainPath = new SKPath();
	private static SKPath _visualPath = new SKPath();
	private static SKPath _clipPath = new SKPath();
	private static SKPath _negativePath = new SKPath();

	public static void RenderRootVisual(int width, int height, ShapeVisual rootVisual, SKSurface surface, SKCanvas canvas)
	{
		var nativeRects = ContentPresenter.GetNativeRects();

		if (nativeRects.IsEmpty)
		{
			rootVisual.Compositor.RenderRootVisual(surface, rootVisual, null);
		}
		else
		{
			// Assuming the canvas we're drawing on is on top of the native elements,
			// we want to crop out "see-through windows" so we can see the native elements underneath the canvas

			// the entire viewport
			(_mainPath ??= new SKPath()).Reset();
			_mainPath.AddRect(new SKRect(0, 0, width, height));

			rootVisual.Compositor.RenderRootVisual(surface, rootVisual, (session, visual) =>
			{
				// minus the native-element areas
				if (visual.IsNativeHostVisual)
				{
					(_clipPath ??= new SKPath()).Reset();
					_clipPath.AddRect(canvas.DeviceClipBounds);
					(_visualPath ??= new SKPath()).Reset();
					_visualPath.AddRect(visual.TotalMatrix.ToMatrix3x2().Transform(new Rect(new Point(), visual.Size.ToSize())).ToSKRect());
					_mainPath = _mainPath.Op(_clipPath.Op(_visualPath, SKPathOp.Intersect), SKPathOp.Difference);
				}

				// plus the popups
				if (visual.IsPopupVisual)
				{
					(_clipPath ??= new SKPath()).Reset();
					_clipPath.AddRect(canvas.DeviceClipBounds); // possible QoL improvement: find a way to get the tight clip path (e.g. for rounded corners) not just the bounding rect
					(_visualPath ??= new SKPath()).Reset();
					_visualPath.AddRect(visual.TotalMatrix.ToMatrix3x2().Transform(new Rect(new Point(), visual.Size.ToSize())).ToSKRect());
					// note that popups are always traversed last in the tree, so there are no "races" between the paths of PopupVisuals and NativeHostVisuals
					_mainPath = _mainPath.Op(_clipPath.Op(_visualPath, SKPathOp.Intersect), SKPathOp.Union);
				}
			});

			// mainPath is now everywhere we want to actually draw
			// so we clear the "negative" of that
			(_negativePath ??= new SKPath()).Reset();
			_negativePath.AddRect(new SKRect(0, 0, width, height));
			_negativePath = _negativePath.Op(_mainPath, SKPathOp.Difference);
			canvas.Save();
			canvas.ClipPath(_negativePath);
			canvas.Clear(SKColors.Transparent);
			canvas.Restore();
		}
	}
}
