#nullable enable

using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class RectangleClip
{
	private (RoundRectangle rect, IGeometry path)? _clipPath;

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
		if (GetClipRoundedRect(visual) is not { } rect)
		{
			return null;
		}

		// This runs for every rounded visual on every frame, so only rebuild the path when its geometry changes.
		if (_clipPath is null || _clipPath.Value.rect != rect)
		{
			_clipPath?.path.Release();
			var builder = GeometryFactory.Current.CreatePrimitiveGeometryBuilder();
			builder.AddRoundedRectangle(rect.Rect, rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft);
			_clipPath = (rect, builder.Build());
		}

		// The cache keeps its own reference, so hand the caller one of theirs.
		_clipPath.Value.path.AddRef();
		return _clipPath.Value.path;
	}

	private protected override void DisposeInternal()
	{
		_clipPath?.path.Release();
		_clipPath = null;
		base.DisposeInternal();
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
