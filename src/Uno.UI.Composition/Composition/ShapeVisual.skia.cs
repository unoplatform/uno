#nullable enable

using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{
	/// <summary>
	/// This is the final transformation matrix from the origin for this Visual. We use this for hit testing.
	/// </summary>
	internal SKMatrix? TotalMatrix { get; private set; }

	internal override void Render(in DrawingSession parentSession)
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
			using var session = BeginShapesDrawing(in parentSession);

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
		base.Render(in parentSession);
	}

	/// <inheritdoc />
	internal override void Draw(in DrawingSession session)
	{
		var canvas = session.Canvas;
		if (ViewBox is { } viewBox)
		{
			canvas.ClipRect(viewBox.GetRect(), antialias: true);
		}

		TotalMatrix = canvas.TotalMatrix;

		base.Draw(in session);
	}

	private DrawingSession BeginShapesDrawing(in DrawingSession parentSession)
	{
		parentSession.Canvas.Save();

		var totalOffset = this.GetTotalOffset();
		// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
		parentSession.Canvas.Translate(totalOffset.X + AnchorPoint.X, totalOffset.Y + AnchorPoint.Y);

		var transform = this.GetTransform().ToSKMatrix();

		if (ViewBox is { } viewBox)
		{
			// We apply the transformed viewbox clipping
			if (transform.IsIdentity)
			{
				parentSession.Canvas.ClipRect(viewBox.GetRect(), antialias: true);
			}
			else
			{
				var shape = new SKPath();
				shape.AddRect(new SKRect(viewBox.Offset.X, viewBox.Offset.Y, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y));
				shape.Transform(transform);
				parentSession.Canvas.ClipPath(shape, antialias: true);
			}
		}

		if (!transform.IsIdentity)
		{
			// Applied rending transformation matrix (i.e. change coordinates system to the "rendering" one)
			parentSession.Canvas.Concat(ref transform);
		}

		// Note: We don't apply the clip here, as it is already applied on the shapes (i.e. CornerRadius)
		//		 The Clip property is only used to apply the clip on the children (i.e. the UIElement's content)
		// Clip?.Apply(parentSession.Surface);

		var session = parentSession; // Creates a new session (clone the struct)

		DrawingSession.PushOpacity(ref session, Opacity);

		return session;
	}
}
