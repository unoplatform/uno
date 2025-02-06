using SkiaSharp;
using Windows.Foundation;

namespace Windows.UI.Composition;

partial class InsetClip
{
	private protected override Rect? GetBoundsCore(Visual visual)
	{
		return new Rect(
			x: LeftInset,
			y: TopInset,
			width: visual.Size.X - LeftInset - RightInset,
			height: visual.Size.Y - TopInset - BottomInset);
	}

	internal override void Apply(SKCanvas canvas, Visual visual)
	{
		var rect = GetBounds(visual).Value.ToSKRect();
		canvas.ClipRect(rect, SKClipOperation.Intersect, true);
	}
}
