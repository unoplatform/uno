#nullable enable

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace Uno.UI.Helpers;

internal static class SkiaRenderHelper
{
	/// <summary>
	/// Does a rendering cycle and returns a path that represents the visible area of the native views.
	/// Takes the current TotalMatrix of the surface's canvas into account
	/// </summary>
	public static SKPath RenderRootVisualAndReturnNegativePath(int width, int height, ContainerVisual rootVisual, SKCanvas canvas)
	{
		rootVisual.Compositor.RenderRootVisual(canvas, rootVisual);
		if (!ContentPresenter.HasNativeElements())
		{
			return new SKPath();
		}
		var initialCanvasTransform = canvas.TotalMatrix;
		rootVisual.Compositor.RenderRootVisual(canvas, rootVisual);
		var parentClipPath = new SKPath();
		parentClipPath.AddRect(new SKRect(0, 0, width, height));
		var outPath = new SKPath();
		rootVisual.GetNativeViewPath(parentClipPath, outPath);
		outPath.Transform(initialCanvasTransform, outPath);
		return outPath;
	}

	/// <summary>
	/// Does a rendering cycle and returns a path that represents the total area that was drawn
	/// minus the native view areas.
	/// </summary>
	public static SKPath RenderRootVisualAndReturnPath(int width, int height, ContainerVisual rootVisual, SKCanvas canvas)
	{
		var outPath = new SKPath();
		outPath.AddRect(new SKRect(0, 0, width, height));
		outPath.Transform(canvas.TotalMatrix, outPath);
		outPath.Op(RenderRootVisualAndReturnNegativePath(width, height, rootVisual, canvas), SKPathOp.Difference, outPath);
		return outPath;
	}
}
