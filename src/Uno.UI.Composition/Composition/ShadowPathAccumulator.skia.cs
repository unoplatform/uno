#nullable enable

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Uno.UI.Composition.Composition;

/// <summary>
/// Accumulates a drop-shadow silhouette as a single α=1 <see cref="OpaqueSilhouette"/> plus a set of
/// disjoint α&lt;1 <see cref="Regions"/>. Add applies Porter-Duff <c>over</c>: an opaque contribution
/// unions into the opaque silhouette and subtracts itself from the translucent regions (the overlap is
/// now opaque, absorbed by the union); a translucent contribution leaves the opaque silhouette
/// unchanged and splits each translucent region by intersection with the standard
/// <c>new_α = α_in + α_existing × (1 − α_in)</c> rule.
/// </summary>
internal sealed class ShadowPathAccumulator : IDisposable
{
	private readonly List<(SKPath Path, float Alpha)> _regions = new();
	private readonly List<(SKPath Path, float Alpha)> _swap = new();

	// Single α=1 region (or null). By construction never overlaps any entry in _regions.
	private SKPath? _opaqueSilhouette;
	private SKRect _opaqueBounds;

	/// <summary>Translucent (α&lt;1) regions. Disjoint from each other and from <see cref="OpaqueSilhouette"/>.</summary>
	internal IReadOnlyList<(SKPath Path, float Alpha)> Regions => _regions;

	/// <summary>The single α=1 region, or <c>null</c> if no opaque contribution has been made.</summary>
	internal SKPath? OpaqueSilhouette => _opaqueSilhouette;

	/// <summary>Total number of distinct regions held by the accumulator (opaque counts as one).</summary>
	internal int Count => (_opaqueSilhouette is not null ? 1 : 0) + _regions.Count;

	void IDisposable.Dispose()
	{
		foreach (var (path, _) in _regions)
		{
			path.Dispose();
		}

		_regions.Clear();
		_swap.Clear();
		_opaqueSilhouette?.Dispose();
		_opaqueSilhouette = null;
		_opaqueBounds = default;
	}

	/// <summary>
	/// Returns true if <paramref name="candidate"/> is entirely contained in <see cref="OpaqueSilhouette"/>.
	/// Used by the walker to skip visuals whose maximum drawing extent is already absorbed by an opaque
	/// region.
	/// </summary>
	internal bool IsFullyCovered(SKPath candidate)
	{
		if (_opaqueSilhouette is null)
		{
			return false;
		}
		if (candidate.IsEmpty)
		{
			return true;
		}

		if (!_opaqueBounds.Contains(candidate.Bounds))
		{
			return false;
		}

		using var diff = new SKPath();
		if (!candidate.Op(_opaqueSilhouette, SKPathOp.Difference, diff))
		{
			return false;
		}
		return diff.IsEmpty;
	}

	internal void Add(SKPath path, float alpha)
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

	private void AddOpaque(SKPath path)
	{
		// Union the new path into _opaqueSilhouette. Common case (no translucent regions yet) ends here.
		if (_opaqueSilhouette is null)
		{
			_opaqueSilhouette = new SKPath(path);
		}
		else
		{
			_opaqueSilhouette.Op(path, SKPathOp.Union, _opaqueSilhouette);
		}
		_opaqueBounds = _opaqueSilhouette.Bounds;

		if (_regions.Count == 0)
		{
			return;
		}

		// Strip the newly-opaque area out of every translucent region (in place — R is the only
		// reference, and either survives in _swap as its own leftover or is disposed if fully consumed).
		foreach (var (R, alphaR) in _regions)
		{
			R.Op(path, SKPathOp.Difference, R);
			if (R.IsEmpty)
			{
				R.Dispose();
			}
			else
			{
				_swap.Add((R, alphaR));
			}
		}

		_regions.Clear();
		_regions.AddRange(_swap);
		_swap.Clear();
	}

	private void AddTranslucent(SKPath path, float alpha)
	{
		var remainder = new SKPath(path);

		// Areas already covered by the opaque silhouette stay at α=1 (Porter-Duff: α + 1·(1−α) = 1) and
		// don't need to be added anywhere. Strip them from the remainder before processing translucents.
		if (_opaqueSilhouette is not null)
		{
			remainder.Op(_opaqueSilhouette, SKPathOp.Difference, remainder);
			if (remainder.IsEmpty)
			{
				remainder.Dispose();
				return;
			}
		}

		// Split each existing translucent region against the remainder. Both inputs are α<1, so the
		// combined α stays strictly < 1 and never gets promoted into _opaqueSilhouette.
		foreach (var (R, alphaR) in _regions)
		{
			using var intersect = new SKPath();
			if (R.Op(remainder, SKPathOp.Intersect, intersect) && !intersect.IsEmpty)
			{
				var combined = alpha + alphaR * (1f - alpha);
				_swap.Add((new SKPath(intersect), combined));

				// R becomes R - intersect (= R - remainder). We use `intersect` rather than `remainder` so
				// the subsequent `remainder - intersect` step is unaffected by us mutating R here.
				R.Op(intersect, SKPathOp.Difference, R);
				if (R.IsEmpty)
				{
					R.Dispose();
				}
				else
				{
					_swap.Add((R, alphaR));
				}

				// Strip the just-processed area from remainder (= remainder - R via the same identity).
				remainder.Op(intersect, SKPathOp.Difference, remainder);
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
		else
		{
			remainder.Dispose();
		}

		_regions.Clear();
		_regions.AddRange(_swap);
		_swap.Clear();
	}
}
