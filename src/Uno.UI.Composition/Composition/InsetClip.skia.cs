using SkiaSharp;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class InsetClip
{
	private (Rect? bounds, SKPath path)? _clipPath;

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
		if (GetBounds(visual) is not { } bounds)
		{
			return null;
		}
		if (_clipPath is null || _clipPath.Value.bounds != bounds)
		{
			var path = new SKPath();
			var rect = bounds.ToSKRect();
			path.AddRect(rect);
			_clipPath = (bounds, path);
		}
		return _clipPath.Value.path;
	}

	private protected override SKRect? GetClipRect(Visual visual)
	{
		return GetBounds(visual)?.ToSKRect();
	}
}
