#nullable enable

using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class RectangleClip
{
	private protected override Rect? GetBoundsCore(Visual visual)
	{
		return new Rect(
			x: Left,
			y: Top,
			width: Right - Left,
			height: Bottom - Top);
	}

	internal override IGeometry? GetClipPath(Visual visual)
	{
		if (GetBounds(visual) is not { } bounds)
		{
			return null;
		}

		var builder = DrawingBackend.Current.CreatePrimitiveGeometryBuilder();
		builder.AddRoundedRectangle(bounds, _topLeftRadius, _topRightRadius, _bottomRightRadius, _bottomLeftRadius);
		return builder.Build();
	}

	private protected override Rect? GetClipRect(Visual visual)
	{
		if (_topLeftRadius.X is 0 && _topLeftRadius.Y is 0 &&
			_topRightRadius.X is 0 && _topRightRadius.Y is 0 &&
			_bottomLeftRadius.X is 0 && _bottomLeftRadius.Y is 0 &&
			_bottomRightRadius.X is 0 && _bottomRightRadius.Y is 0)
		{
			return GetBounds(visual);
		}
		else
		{
			return null;
		}
	}

	private protected override RoundRectangle? GetClipRoundedRect(Visual visual)
	{
		if (GetBounds(visual) is { } bounds)
		{
			return new RoundRectangle
			{
				Rect = bounds,
				TopLeft = _topLeftRadius,
				TopRight = _topRightRadius,
				BottomRight = _bottomRightRadius,
				BottomLeft = _bottomLeftRadius,
			};
		}
		else
		{
			return null;
		}
	}
}
