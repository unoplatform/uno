#nullable enable

using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;
using SkiaSharp;
using Uno.Extensions;

namespace Uno.UI.Helpers;

internal static class SkiaRenderHelper
{
	// reusable paths to avoid repetitive allocations
	private static readonly SKPath _visualPath = new SKPath();
	private static readonly SKPath _clipPath = new SKPath();

	/// <summary>
	/// Does a rendering cycle and returns a path that represents the total area that was drawn
	/// </summary>
	public static void RenderRootVisualAndClearNativeAreas(int width, int height, ShapeVisual rootVisual, SKSurface surface)
	{
		var path = RenderRootVisualAndReturnPath(width, height, rootVisual, surface);
		if (path is { })
		{
			// we clear the "negative" of what was drawn
			var negativePath = new SKPath();
			negativePath.AddRect(new SKRect(0, 0, width, height));
			negativePath = negativePath.Op(path, SKPathOp.Difference);
			var canvas = surface.Canvas;
			canvas.Save();
			canvas.ClipPath(negativePath);
			canvas.Clear(SKColors.Transparent);
			canvas.Restore();
		}
	}

	/// <summary>
	/// Does a rendering cycle and returns a path that represents the total area that was drawn
	/// or null if the entire window is drawn.
	/// </summary>
	public static SKPath? RenderRootVisualAndReturnPath(int width, int height, ShapeVisual rootVisual, SKSurface surface)
	{
		if (!ContentPresenter.HasNativeElements())
		{
			rootVisual.Compositor.RenderRootVisual(surface, rootVisual, null);
			return null;
		}
		else
		{
			var canvas = surface.Canvas;
			// Assuming the canvas we're drawing on is on top of the native elements,
			// we want to crop out "see-through windows" so we can see the native elements underneath the canvas

			// the entire viewport
			var mainPath = new SKPath();
			mainPath.AddRect(new SKRect(0, 0, width, height));

			rootVisual.Compositor.RenderRootVisual(surface, rootVisual, (session, visual) =>
			{
				// minus the native-element areas
				if (visual.IsNativeHostVisual)
				{
					_clipPath.Reset();
					_clipPath.AddRect(canvas.DeviceClipBounds);
					_visualPath.Reset();
					_visualPath.AddRect(visual.TotalMatrix.ToMatrix3x2().Transform(new Rect(new Point(), visual.Size.ToSize())).ToSKRect());
					mainPath = mainPath.Op(_clipPath.Op(_visualPath, SKPathOp.Intersect), SKPathOp.Difference);
				}

				// plus the popups
				if (visual.IsPopupVisual)
				{
					_clipPath.Reset();
					_clipPath.AddRect(canvas.DeviceClipBounds); // possible QoL improvement: find a way to get the tight clip path (e.g. for rounded corners) not just the bounding rect
					_visualPath.Reset();
					_visualPath.AddRect(visual.TotalMatrix.ToMatrix3x2().Transform(new Rect(new Point(), visual.Size.ToSize())).ToSKRect());
					// note that popups are always traversed last in the tree, so there are no "races" between the paths of PopupVisuals and NativeHostVisuals
					mainPath = mainPath.Op(_clipPath.Op(_visualPath, SKPathOp.Intersect), SKPathOp.Union);
				}
			});

			return mainPath;
		}
	}
}
