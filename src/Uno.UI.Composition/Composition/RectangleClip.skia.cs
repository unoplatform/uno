#nullable enable

using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class RectangleClip
{
	private IGeometry? _clipPath;

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
		// This runs for every rounded visual on every frame. The path depends only on this clip's own
		// properties -- GetBoundsCore ignores the visual -- so build it once and let OnPropertyChangedCore
		// drop it when any of them changes.
		if (_clipPath is null)
		{
			if (GetBounds(visual) is not { } bounds)
			{
				return null;
			}

			var builder = GeometryFactory.Current.CreatePrimitiveGeometryBuilder();
			builder.AddRoundedRectangle(bounds, _topLeftRadius, _topRightRadius, _bottomRightRadius, _bottomLeftRadius);
			_clipPath = builder.Build();
		}

		// The cache keeps its own reference, so hand the caller one of theirs.
		_clipPath.AddRef();
		return _clipPath;
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		_clipPath?.Release();
		_clipPath = null;
		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);
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
