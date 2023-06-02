#nullable enable

using System.Numerics;
using Windows.Foundation;
using SkiaSharp;
using Uno.Extensions;
using System.Xml.Xsl;

namespace Windows.UI.Composition;

public partial class ShapeVisual
{
	internal override void Render(SKSurface surface)
	{
		if (this is { Opacity: 0 } or { IsVisible: false })
		{
			return;
		}

		// First we render the shapes (a.k.a. the "local content")
		// For UIElement, those are background and border or shape's content
		// WARNING: As we are overriding the "Render" method, at this point we are still in the parent's coordinate system
		if (_shapes is { Count: not 0 } shapes)
		{
			PrepareSurfaceForShapesDrawing(surface);

			for (var i = 0; i < shapes.Count; i++)
			{
				var shape = shapes[i];
				var shapeTransform = shape.GetTransform();

				if (shapeTransform.IsIdentity)
				{
					shape.Render(surface);
				}
				else
				{
					var shapeTransformMatrix = shapeTransform.ToSKMatrix();

					surface.Canvas.Save();
					surface.Canvas.Concat(ref shapeTransformMatrix);

					shape.Render(surface);

					surface.Canvas.Restore();
				}
			}

			CleanupSurfaceAfterShapesDrawing(surface);
		}

		// Second we render the children
		base.Render(surface);
	}

	/// <inheritdoc />
	private protected override void Draw(SKSurface surface)
	{
		if (ViewBox is { } viewBox)
		{
			surface.Canvas.ClipRect(new SKRect(viewBox.Offset.X, viewBox.Offset.Y, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y));
		}

		base.Draw(surface);
	}

	private void PrepareSurfaceForShapesDrawing(SKSurface surface)
	{
		surface.Canvas.Save();

		// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
		surface.Canvas.Translate(Offset.X + AnchorPoint.X, Offset.Y + AnchorPoint.Y);

		var viewBox = ViewBox;
		var transform = this.GetTransform().ToSKMatrix();

		if (viewBox is not null)
		{
			// We apply the transformed viewbox clipping
			if (transform.IsIdentity)
			{
				surface.Canvas.ClipRect(new SKRect(viewBox.Offset.X, viewBox.Offset.Y, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y));
			}
			else
			{
				var shape = new SKPath();
				shape.AddRect(new SKRect(0, 0, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y));
				shape.Transform(transform);
				surface.Canvas.ClipPath(shape, antialias: true);
			}
		}
		
		if (!transform.IsIdentity)
		{
			// Applied rending transformation matrix (i.e. change coordinates system to the "rendering" one)
			surface.Canvas.Concat(ref transform);
		}

		Clip?.Apply(surface);
	}

	private void CleanupSurfaceAfterShapesDrawing(SKSurface surface)
	{
		surface.Canvas.Restore();
	}
}
