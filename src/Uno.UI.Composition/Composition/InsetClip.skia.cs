using SkiaSharp;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

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

	internal override SKPath GetClipPath(Visual visual)
	{
		var path = new SKPath();
		var rect = GetBounds(visual).Value.ToSKRect();
		path.AddRect(rect);
		return path;
	}
}
