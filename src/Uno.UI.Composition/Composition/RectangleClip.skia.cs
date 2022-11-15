#nullable enable

using SkiaSharp;

namespace Windows.UI.Composition;

partial class RectangleClip
{
	private SKRoundRect _skRoundRect;
	private SKPoint[] _radii = new SKPoint[4];

	internal SKRoundRect SKRoundRect => GetRect();

	private void GetRect()
	{
		_skRoundRect ??= new SKRoundRect();
		_skRoundRect.SetRectRadii(
			new SKRect(
				Left,
				Top,
				Right,
				Bottom),
			_radii);
		return _skRoundRect;
	}
}
