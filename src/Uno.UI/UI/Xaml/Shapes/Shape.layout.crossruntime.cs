#nullable enable

#if __SKIA__ || __WASM__
using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using static System.Double;
using Windows.Phone.Media.Devices;
using System.Diagnostics;

#if __SKIA__
using NativePath = Windows.UI.Composition.SkiaGeometrySource2D;
using NativeSingle = System.Double;

#elif __WASM__
using NativePath = Windows.UI.Xaml.Shapes.Shape;
using NativeSingle = System.Double;
#endif

namespace Windows.UI.Xaml.Shapes;

partial class Shape
{
	private protected Size ArrangeAbsoluteShape(Size finalSize, NativePath? path, FillRule fillRule = FillRule.EvenOdd)
	{
		if (path! == null!)
		{
			Render(null);
			return default;
		}

		var stretch = Stretch;
		var stroke = Stroke;
		var strokeThickness = stroke is null ? DefaultStrokeThicknessWhenNoStrokeDefined : StrokeThickness;
		var pathBounds = GetPathBoundingBox(path); // The BoundingBox shouldn't include the control points.
		var pathSize = (Size)pathBounds.Size;

		if (NativeSingle.IsInfinity(pathBounds.Right) || NativeSingle.IsInfinity(pathBounds.Bottom))
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Ignoring path with invalid bounds {pathBounds}");
			}

			return default;
		}

		GetStretchMetrics(stretch, strokeThickness, finalSize, pathBounds, out var xScale, out var yScale, out var dX, out var dY, out var stretchedSize);

#if __SKIA__
		Render(path, xScale, yScale, dX, dY);
#elif __WASM__
		Render(path, stretchedSize, xScale, yScale, dX, dY);
#endif

		return stretchedSize;
	}

	private protected (Size shapeSize, Rect renderingArea) ArrangeRelativeShape(Size finalSize)
	{
		var stroke = Stroke;
		var strokeThickness = stroke is null ? DefaultStrokeThicknessWhenNoStrokeDefined : StrokeThickness;
		var halfStrokeThickness = strokeThickness / 2;
		var renderingArea = new Rect(halfStrokeThickness, halfStrokeThickness, Math.Max(0, finalSize.Width - strokeThickness), Math.Max(0, finalSize.Height - strokeThickness));
		switch (Stretch)
		{
			case Stretch.None:
				renderingArea.Height = renderingArea.Width = 0;
				break;
			case Stretch.Fill:
				// size is already valid ... nothing to do!
				break;
			case Stretch.Uniform:
				if (renderingArea.Width > renderingArea.Height)
				{
					renderingArea.Width = renderingArea.Height;
				}
				else
				{
					renderingArea.Height = renderingArea.Width;
				}
				break;
			case Stretch.UniformToFill:
				if (renderingArea.Width < renderingArea.Height)
				{
					renderingArea.Width = renderingArea.Height;
				}
				else
				{
					renderingArea.Height = renderingArea.Width;
				}
				break;
		}

		return (finalSize, renderingArea);
	}
	#region Helper methods

	// NOTE: The logic is mostly from https://github.com/dotnet/wpf/blob/2ff355a607d79eef5fea7796de1f29cf9ea4fbed/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shapes/Shape.cs
	// But with few adjustments to match UWP.
	private void GetStretchMetrics(Stretch mode, double strokeThickness, Size availableSize, Rect geometryBounds,
		out double xScale, out double yScale, out double dX, out double dY, out Size stretchedSize)
	{
		if (mode == Stretch.None)
		{
			xScale = 1;
			yScale = 1;
			dX = 0;
			dY = 0;
			stretchedSize = availableSize;
			return;
		}

		if (!geometryBounds.IsEmpty)
		{
			double margin = strokeThickness / 2;

			xScale = Math.Max(availableSize.Width - strokeThickness, 0);
			yScale = Math.Max(availableSize.Height - strokeThickness, 0);

			if (geometryBounds.Width > xScale * Double.Epsilon)
			{
				xScale /= geometryBounds.Width;
			}
			else
			{
				xScale = 1;
			}

			if (geometryBounds.Height > yScale * Double.Epsilon)
			{
				yScale /= geometryBounds.Height;
			}
			else
			{
				yScale = 1;
			}

			if (mode == Stretch.Uniform)
			{
				var uniformScale = Math.Min(xScale, yScale);
				xScale = yScale = uniformScale;
			}
			else if (mode == Stretch.UniformToFill)
			{
				var uniformScale = Math.Max(xScale, yScale);
				xScale = yScale = uniformScale;
			}

			dX = margin - geometryBounds.Left * xScale;
			dY = margin - geometryBounds.Top * yScale;

			stretchedSize = new Size(
				geometryBounds.Width * xScale + strokeThickness, geometryBounds.Height * yScale + strokeThickness);
		}
		else
		{
			xScale = yScale = 1;
			dX = dY = 0;
			stretchedSize = new Size(0, 0);
		}
	}


	#endregion
}
#endif
