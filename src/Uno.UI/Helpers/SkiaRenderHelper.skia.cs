using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using Uno.Extensions;

namespace Uno.UI.Helpers;

internal static class SkiaRenderHelper
{
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
			var mainPath = new SKPath();
			mainPath.AddRect(new SKRect(0, 0, width, height));

			// minus the native-element areas
			foreach (var rect in ContentPresenter.GetNativeRects())
			{
				var path = new SKPath();
				path.AddRect(rect.ToSKRect());
				mainPath = mainPath.Op(path, SKPathOp.Difference);
			}

			rootVisual.Compositor.RenderRootVisual(surface, rootVisual, (session, visual) =>
			{
				// plus the popups
				if (visual.IsPopupVisual)
				{
					var rect = visual.TotalMatrix.ToMatrix3x2().Transform(new Windows.Foundation.Rect(0, 0, visual.Size.X, visual.Size.Y));
					var path = new SKPath();
					path.AddRect(rect.ToSKRect());
					mainPath = mainPath.Op(path, SKPathOp.Union);
				}
			});

			// mainPath is now everywhere we want to actually draw
			// so we clear the "negative" of that
			var negativePath = new SKPath();
			negativePath.AddRect(new SKRect(0, 0, width, height));
			negativePath = negativePath.Op(mainPath, SKPathOp.Difference);
			canvas.Save();
			canvas.ClipPath(negativePath);
			canvas.Clear(SKColors.Transparent);
			canvas.Restore();
		}
	}
}
