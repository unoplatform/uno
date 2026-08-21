#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition;

/// <summary>
/// A mutable accumulator for a per-frame damage (dirty) region. Damage is conservative, so contributions are
/// tracked as a bounded list of bounding rects — never as incremental geometry booleans: a per-contribution
/// <see cref="IGeometry.Combine"/> over a growing region is O(region²) per frame and dominates the frame on
/// engines whose Combine is not trivially cheap (the managed engine spends most of a scrolled frame there).
/// One union geometry is materialized per frame at <see cref="Detach"/>, from at most <see cref="MaxRects"/> rects.
/// </summary>
internal sealed class DamageRegion : IDisposable
{
	// Past this many distinct dirty rects the frame is effectively a full repaint anyway; collapse to bounds.
	private const int MaxRects = 16;

	private readonly List<Rect> _rects = new();

	internal bool IsEmpty => _rects.Count == 0;

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
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}

		for (var i = 0; i < _rects.Count; i++)
		{
			if (Contains(_rects[i], rect))
			{
				return;
			}
		}

		_rects.Add(rect);
		if (_rects.Count > MaxRects)
		{
			var bounds = _rects[0];
			for (var i = 1; i < _rects.Count; i++)
			{
				bounds.Union(_rects[i]);
			}

			_rects.Clear();
			_rects.Add(bounds);
		}
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
	}

	/// <summary>Unions another accumulator's region into this one (used to fold carried-over damage forward).</summary>
	internal void Union(DamageRegion other)
	{
		foreach (var r in other._rects)
		{
			UnionRect(r);
		}
	}

	/// <summary>Materializes and detaches the accumulated region, leaving this accumulator empty. The caller owns the result.</summary>
	internal IGeometry? Detach()
	{
		if (_rects.Count == 0)
		{
			return null;
		}

		var region = GeometryFactory.Current.CreateRectangleGeometry(_rects[0]);
		for (var i = 1; i < _rects.Count; i++)
		{
			using var rect = GeometryFactory.Current.CreateRectangleGeometry(_rects[i]);
			var previous = region;
			region = previous.Combine(rect, GeometryCombineMode.Union);
			previous.Dispose();
		}

		_rects.Clear();
		return region;
	}

	internal void Reset() => _rects.Clear();

	public void Dispose() => Reset();

	private static bool Contains(Rect outer, Rect inner)
		=> inner.Left >= outer.Left && inner.Top >= outer.Top && inner.Right <= outer.Right && inner.Bottom <= outer.Bottom;
}
