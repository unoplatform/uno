#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.Extensions;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class RectangleClip
{
<<<<<<< HEAD
	private SKRoundRect? _skRoundRect;
	private static readonly SKPath _spareClipPath = new();
=======
	private IGeometry? _clipPath;
>>>>>>> 2f505dc (perf(composition): Cache the rounded-clip path)

	private protected override Rect? GetBoundsCore(Visual visual)
	{
		return new Rect(
			x: Left,
			y: Top,
			width: Right - Left,
			height: Bottom - Top);
	}

	// The path returned here is reused, do not cache
	internal override SKPath GetClipPath(Visual visual)
	{
<<<<<<< HEAD
		var path = _spareClipPath;
		path.Rewind();
		path.AddRoundRect(GetClipRoundedRect(visual));

		return path;
=======
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
>>>>>>> 2f505dc (perf(composition): Cache the rounded-clip path)
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
