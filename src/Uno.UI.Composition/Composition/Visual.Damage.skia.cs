#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.Disposables;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

// Damage-region rendering: each visual contributes the root-space region it (re)paints to the per-frame
// damage accumulator, so the renderer can clip the present to only the changed pixels (output identical to
// a full repaint). See CompositionTarget.Rendering.skia.cs for how the accumulated region is presented.
public partial class Visual
{
	// The root-space bounds this visual occupied the last time it was drawn, and the TotalMatrix it was
	// drawn with, used to add the vacated region to the damage region when it moves/resizes (e.g. scrolling)
	// even when its cached picture is reused without repainting. Only used for damage region.
	private SKRect _lastRenderBounds;
	private Matrix4x4 _lastRenderMatrix;
	private bool _hasLastRenderBounds;

	// True when a descendant changed this frame (captured in Render before the flag is cleared). A drop-shadow
	// caster's shadow silhouette includes its descendants, so it must re-damage its shadow region when the
	// subtree changes — even if the caster's own visual is neither repainted nor moved.
	private bool _subtreeChangedThisFrame;

	// Called during the render walk when this visual actually (re)paints its own content. Adds both the
	// region it now occupies (new bounds) and the region it occupied in the previous frame (old bounds,
	// to erase vacated pixels on a move/resize) to the composition target's per-frame damage region.
	// Bounds are computed here — not at invalidation time — because the visual's Size/matrix are only
	// final during the render walk. The renderer decides whether the accumulated damage is actually used
	// to clip the present (only renderers whose surface retains the previous frame do so).
	private void ContributeDamageOnPaint(bool contentChanged, SKPath? damage)
	{
		// No accumulator means this is an off-screen render (RenderTargetBitmap, visual surface) that doesn't
		// track damage; the on-screen pass threads its accumulator through the PaintingSession.
		if (damage is null)
		{
			return;
		}

		// A visual contributes damage when its content changed (it repainted) OR when it merely moved — its
		// transform changed since the last frame, e.g. while scrolling — even though its cached picture is
		// reused. An unchanged picture drawn at the same place needs no repaint, which is the whole point of
		// damage region, so skip it cheaply (just a matrix comparison).
		var matrix = TotalMatrix;
		var moved = !_hasLastRenderBounds || matrix != _lastRenderMatrix;

		// A drop-shadow caster also re-damages its shadow region when its subtree changed: the shadow's
		// silhouette is the union of what this visual AND its descendants draw (both the analytic and the
		// picture shadow paths walk the whole subtree), so a descendant moving/resizing changes the shadow —
		// which is offset and blurred beyond the descendant's own damaged bounds, so the descendant's own
		// damage doesn't cover it — even though this visual itself didn't repaint or move.
		var shadowSilhouetteChanged = ShadowState is not null && _subtreeChangedThisFrame;

		if (!contentChanged && !moved && !shadowSilhouetteChanged)
		{
			return;
		}

		if (TryGetPaintDamageRegion(out var bounds, out var regionPath))
		{
			if (regionPath is not null)
			{
				damage.Union(regionPath);
				_pathPool.Free(regionPath);
			}
			else
			{
				damage.UnionRect(bounds);
			}

			// Erase the region this visual vacated — but only when it actually moved or resized. The bounding
			// rect is a safe (if loose) cover for a curved old region. Adding it on an in-place repaint (same
			// matrix and bounds) would be redundant AND would rectangularize an otherwise-rounded region, so it
			// is skipped: the new region already covers everything the unchanged old one did.
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
			// The visual no longer has paintable bounds (e.g. collapsed to zero size); repaint where it was.
			damage.UnionRect(_lastRenderBounds);
			_hasLastRenderBounds = false;
		}
	}

	// Computes the damage region (in root/logical coordinates) for this visual's own paint. The damage region
	// is the intersection of two things: the clip in effect when the visual draws (the region it is *allowed*
	// to draw into, computed via GetTotalClipPath like native-element clipping — and which can be any curve,
	// not just a rectangle), and the bounds of what it *actually* paints (its content). The clip alone
	// over-damages — an element's clip is often the whole ScrollViewer viewport or the infinite clip, while
	// it only paints a small icon/line/glyph. When the content extent can't be bounded tighter than the clip
	// (e.g. a drop shadow that paints beyond the visual), we fall back to the clip itself.
	//
	// Outputs a plain rectangle in the common case (rectangular clip), or, when the clip is non-rectangular,
	// a path (rented from <see cref="_pathPool"/> — the caller must free it). Returns false when the visual
	// paints nothing of its own or is fully clipped out.
	private bool TryGetPaintDamageRegion(out SKRect bounds, out SKPath? regionPath)
	{
		bounds = default;
		regionPath = null;

		var clipPath = _pathPool.Allocate();
		var contentPath = _pathPool.Allocate();
		var keepClipPath = false;
		var keepContentPath = false;
		try
		{
			clipPath.Rewind();
			// skipPostPaintingClipping: true — a visual's own post-painting clip only affects its children.
			GetTotalClipPath(clipPath, skipPostPaintingClipping: true);
			if (clipPath.IsEmpty)
			{
				return false;
			}

			var clipIsRect = clipPath.IsRect;
			var clipRect = clipPath.Bounds;

			// Non-rectangular content (a rounded border, an ellipse, an arbitrary shape): damage the actual
			// painted shape rather than its bounding box. Skipped when a shadow or backdrop-sampling margin is
			// involved — those expand a rectangular silhouette instead.
			if (ShadowState is null && DamageRegionSamplingMargin == 0)
			{
				contentPath.Rewind();
				if (TryGetLocalContentPath(contentPath) && !contentPath.IsEmpty)
				{
					contentPath.Transform(TotalMatrix.ToSKMatrix());
					OutsetForAntialiasing(contentPath);
					contentPath.Op(clipPath, SKPathOp.Intersect, contentPath);
					if (contentPath.IsEmpty)
					{
						return false;
					}
					bounds = contentPath.Bounds;
					regionPath = contentPath;
					keepContentPath = true;
					return true;
				}
			}

			if (TryGetLocalContentBounds(out var local))
			{
				if (local.IsEmpty)
				{
					return false;
				}

				// Expand by the backdrop-sampling margin (e.g. a blur radius): the effect reads the surface this
				// far beyond its own bounds, so that ring must be kept fresh or the blur samples stale pixels.
				var samplingMargin = DamageRegionSamplingMargin;
				if (samplingMargin > 0)
				{
					local.Inflate(samplingMargin, samplingMargin);
				}

				var root = TotalMatrix.ToSKMatrix().MapRect(local);
				// Absorb antialiasing bleed and sub-pixel placement at the content edges, then snap outward to
				// whole pixels so the present-clip never bisects an antialiased boundary pixel (which would
				// leave a faint stale seam where the clipped repaint blends against the retained surface).
				root.Inflate(2, 2);
				root = new SKRect(
					(float)Math.Floor(root.Left),
					(float)Math.Floor(root.Top),
					(float)Math.Ceiling(root.Right),
					(float)Math.Ceiling(root.Bottom));

				if (clipIsRect)
				{
					var clipped = SKRect.Intersect(root, clipRect);
					if (clipped.IsEmpty)
					{
						return false;
					}
					bounds = clipped;
					return true;
				}

				// Non-rectangular clip: intersect the content rect with the clip path so the damage honors the
				// curve instead of over-damaging to the content's bounding box.
				var rectPath = _pathPool.Allocate();
				using var rectPathDisposable = new DisposableStruct<SKPath>(static p => _pathPool.Free(p), rectPath);
				rectPath.Rewind();
				rectPath.AddRect(root);
				clipPath.Op(rectPath, SKPathOp.Intersect, clipPath);

				if (clipPath.IsEmpty)
				{
					return false;
				}
				bounds = clipPath.Bounds;
				regionPath = clipPath;
				keepClipPath = true;
				return true;
			}

			// The content extent can't be bounded tighter than the clip (shadow casters / unknown painters).
			if (clipIsRect)
			{
				bounds = clipRect;
				return true;
			}
			bounds = clipPath.Bounds;
			regionPath = clipPath;
			keepClipPath = true;
			return true;
		}
		finally
		{
			if (!keepClipPath)
			{
				_pathPool.Free(clipPath);
			}
			if (!keepContentPath)
			{
				_pathPool.Free(contentPath);
			}
		}
	}

	// Stroke of width 2*margin: its outline is a band reaching `margin` (2px) on each side of the path edge.
	private static readonly SKPaint _outsetPaint = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 4f, StrokeJoin = SKStrokeJoin.Round, StrokeCap = SKStrokeCap.Round };

	// Outsets a region path by ~2px (in root pixels) to absorb antialiasing bleed at the shape's edge, so the
	// non-antialiased present-clip doesn't bisect the shape's antialiased boundary and leave a faint seam.
	private static void OutsetForAntialiasing(SKPath path)
	{
		var band = _pathPool.Allocate();
		using var bandDisposable = new DisposableStruct<SKPath>(static p => _pathPool.Free(p), band);
		var result = _pathPool.Allocate();
		using var resultDisposable = new DisposableStruct<SKPath>(static p => _pathPool.Free(p), result);

		band.Rewind();
		result.Rewind();
		// (path) ∪ (stroke band around its edge) = the path grown outward by `margin`.
		_outsetPaint.GetFillPath(path, band);
		path.Op(band, SKPathOp.Union, result);
		path.Rewind();
		path.AddPath(result);
	}

	// Bounds, in this visual's local coordinate space, of what it paints *itself* (not its children).
	// Returns false when the paint extent can't be bounded tighter than the clip (the caller then uses the
	// clip as a safe upper bound). An empty rect means the visual paints nothing of its own. Overridden by
	// visual types whose paint isn't bounded by Size (e.g. ShapeVisual paints arbitrary geometry).
	internal virtual bool TryGetLocalContentBounds(out SKRect localBounds)
	{
		localBounds = default;

		if (ShadowState is not null)
		{
			// A drop shadow is cast from this visual's whole silhouette — its own Size rect unioned with every
			// descendant, since a child can overflow the caster and still cast a shadow. If Size can't bound the
			// caster's own content, fall back to the clip.
			if (Size is not { X: > 0, Y: > 0 })
			{
				return false;
			}
			return TryGetShadowSilhouetteBounds(new SKRect(0, 0, Size.X, Size.Y), out localBounds);
		}

		if (!CanPaint())
		{
			// Containers and other non-painting visuals contribute nothing of their own; their children
			// contribute their own damage as they are walked.
			localBounds = SKRect.Empty;
			return true;
		}

		if (PaintsWithinOwnSize)
		{
			// This visual paints strictly within its own size, so Size bounds its content. A degenerate size
			// (zero width or height, e.g. an empty TextBlock) means it paints nothing: return an empty rect so
			// it contributes no damage, rather than falling through to the whole-clip fallback below — which,
			// for an unclipped visual, would be the infinite clip and would dirty the entire frame.
			localBounds = new SKRect(0, 0, Math.Max(0f, Size.X), Math.Max(0f, Size.Y));
			return true;
		}

		return false;
	}

	// Appends the actual painted shape (a path, in this visual's local coordinate space) to <paramref
	// name="dst"/> for visuals whose content isn't rectangular (e.g. a rounded border or an ellipse), so the
	// damage region follows the shape instead of its bounding box. Returns false to fall back to the
	// rectangular <see cref="TryGetLocalContentBounds"/> path. Not used when a shadow or backdrop-sampling
	// margin is involved (those expand a rectangular silhouette).
	internal virtual bool TryGetLocalContentPath(SKPath dst) => false;

	// Expands a shadow caster's OWN painted local bounds to its full drop-shadow silhouette: unions the painted
	// bounds of every descendant (a child can overflow the caster and still cast a shadow), then offsets/blurs
	// for the shadow. The subtree is accumulated in root space (each descendant's content bounds via its
	// TotalMatrix) and mapped back to this caster's local space. Returns false when a descendant's painted
	// extent can't be bounded, so the caller falls back to the clip as a safe upper bound.
	private protected bool TryGetShadowSilhouetteBounds(SKRect ownLocalBounds, out SKRect localBounds)
	{
		localBounds = default;

		var casterMatrix = TotalMatrix.ToSKMatrix();
		var silhouetteInRoot = casterMatrix.MapRect(ownLocalBounds);
		if (!TryAccumulateDescendantContentBoundsInRoot(ref silhouetteInRoot))
		{
			return false;
		}

		var silhouetteLocal = casterMatrix.TryInvert(out var inverse)
			? inverse.MapRect(silhouetteInRoot)
			: ownLocalBounds;
		localBounds = ExpandForShadow(silhouetteLocal);
		return true;
	}

	// Unions, into <paramref name="acc"/> (root space), the painted content bounds of every descendant of this
	// visual (via each descendant's own TryGetLocalContentBounds, so a ShapeVisual's arbitrary geometry is
	// covered, not just its Size). Returns false if a descendant's painted extent can't be bounded. A descendant
	// that is itself a shadow caster already folds its own subtree (and shadow) into its content bounds, so its
	// children aren't walked again.
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

	// Expands a local content rect to also cover the drop shadow this visual casts (the silhouette offset by
	// (Dx,Dy) and blurred by ~3*sigma), so the shadow is included in the damage region. No-op without a shadow.
	private protected SKRect ExpandForShadow(SKRect content)
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
