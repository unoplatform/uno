#nullable enable

using SkiaSharp;
using Uno.Extensions;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class RectangleClip
{
	private static readonly SKPoint[] _radiiStore = new SKPoint[4];

	private SKRoundRect? _skRoundRect;

	internal SKRoundRect SKRoundRect => GetRect();

	private SKRoundRect GetRect()
	{
		_skRoundRect ??= new SKRoundRect();

		_radiiStore[0] = new SKPoint(_topLeftRadius.X, _topLeftRadius.Y);
		_radiiStore[1] = new SKPoint(_topRightRadius.X, _topRightRadius.Y);
		_radiiStore[2] = new SKPoint(_bottomRightRadius.X, _bottomRightRadius.Y);
		_radiiStore[3] = new SKPoint(_bottomLeftRadius.X, _bottomLeftRadius.Y);

		_skRoundRect.SetRectRadii(GetRectangleBounds().ToSKRect(), _radiiStore);
		return _skRoundRect;
	}

	private protected override Rect? GetBoundsCore(Visual visual)
		=> GetRectangleBounds();

	private Rect GetRectangleBounds()
	{
		return new Rect(
			x: Left,
			y: Top,
			width: Right - Left,
			height: Bottom - Top);
	}

	internal override void Apply(SKCanvas canvas, Visual visual)
	{
		canvas.ClipRoundRect(SKRoundRect, SKClipOperation.Intersect, true);
	}
}
