#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class Visual
{
	private SKRect _lastRenderBounds;
	private Matrix4x4 _lastRenderMatrix;
	private bool _hasLastRenderBounds;
	private SKRect _lastClipBounds;

	private bool _subtreeChangedThisFrame;

	internal virtual float DamageRegionSamplingMargin => 0;

	private void ContributeDamageOnPaint(bool contentChanged, DamageRegion? damage, SKRect clipBounds, bool clipChanged)
	{
		if (damage is null)
		{
			return;
		}

		var matrix = TotalMatrix;
		var moved = !_hasLastRenderBounds || matrix != _lastRenderMatrix;

		var shadowSilhouetteChanged = ShadowState is not null && _subtreeChangedThisFrame;

		// The accumulated clip can grow or shrink while this visual's own content and transform stay
		// identical (e.g. an ancestor re-clipping to a new size), revealing or hiding part of it, and nothing
		// else would report that as damage. Two independent signals, because neither covers the other:
		// clipChanged is raised where a Clip/LayoutClip is mutated, so it catches a shape change inside
		// unchanged bounds; the bounds fingerprint catches clips this visual never sees mutate, such as the
		// frame's root clip.
		clipChanged |= clipBounds != _lastClipBounds;
		_lastClipBounds = clipBounds;

		if (!contentChanged && !moved && !clipChanged && !shadowSilhouetteChanged)
		{
			return;
		}

		if (TryGetPaintDamageRegion(clipBounds, out var bounds))
		{
			damage.UnionRect(bounds);

			if (_hasLastRenderBounds && (matrix != _lastRenderMatrix || bounds != _lastRenderBounds))
			{
				damage.UnionRect(_lastRenderBounds);
			}
			_lastRenderBounds = bounds;
			_lastRenderMatrix = matrix;
			_hasLastRenderBounds = true;
		}
		else if (_hasLastRenderBounds)
		{
			damage.UnionRect(_lastRenderBounds);
			_hasLastRenderBounds = false;
		}
	}

	// Rect-only, deliberately: an exact region would cost path booleans per visual per frame, and during a
	// scroll every visual reaches this point. The rect is a superset of the exact region, so it only widens damage.
	private bool TryGetPaintDamageRegion(SKRect clipBounds, out SKRect bounds)
	{
		bounds = default;

		if (clipBounds.Width <= 0 || clipBounds.Height <= 0)
		{
			return false;
		}

		if (!TryGetLocalContentBounds(out var local))
		{
			bounds = clipBounds;
			return true;
		}

		if (local.IsEmpty)
		{
			return false;
		}

		var samplingMargin = DamageRegionSamplingMargin;
		if (samplingMargin > 0)
		{
			local.Inflate(samplingMargin, samplingMargin);
		}

		// Covers antialiasing bleed past the content edge.
		var root = TotalMatrix.ToSKMatrix().MapRect(local);
		root.Inflate(2, 2);
		root = new SKRect(
			(float)Math.Floor(root.Left),
			(float)Math.Floor(root.Top),
			(float)Math.Ceiling(root.Right),
			(float)Math.Ceiling(root.Bottom));

		var clipped = SKRect.Intersect(root, clipBounds);
		if (clipped.Width <= 0 || clipped.Height <= 0)
		{
			return false;
		}

		bounds = clipped;
		return true;
	}

	internal virtual bool TryGetLocalContentBounds(out SKRect localBounds)
	{
		localBounds = default;

		// What this visual paints itself, in local coordinates: nothing for non-painting visuals (containers),
		// its Size when it paints within Size (the same bound WalkShadowSilhouette uses for an own contribution),
		// otherwise it can't be bounded here and we fall back to the clip.
		SKRect ownContent;
		if (!CanPaint())
		{
			ownContent = SKRect.Empty;
		}
		else if (PaintsWithinOwnSize)
		{
			ownContent = new SKRect(0, 0, Math.Max(0f, Size.X), Math.Max(0f, Size.Y));
		}
		else
		{
			return false;
		}

		// A drop shadow's silhouette is this own content unioned with every descendant, then offset and
		// blurred; without a shadow the content is just what this visual paints.
		if (ShadowState is not null)
		{
			return TryGetShadowSilhouetteBounds(ownContent, out localBounds);
		}

		localBounds = ownContent;
		return true;
	}

	private protected bool TryGetShadowSilhouetteBounds(SKRect ownLocalBounds, out SKRect localBounds)
	{
		localBounds = default;

		var casterMatrix = TotalMatrix.ToSKMatrix();
		var silhouetteInRoot = casterMatrix.MapRect(ownLocalBounds);
		if (!TryAccumulateDescendantContentBoundsInRoot(ref silhouetteInRoot))
		{
			return false;
		}

		if (silhouetteInRoot.IsEmpty)
		{
			// Neither the caster nor its descendants paint anything, so there's no silhouette to cast a
			// shadow (and ExpandForShadow would otherwise inflate an empty rect into a spurious region).
			localBounds = SKRect.Empty;
			return true;
		}

		var silhouetteLocal = casterMatrix.TryInvert(out var inverse)
			? inverse.MapRect(silhouetteInRoot)
			: ownLocalBounds;
		localBounds = ExpandForShadow(silhouetteLocal);
		return true;
	}

	private bool TryAccumulateDescendantContentBoundsInRoot(ref SKRect acc)
	{
		foreach (var child in GetChildrenInRenderOrder())
		{
			if (child.Opacity == 0f || !child.IsVisible)
			{
				continue;
			}

			if (!child.TryGetLocalContentBounds(out var childLocal))
			{
				return false;
			}

			if (!childLocal.IsEmpty)
			{
				var rect = child.TotalMatrix.ToSKMatrix().MapRect(childLocal);
				acc = acc.IsEmpty ? rect : SKRect.Union(acc, rect);
			}

			if (child.ShadowState is null && !child.TryAccumulateDescendantContentBoundsInRoot(ref acc))
			{
				return false;
			}
		}

		return true;
	}

	private SKRect ExpandForShadow(SKRect content)
	{
		if (ShadowState is not { } shadow)
		{
			return content;
		}

		var shadowRect = content;
		shadowRect.Offset(shadow.Dx, shadow.Dy);
		shadowRect.Inflate(shadow.SigmaX * 3, shadow.SigmaY * 3);
		return SKRect.Union(content, shadowRect);
	}
}
