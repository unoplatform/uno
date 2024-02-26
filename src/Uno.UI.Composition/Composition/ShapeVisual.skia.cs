#nullable enable

using System.Numerics;
using Windows.Foundation;
using SkiaSharp;
using Uno.Extensions;
using System.Xml.Xsl;
using Uno.UI.Composition;

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
		if (_shapes is { Count: not 0 } shapes)
		{
			using var session = BeginShapesDrawing(in parentSession);

			for (var i = 0; i < shapes.Count; i++)
			{
				shapes[i].Render(in session);
			}
		}

		// Second we render the children
		base.Render(in parentSession);
	}

	/// <inheritdoc />
	internal override void Draw(in DrawingSession session)
	{
		if (ViewBox is { } viewBox)
		{
			session.Surface.Canvas.ClipRect(viewBox.GetRect(), antialias: true);
		}

		base.Draw(in session);
	}

	private DrawingSession BeginShapesDrawing(in DrawingSession parentSession)
	{
		parentSession.Surface.Canvas.Save();

		var totalOffset = this.GetTotalOffset();
		// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
		parentSession.Surface.Canvas.Translate(totalOffset.X + AnchorPoint.X, totalOffset.Y + AnchorPoint.Y);

		var transform = this.GetTransform().ToSKMatrix();

		if (ViewBox is { } viewBox)
		{
			// We apply the transformed viewbox clipping
			if (transform.IsIdentity)
			{
				parentSession.Surface.Canvas.ClipRect(viewBox.GetRect(), antialias: true);
			}
			else
			{
				var shape = new SKPath();
				shape.AddRect(new SKRect(viewBox.Offset.X, viewBox.Offset.Y, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y));
				shape.Transform(transform);
				parentSession.Surface.Canvas.ClipPath(shape, antialias: true);
			}
		}

		if (!transform.IsIdentity)
		{
			// Applied rending transformation matrix (i.e. change coordinates system to the "rendering" one)
			parentSession.Surface.Canvas.Concat(ref transform);
		}

		// Note: We don't apply the clip here, as it is already applied on the shapes (i.e. CornerRadius)
		//		 The Clip property is only used to apply the clip on the children (i.e. the UIElement's content)
		// Clip?.Apply(parentSession.Surface);

		var session = parentSession; // Creates a new session (clone the struct)

		DrawingSession.PushOpacity(ref session, Opacity);

		return session;
	}
}
