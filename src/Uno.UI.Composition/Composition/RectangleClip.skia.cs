#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class RectangleClip
{
	private SKRoundRect? _skRoundRect;

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

		var builder = DrawingBackend.Current.CreatePathBuilder();
		builder.AddRoundedRectangle(bounds, _topLeftRadius, _topRightRadius, _bottomRightRadius, _bottomLeftRadius);
		return builder.Build();
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

			Span<SKPoint> radii = stackalloc SKPoint[]
			{
				new SKPoint(_topLeftRadius.X, _topLeftRadius.Y),
				new SKPoint(_topRightRadius.X, _topRightRadius.Y),
				new SKPoint(_bottomRightRadius.X, _bottomRightRadius.Y),
				new SKPoint(_bottomLeftRadius.X, _bottomLeftRadius.Y),
			};

			_skRoundRect.SetRectRadii(bounds.ToSKRect(), radii);

			return _skRoundRect;
		}
		else
		{
			return null;
		}
	}
}
