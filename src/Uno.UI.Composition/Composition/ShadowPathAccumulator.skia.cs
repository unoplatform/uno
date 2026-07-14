#nullable enable

using System.Collections.Generic;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Uno.UI.Composition.Composition;

/// <summary>
/// Accumulates a drop-shadow silhouette as a single α=1 <see cref="OpaqueSilhouette"/> plus a set of
/// disjoint α&lt;1 <see cref="Regions"/>. Add applies Porter-Duff <c>over</c>: an opaque contribution
/// unions into the opaque silhouette and subtracts itself from the translucent regions (the overlap is
/// now opaque, absorbed by the union); a translucent contribution leaves the opaque silhouette
/// unchanged and splits each translucent region by intersection with the standard
/// <c>new_α = α_in + α_existing × (1 − α_in)</c> rule.
/// </summary>
/// <remarks>
/// Geometries are immutable <see cref="IGeometry"/> handles; combining produces new geometries rather
/// than mutating in place, and the intermediates are reclaimed by the GC (the Skia backend's native
/// path has a finalizer), so the accumulator holds no unmanaged resources of its own.
/// </remarks>
internal sealed class ShadowPathAccumulator
{
	private readonly List<(IGeometry Path, float Alpha)> _regions = new();
	private readonly List<(IGeometry Path, float Alpha)> _swap = new();

	// Single α=1 region (or null). By construction never overlaps any entry in _regions.
	private IGeometry? _opaqueSilhouette;
	private Rect _opaqueBounds;

	/// <summary>Translucent (α&lt;1) regions. Disjoint from each other and from <see cref="OpaqueSilhouette"/>.</summary>
	internal IReadOnlyList<(IGeometry Path, float Alpha)> Regions => _regions;

	/// <summary>The single α=1 region, or <c>null</c> if no opaque contribution has been made.</summary>
	internal IGeometry? OpaqueSilhouette => _opaqueSilhouette;

	/// <summary>Total number of distinct regions held by the accumulator (opaque counts as one).</summary>
	internal int Count => (_opaqueSilhouette is not null ? 1 : 0) + _regions.Count;

	/// <summary>
	/// Returns true if <paramref name="candidate"/> is entirely contained in <see cref="OpaqueSilhouette"/>.
	/// Used by the walker to skip visuals whose maximum drawing extent is already absorbed by an opaque
	/// region.
	/// </summary>
	internal bool IsFullyCovered(IGeometry candidate)
	{
		if (_opaqueSilhouette is null)
		{
			return false;
		}
		if (candidate.IsEmpty)
		{
			return true;
		}

		if (!ContainsRect(_opaqueBounds, candidate.Bounds))
		{
			return false;
		}

		return candidate.Combine(_opaqueSilhouette, GeometryCombineMode.Difference).IsEmpty;
	}

	internal void Add(IGeometry path, float alpha)
	{
		if (alpha <= 0f || path.IsEmpty)
		{
			return;
		}

		if (alpha >= 1f)
		{
			AddOpaque(path);
		}
		else
		{
			AddTranslucent(path, alpha);
		}
	}

	private void AddOpaque(IGeometry path)
	{
		// Union the new path into _opaqueSilhouette. Common case (no translucent regions yet) ends here.
		_opaqueSilhouette = _opaqueSilhouette is null
			? path
			: _opaqueSilhouette.Combine(path, GeometryCombineMode.Union);
		_opaqueBounds = _opaqueSilhouette.Bounds;

		if (_regions.Count == 0)
		{
			return;
		}

		// Strip the newly-opaque area out of every translucent region; fully-consumed regions drop out.
		foreach (var (R, alphaR) in _regions)
		{
			var leftover = R.Combine(path, GeometryCombineMode.Difference);
			if (!leftover.IsEmpty)
			{
				_swap.Add((leftover, alphaR));
			}
		}

		_regions.Clear();
		_regions.AddRange(_swap);
		_swap.Clear();
	}

	private void AddTranslucent(IGeometry path, float alpha)
	{
		var remainder = path;

		// Areas already covered by the opaque silhouette stay at α=1 (Porter-Duff: α + 1·(1−α) = 1) and
		// don't need to be added anywhere. Strip them from the remainder before processing translucents.
		if (_opaqueSilhouette is not null)
		{
			remainder = remainder.Combine(_opaqueSilhouette, GeometryCombineMode.Difference);
			if (remainder.IsEmpty)
			{
				return;
			}
		}

		// Split each existing translucent region against the remainder. Both inputs are α<1, so the
		// combined α stays strictly < 1 and never gets promoted into _opaqueSilhouette.
		foreach (var (R, alphaR) in _regions)
		{
			var intersect = R.Combine(remainder, GeometryCombineMode.Intersect);
			if (!intersect.IsEmpty)
			{
				var combined = alpha + alphaR * (1f - alpha);
				_swap.Add((intersect, combined));

				// R becomes R - intersect (= R - remainder). We use `intersect` rather than `remainder` so
				// the subsequent `remainder - intersect` step is unaffected.
				var rLeftover = R.Combine(intersect, GeometryCombineMode.Difference);
				if (!rLeftover.IsEmpty)
				{
					_swap.Add((rLeftover, alphaR));
				}

				// Strip the just-processed area from remainder (= remainder - R via the same identity).
				remainder = remainder.Combine(intersect, GeometryCombineMode.Difference);
			}
			else
			{
				_swap.Add((R, alphaR));
			}
		}

		if (!remainder.IsEmpty)
		{
			_swap.Add((remainder, alpha));
		}

		_regions.Clear();
		_regions.AddRange(_swap);
		_swap.Clear();
	}

	private static bool ContainsRect(Rect outer, Rect inner)
		=> inner.Left >= outer.Left && inner.Top >= outer.Top
			&& inner.Right <= outer.Right && inner.Bottom <= outer.Bottom;
}
