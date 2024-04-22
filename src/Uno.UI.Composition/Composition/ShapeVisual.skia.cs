#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{
	private protected override void ApplyClipping(in SKCanvas canvas)
	{
		base.ApplyClipping(in canvas);
		if (ViewBox is { } viewBox)
		{
			canvas.ClipRect(viewBox.GetSKRect(), antialias: true);
		}
	}

	/// <inheritdoc />
	internal override void Draw(in DrawingSession session)
	{
		if (_shapes is { Count: not 0 } shapes)
		{
			foreach (var t in shapes)
			{
				t.Render(in session);
			}
		}

		base.Draw(in session);
	}
}
