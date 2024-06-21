using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using Uno.Extensions;

namespace Uno.UI.Helpers;

internal static class SkiaRenderHelper
{
	public static void RenderRootVisual(int width, int height, ShapeVisual rootVisual, SKSurface surface, SKCanvas canvas)
	{

		var mainPath = new SKPath();
		mainPath.AddRect(new SKRect(0, 0, width, height));
		foreach (var rect in ContentPresenter.GetNativeRects())
		{
			var path = new SKPath();
			path.AddRect(rect.ToSKRect());
			mainPath = mainPath.Op(path, SKPathOp.Difference);
		}
		rootVisual.Compositor.RenderRootVisual(surface, rootVisual, (session, visual) =>
		{
			if (visual.IsPopupVisual)
			{
				var rect = visual.TotalMatrix.ToMatrix3x2().Transform(new Windows.Foundation.Rect(0, 0, visual.Size.X, visual.Size.Y));
				var path = new SKPath();
				path.AddRect(rect.ToSKRect());
				mainPath = mainPath.Op(path, SKPathOp.Union);
			}
		});

		var negativePath = new SKPath();
		negativePath.AddRect(new SKRect(0, 0, width, height));
		negativePath = negativePath.Op(mainPath, SKPathOp.Difference);
		canvas.ClipPath(negativePath);
		canvas.Clear(SKColors.Transparent);
	}
}
