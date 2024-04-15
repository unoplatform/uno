using SkiaSharp;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class InsetClip
{
	internal override Rect? GetBounds(Visual visual)
	{
		return new Rect(
			x: LeftInset,
			y: TopInset,
			width: visual.Size.X - LeftInset - RightInset,
			height: visual.Size.Y - TopInset - BottomInset);
	}

	internal override void Apply(SKCanvas canvas, Visual visual)
	{
		var rect = new SKRect(
			LeftInset,
			TopInset,
			visual.Size.X - RightInset,
			visual.Size.Y - BottomInset);
		canvas.ClipRect(rect, SKClipOperation.Intersect, true);
	}
}
