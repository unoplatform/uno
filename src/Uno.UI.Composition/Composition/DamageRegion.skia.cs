#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition;

/// <summary>
/// A mutable accumulator for a per-frame damage (dirty) region, expressed on the neutral <see cref="IGeometry"/>
/// seam. This is the backend-neutral analog of the mutable <c>SKPath</c> that used to carry damage: because
/// <see cref="IGeometry"/> is immutable (its <see cref="IGeometry.Combine"/>/<see cref="IGeometry.Transform"/>
/// return fresh geometries), the mutate-in-place role is held here instead, reassigning the wrapped geometry and
/// disposing the superseded one. Any pooling/reuse of the underlying geometries is an implementation concern of
/// the geometry seam, not of this accumulator.
/// </summary>
internal sealed class DamageRegion : IDisposable
{
	private IGeometry? _region;

	/// <summary>The accumulated region, or null when nothing has been contributed yet.</summary>
	internal IGeometry? Geometry => _region;

	internal bool IsEmpty => _region is null || _region.IsEmpty;

	/// <summary>Unions <paramref name="addition"/> into the region. The addition is copied, never adopted.</summary>
	internal void Union(IGeometry addition)
	{
		if (addition.IsEmpty)
		{
			return;
		}

		if (_region is null)
		{
			// Copy: the caller keeps ownership of `addition` (e.g. a visual's reused own-content path).
			_region = addition.Transform(Matrix3x2.Identity);
		}
		else
		{
			var previous = _region;
			_region = _region.Combine(addition, GeometryCombineMode.Union);
			previous.Dispose();
		}
	}

	internal void UnionRect(Rect rect)
	{
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}

		using var scratch = GeometryFactory.Current.CreateRectangleGeometry(rect);
		Union(scratch);
	}

	/// <summary>Intersects the region with <paramref name="frameRect"/> (drops everything outside the frame).</summary>
	internal void ClampTo(Rect frameRect)
	{
		if (_region is null || _region.IsEmpty || Contains(frameRect, _region.Bounds))
		{
			return;
		}

		using var scratch = GeometryFactory.Current.CreateRectangleGeometry(frameRect);
		var previous = _region;
		_region = _region.Combine(scratch, GeometryCombineMode.Intersect);
		previous.Dispose();
	}

	/// <summary>Unions another accumulator's region into this one (used to fold carried-over damage forward).</summary>
	internal void Union(DamageRegion other)
	{
		if (other._region is { IsEmpty: false } region)
		{
			Union(region);
		}
	}

	/// <summary>Detaches the accumulated region, leaving this accumulator empty. The caller owns the result.</summary>
	internal IGeometry? Detach()
	{
		var region = _region;
		_region = null;
		return region;
	}

	internal void Reset()
	{
		_region?.Dispose();
		_region = null;
	}

	public void Dispose() => Reset();

	private static bool Contains(Rect outer, Rect inner)
		=> inner.Left >= outer.Left && inner.Top >= outer.Top && inner.Right <= outer.Right && inner.Bottom <= outer.Bottom;
}
