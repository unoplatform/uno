#nullable enable

using SkiaSharp;
using Uno.Extensions;
using Windows.Foundation;

namespace Windows.UI.Composition;

partial class RectangleClip
{
	private static readonly SKPoint[] _radiiStore = new SKPoint[4];

	private SKRoundRect? _skRoundRect;

	private SKRoundRect GetRect(Visual visual)
	{
		_skRoundRect ??= new SKRoundRect();

		_radiiStore[0] = new SKPoint(_topLeftRadius.X, _topLeftRadius.Y);
		_radiiStore[1] = new SKPoint(_topRightRadius.X, _topRightRadius.Y);
		_radiiStore[2] = new SKPoint(_bottomRightRadius.X, _bottomRightRadius.Y);
		_radiiStore[3] = new SKPoint(_bottomLeftRadius.X, _bottomLeftRadius.Y);

		_skRoundRect.SetRectRadii(GetBounds(visual)!.Value.ToSKRect(), _radiiStore);

		return _skRoundRect;
	}

	private protected override Rect? GetBoundsCore(Visual visual)
		=> new Rect(
			x: Left,
			y: Top,
			width: Right - Left,
			height: Bottom - Top);

	internal override void Apply(SKCanvas canvas, Visual visual)
	{
		canvas.ClipRoundRect(GetRect(visual), SKClipOperation.Intersect, true);
	}
}
