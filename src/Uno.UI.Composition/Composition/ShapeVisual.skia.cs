#nullable enable

using System.Numerics;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Composition;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{
	internal override void Render(in DrawingSession parentSession)
	{
		if (this is { Opacity: 0 } or { IsVisible: false })
		{
			return;
		}

		// First we render the shapes (a.k.a. the "local content")
		// For UIElement, those are background and border or shape's content
		// WARNING: As we are overriding the "Render" method, at this point we are still in the parent's coordinate system

		// This shouldn't be inside the if condition.
		// When inside the if, the session.Dispose will be called before base.Render,
		// which can cause clipping to be removed before rendering children.
		using var session = BeginShapesDrawing(in parentSession);

		if (_shapes is { Count: not 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				// TODO: If the shape will end up being not in Window bounds,
				// we should skip rendering it for performance reasons.
				// Maybe get canvas total matrix and multiply that by shape transform
				// Then, use that to transform Rect(0, 0, Size.X, Size.Y)
				// and check if that intersects with Rect(0, 0, RootVisualSize.X, RootVisualSize.Y)
				// This should also done in other parts, e.g, text rendering.
				// NOTE: We probably shouldn't skip a whole Visual as it could have a child that
				// is translated and gets back into the Window bounds.
				// Maybe we could only skip the visual if we know it's clipped, but that won't always the case.
				shapes[i].Render(in session);
			}
		}

		// Second we render the children
		base.Render(in session);
	}

	private DrawingSession BeginShapesDrawing(in DrawingSession parentSession)
	{
		parentSession.Canvas.Save();

		var transform = this.GetTransform().ToSKMatrix();

		if (ViewBox is { } viewBox)
		{
			if (!viewBox.IsAncestorClip)
			{
				ApplyTranslation(parentSession.Canvas);
			}

			// We apply the transformed viewbox clipping
			if (transform.IsIdentity)
			{
				parentSession.Canvas.ClipRect(viewBox.GetSKRect(), antialias: true);
			}
			else
			{
				var shape = new SKPath();
				var clipRect = new SKRect(viewBox.Offset.X, viewBox.Offset.Y, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y);

				shape.AddRect(clipRect);
				if (!viewBox.IsAncestorClip)
				{
					shape.Transform(transform);
				}

				parentSession.Canvas.ClipPath(shape, antialias: true);
			}

			if (viewBox.IsAncestorClip)
			{
				ApplyTranslation(parentSession.Canvas);
			}
		}
		else
		{
			ApplyTranslation(parentSession.Canvas);
		}

		if (!transform.IsIdentity)
		{
			// Applied rending transformation matrix (i.e. change coordinates system to the "rendering" one)
			parentSession.Canvas.Concat(ref transform);
		}

		// Note: Here only the `Clip` is relevant for the shapes, `CornerRadiusClip` applies only for children (i.e. UIElement's content)
		Clip?.Apply(parentSession.Canvas, this);

		var session = parentSession; // Creates a new session (clone the struct)

		DrawingSession.PushOpacity(ref session, Opacity);

		return session;
	}

	private void ApplyTranslation(SKCanvas canvas)
	{
		var totalOffset = this.GetTotalOffset();
		// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
		canvas.Translate(totalOffset.X + AnchorPoint.X, totalOffset.Y + AnchorPoint.Y);
	}

	internal Rect? GetViewBoxRectInElementCoordinateSpace()
	{
		if (ViewBox is null)
		{
			return null;
		}

		var rect = ViewBox.GetRect();
		if (ViewBox.IsAncestorClip)
		{
			var totalOffset = this.GetTotalOffset();
			rect.X -= totalOffset.X;
			rect.Y -= totalOffset.Y;
		}

		return rect;
	}
}
