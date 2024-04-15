#nullable enable

using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{
	/// <inheritdoc />
	internal override void Draw(in DrawingSession session)
	{
		// Note that Visual.Clip is already applied in Visual.Render -> Visual.BeginDrawing

		if (ViewBox is { } viewBox)
		{
			session.Canvas.ClipRect(viewBox.GetSKRect(), antialias: true);
		}

		if (_shapes is { Count: not 0 } shapes)
		{
			foreach (var t in shapes)
			{
				t.Render(in session);
			}
		}

		// The CornerRadiusClip doesn't affect the shapes of the ShapeVisual, only its children
		CornerRadiusClip?.Apply(session.Canvas, this);

		base.Draw(in session);
	}
}
