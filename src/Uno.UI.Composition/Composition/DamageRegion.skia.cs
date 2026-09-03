#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition;

/// <summary>
/// A mutable accumulator for a per-frame damage (dirty) region.
/// </summary>
/// <remarks>
/// Damage is conservative, so contributions are tracked as bounding rects — never as incremental geometry
/// booleans: a per-contribution <see cref="IGeometry.Combine"/> over a growing region is O(region²) per frame and
/// dominates the frame on engines whose Combine is not trivially cheap. One union geometry is materialized per
/// frame at <see cref="Detach"/>, from at most <see cref="MaxRects"/> rects.
/// </remarks>
internal sealed class DamageRegion : IDisposable
{
	// Past this many distinct dirty rects the frame is effectively a full repaint anyway; collapse to bounds.
	private const int MaxRects = 16;

	// Finished rects. The one still being grown is _open, which joins them only when the region is materialized.
	private readonly List<Rect> _rects = new();
	private Rect _open;
	private Rect _allBounds;
	private bool _hasOpen;

	internal bool IsEmpty => !_hasOpen && _rects.Count == 0;

	/// <summary>Unions <paramref name="addition"/>'s bounds into the region (damage is conservative).</summary>
	internal void Union(IGeometry addition)
	{
		if (!addition.IsEmpty)
		{
			UnionRect(addition.Bounds);
		}
	}

	internal void UnionRect(Rect rect)
	{
		// A zero-area or inverted rect must not reach the bookkeeping below: Rect.Union would drop it while the
		// bounds tracking would keep it, leaving _allBounds no longer covering the rects.
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}

		if (!_hasOpen)
		{
			_open = _allBounds = rect;
			_hasOpen = true;
			return;
		}

		_allBounds.Union(rect);

		// Growing the open rect keeps the region closer to a single analytic rect, but it also damages everything
		// between that rect and this contribution. Only worth it while it costs no more than this contribution
		// already does — true of a scroll, where visuals arrive in tree order and land in the same port, and false
		// for damage elsewhere, which opens its own rect rather than fusing the two.
		var grown = _open;
		grown.Union(rect);
		if (Area(grown) <= Area(_open) + Area(rect))
		{
			_open = grown;
			return;
		}

		if (_rects.Count >= MaxRects)
		{
			_rects.Clear();
			_open = _allBounds;
			return;
		}

		_rects.Add(_open);
		_open = rect;
	}

	/// <summary>Intersects the region with <paramref name="frameRect"/> (drops everything outside the frame).</summary>
	internal void ClampTo(Rect frameRect)
	{
		for (var i = _rects.Count - 1; i >= 0; i--)
		{
			var r = _rects[i];
			r.Intersect(frameRect);
			if (r.IsEmpty || r.Width <= 0 || r.Height <= 0)
			{
				_rects.RemoveAt(i);
			}
			else
			{
				_rects[i] = r;
			}
		}

		if (_hasOpen)
		{
			var open = _open;
			open.Intersect(frameRect);
			if (open.IsEmpty || open.Width <= 0 || open.Height <= 0)
			{
				_hasOpen = false;
				if (_rects.Count > 0)
				{
					_open = _rects[^1];
					_rects.RemoveAt(_rects.Count - 1);
					_hasOpen = true;
				}
			}
			else
			{
				_open = open;
			}
		}
	}

	/// <summary>Unions another accumulator's region into this one (used to fold carried-over damage forward).</summary>
	internal void Union(DamageRegion other)
	{
		foreach (var r in other._rects)
		{
			UnionRect(r);
		}

		if (other._hasOpen)
		{
			UnionRect(other._open);
		}
	}

	/// <summary>Materializes and detaches the accumulated region, leaving this accumulator empty. The caller owns the result.</summary>
	internal IGeometry? Detach()
	{
		if (IsEmpty)
		{
			return null;
		}

		var all = new List<Rect>(_rects.Count + 1);
		all.AddRange(_rects);
		if (_hasOpen)
		{
			all.Add(_open);
		}

		var region = GeometryFactory.Current.CreateRectangleGeometry(all[0]);
		for (var i = 1; i < all.Count; i++)
		{
			using var rect = GeometryFactory.Current.CreateRectangleGeometry(all[i]);
			var previous = region;
			region = previous.Combine(rect, GeometryCombineMode.Union);
			previous.Dispose();
		}

		Reset();
		return region;
	}

	internal void Reset()
	{
		_rects.Clear();
		_open = default;
		_allBounds = default;
		_hasOpen = false;
	}

	public void Dispose() => Reset();

	private static double Area(Rect r) => r.Width * r.Height;
}
