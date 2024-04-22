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
	internal override void Paint(in PaintingSession session)
	{
		if (_shapes is { Count: not 0 } shapes)
		{
			foreach (var shape in shapes)
			{
				shape.Render(in session);
			}
		}

		base.Paint(in session);
	}

	internal override bool CanPaint => _shapes is { Count: not 0 };
}
