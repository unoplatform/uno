#nullable enable

using Windows.Foundation;
using SkiaSharp;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{
	/// <inheritdoc />
	internal override void Paint(in PaintingSession session)
	{
		var canvas = session.Canvas;

		if (Size.X == 0 || Size.Y == 0)
		{
			return;
		}

		if (ViewBox is not null)
		{
			canvas.Scale(
				ViewBox.Size.X > 0 ? Size.X / ViewBox.Size.X : 1,
				ViewBox.Size.Y > 0 ? Size.Y / ViewBox.Size.Y : 1);
			canvas.Translate(-ViewBox.Offset.X, -ViewBox.Offset.Y); // translate after scaling
			canvas.ClipRect(new SKRect(0, 0, ViewBox.Size.X, ViewBox.Size.Y));
		}

		canvas.ClipRect(new SKRect(0, 0, Size.X, Size.Y));

		if (_shapes is { Count: not 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				shapes[i].Render(in session);
			}
		}

		base.Paint(in session);
	}

	/// <remarks>This does NOT take the clipping into account.</remarks>
	internal override bool HitTest(Point point)
	{
		if (_shapes is null)
		{
			return false;
		}

		foreach (var shape in _shapes)
		{
			if (shape.HitTest(point))
			{
				return true;
			}
		}

		// Do not check the child visuals. On WinUI, if you add a child visual (e.g. using ContainerVisual.Children.InsertAtTop),
		// the child doesn't factor at all in hit-testing. The children of the UIElement that owns this visual will be checked
		// separately in VisualTreeHelper.HitTest

		return false;
	}
}
