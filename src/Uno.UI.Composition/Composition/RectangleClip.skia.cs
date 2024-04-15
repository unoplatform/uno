#nullable enable

using SkiaSharp;
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

		_skRoundRect.SetRectRadii(
			new SKRect(
				Left,
				Top,
				Right,
				Bottom),
			_radiiStore);
		return _skRoundRect;
	}

	internal override Rect? GetBounds(Visual visual)
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
