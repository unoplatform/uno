#nullable enable

using System.Numerics;
using Windows.Foundation;
using SkiaSharp;
using Uno.Extensions;

namespace Windows.UI.Composition;

public partial class ShapeVisual
{
	internal override void Render(SKSurface surface)
	{
		// First we render the shapes (a.k.a. the "local content")
		// For UIElement, those are background and border or shape's content
		if (_shapes is { Count: not 0 } shapes)
		{
			var viewBox = ViewBox;
			if (viewBox is not null)
			{
				// First we go back to the parent coordinates system.
				surface.Canvas.Restore();
				surface.Canvas.Save();

				// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
				surface.Canvas.Translate(Offset.X + AnchorPoint.X, Offset.Y + AnchorPoint.Y);

				// We apply the transformed viewbox clipping
				var transform = this.GetTransform();
				var shape = new SKPath();
				shape.AddRect(new SKRect(0, 0, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y));
				shape.Transform(transform.ToSKMatrix());
				surface.Canvas.ClipPath(shape, antialias: true);

				// Applied rending transformation matrix (i.e. change coordinates system to the "rendering" one)
				if (!transform.IsIdentity)
				{
					var skTransform = transform.ToSKMatrix();
					surface.Canvas.Concat(ref skTransform);
				}

				Clip?.Apply(surface);
			}

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

			if (viewBox is not null)
			{
				surface.Canvas.Restore();
				surface.Canvas.Save();

				surface.Canvas.Translate(Offset.X + AnchorPoint.X, Offset.Y + AnchorPoint.Y);

				// Applied rending transformation matrix (i.e. change coordinates system to the "rendering" one)
				if (this.GetTransform() is { IsIdentity: false } transform)
				{
					var skTransform = transform.ToSKMatrix();
					surface.Canvas.Concat(ref skTransform);
				}

				Clip?.Apply(surface);
			}
		}

		// Second we render the children (without clipping so they are able to draw some content out of the clip bounds - in case or RenderTransform)
		if (ViewBox is { } vb)
		{
			surface.Canvas.ClipRect(new SKRect(vb.Offset.X, vb.Offset.Y, vb.Offset.X + vb.Size.X, vb.Offset.Y + vb.Size.Y));
		}

		base.Render(surface);
	}
}
