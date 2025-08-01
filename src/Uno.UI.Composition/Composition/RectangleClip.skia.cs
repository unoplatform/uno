#nullable enable

using System.Numerics;
using SkiaSharp;
using Uno.Extensions;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class RectangleClip
{
	private static readonly SKPoint[] _radiiStore = new SKPoint[4];

	private SKRoundRect? _skRoundRect;

	private protected override Rect? GetBoundsCore(Visual visual)
	{
		return new Rect(
			x: Left,
			y: Top,
			width: Right - Left,
			height: Bottom - Top);
	}

	internal override SKPath GetClipPath(Visual visual)
	{
		var path = new SKPath();
		path.AddRoundRect(GetClipRoundedRect(visual));
		return path;
	}

	private protected override SKRect? GetClipRect(Visual visual)
	{
		if (_topLeftRadius.X is 0 && _topLeftRadius.Y is 0 &&
			_topRightRadius.X is 0 && _topRightRadius.Y is 0 &&
			_bottomLeftRadius.X is 0 && _bottomLeftRadius.Y is 0 &&
			_bottomRightRadius.X is 0 && _bottomRightRadius.Y is 0)
		{
			return GetBounds(visual)?.ToSKRect();
		}
		else
		{
			return null;
		}
	}

	private protected override SKRoundRect? GetClipRoundedRect(Visual visual)
	{
		if (GetBounds(visual) is { } bounds)
		{
			_skRoundRect ??= new SKRoundRect();

			_radiiStore[0] = new SKPoint(_topLeftRadius.X, _topLeftRadius.Y);
			_radiiStore[1] = new SKPoint(_topRightRadius.X, _topRightRadius.Y);
			_radiiStore[2] = new SKPoint(_bottomRightRadius.X, _bottomRightRadius.Y);
			_radiiStore[3] = new SKPoint(_bottomLeftRadius.X, _bottomLeftRadius.Y);

			_skRoundRect.SetRectRadii(bounds.ToSKRect(), _radiiStore);
			return _skRoundRect;
		}
		else
		{
			return null;
		}
	}
}
