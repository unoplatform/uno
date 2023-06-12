#nullable enable

using System.Numerics;
using Windows.Foundation;
using SkiaSharp;
using Uno.Extensions;
using System.Xml.Xsl;
using Uno.UI.Composition;

namespace Windows.UI.Composition;

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
		if (_shapes is { Count: not 0 } shapes)
		{
			using var session = BeginShapesDrawing(in parentSession);

			for (var i = 0; i < shapes.Count; i++)
			{
				var shape = shapes[i];
				var shapeTransform = shape.GetTransform();

				if (shapeTransform.IsIdentity)
				{
					shape.Draw(in session);
				}
				else
				{
					var shapeTransformMatrix = shapeTransform.ToSKMatrix();

					session.Surface.Canvas.Save();
					session.Surface.Canvas.Concat(ref shapeTransformMatrix);

					shape.Draw(in session);

					session.Surface.Canvas.Restore();
				}
			}
		}

		// Second we render the children
		base.Render(in parentSession);
	}

	/// <inheritdoc />
	private protected override void Draw(in DrawingSession session)
	{
		if (ViewBox is { } viewBox)
		{
			session.Surface.Canvas.ClipRect(new SKRect(viewBox.Offset.X, viewBox.Offset.Y, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y));
		}

		base.Draw(in session);
	}

	private DrawingSession BeginShapesDrawing(in DrawingSession parentSession)
	{
		parentSession.Surface.Canvas.Save();

		// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
		parentSession.Surface.Canvas.Translate(Offset.X + AnchorPoint.X, Offset.Y + AnchorPoint.Y);

		var transform = this.GetTransform().ToSKMatrix();

		if (ViewBox is { } viewBox)
		{
			// We apply the transformed viewbox clipping
			if (transform.IsIdentity)
			{
				parentSession.Surface.Canvas.ClipRect(new SKRect(viewBox.Offset.X, viewBox.Offset.Y, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y));
			}
			else
			{
				var shape = new SKPath();
				shape.AddRect(new SKRect(0, 0, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y));
				shape.Transform(transform);
				parentSession.Surface.Canvas.ClipPath(shape, antialias: true);
			}
		}

		if (!transform.IsIdentity)
		{
			// Applied rending transformation matrix (i.e. change coordinates system to the "rendering" one)
			parentSession.Surface.Canvas.Concat(ref transform);
		}

		Clip?.Apply(parentSession.Surface);

		var session = parentSession; // Creates a new session (clone the struct)

		DrawingSession.PushOpacity(ref session, Opacity);

		return session;
	}
}
