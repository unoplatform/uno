#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;
using Uno.Extensions;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

public partial class Visual
{
	private Rect _lastRenderBounds;
	private Matrix4x4 _lastRenderMatrix;
	private bool _hasLastRenderBounds;

	// Set during the render walk when this visual's subtree changed this frame; used only to re-damage a
	// drop-shadow's silhouette (which depends on descendants) when a descendant moved/changed.
	internal bool _subtreeChangedThisFrame;

	// Bounds of the clip this visual's content was last painted under (see ContributeDamageOnPaint).
	private Rect _lastClipBounds;

	internal virtual float DamageRegionSamplingMargin => 0;

	/// <summary>
	/// A visual removed from the tree won't be visited next frame, so nothing would damage the area it occupied.
	/// Damage this visual's (and every descendant's) last-rendered bounds into <paramref name="target"/> so the
	/// partial-repaint path clears their old pixels.
	/// </summary>
	internal void ContributeRemovalDamage(Uno.UI.Composition.ICompositionTarget target)
	{
		if (_hasLastRenderBounds)
		{
			target.AddDamage(_lastRenderBounds);
			_hasLastRenderBounds = false;
		}

		foreach (var child in GetChildrenInRenderOrder())
		{
			child.ContributeRemovalDamage(target);
		}
	}

	internal void ContributeDamageOnPaint(bool contentChanged, DamageRegion? damage, bool clipChanged)
	{
		if (damage is null)
		{
			return;
		}

		var matrix = TotalMatrix;
		var moved = !_hasLastRenderBounds || matrix != _lastRenderMatrix;

		var shadowSilhouetteChanged = ShadowState is not null && _subtreeChangedThisFrame;

		// The clip in effect for this visual's own content, in root coordinates. Rect-only: damage only ever
		// consumes clip BOUNDS, and during a scroll every visible visual reaches this point every frame — a
		// geometry-based total-clip walk (allocations + polygon booleans) per moved visual is pure overhead.
		// Non-rect clips contribute their bounds (or nothing when unbounded), which only widens damage — safe.
		var clipRect = GetTotalClipBoundsRect();

		// The accumulated clip can grow or shrink while this visual's own content and transform stay identical
		// (an ancestor re-clipping to a new size), revealing or hiding part of it, and nothing else would report
		// that as damage. Two independent signals, because neither covers the other: clipChanged is raised where a
		// Clip/LayoutClip is mutated, so it catches a shape change inside unchanged bounds, while the bounds
		// fingerprint catches clips this visual never sees mutate, such as the frame's root clip.
		clipChanged |= !RectEquals(clipRect, _lastClipBounds);
		_lastClipBounds = clipRect;

		if (!contentChanged && !moved && !clipChanged && !shadowSilhouetteChanged)
		{
			return;
		}

		if (TryGetPaintDamageRegion(clipRect, out var bounds))
		{
			damage.UnionRect(bounds);

			if (_hasLastRenderBounds && (matrix != _lastRenderMatrix || !RectEquals(bounds, _lastRenderBounds)))
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

	/// <summary>Root-space rect bounds of the clips in effect for this visual's own content: its own and its
	/// ancestors' rect-shaped clips intersected (see <see cref="GetLocalCullClipBounds"/>); non-rect clips
	/// contribute nothing, which only widens the result.</summary>
	private Rect GetTotalClipBoundsRect()
	{
		var rect = InfiniteClipRect;
		for (var visual = this; visual is not null; visual = visual.Parent as Visual)
		{
			if (visual.GetLocalCullClipBounds() is { } localClip)
			{
				rect = Intersect(rect, localClip.Transform(visual.TotalMatrix.ToMatrix3x2()));
				if (IsRectEmpty(rect))
				{
					return default;
				}
			}
		}

		return rect;
	}

	private bool TryGetPaintDamageRegion(Rect clipRect, out Rect bounds)
	{
		bounds = default;

		if (IsRectEmpty(clipRect))
		{
			return false;
		}

		// Rect-only, deliberately: DamageRegion accumulates rects (a geometry contribution is reduced to its
		// bounds anyway), so a geometry-shaped region here would pay polygon booleans — pathological on the
		// managed geometry engine when every visual moves (scrolling) — for no tighter final damage.

		// The clip is outset too: intersecting against a tight clip would otherwise claw the outset back off
		// whichever edge the content reaches, and the unbounded fallback below returns this rect as-is.
		clipRect = OutsetForAntialiasing(clipRect);

		if (TryGetLocalContentBounds(out var local))
		{
			if (IsRectEmpty(local))
			{
				return false;
			}

			var samplingMargin = DamageRegionSamplingMargin;
			if (samplingMargin > 0)
			{
				local = Inflate(local, samplingMargin, samplingMargin);
			}

			var clipped = Intersect(OutsetForAntialiasing(local.Transform(TotalMatrix.ToMatrix3x2())), clipRect);
			if (IsRectEmpty(clipped))
			{
				return false;
			}

			bounds = clipped;
			return true;
		}

		bounds = clipRect;
		return true;
	}

	/// <summary>
	/// Widens a damage contribution to cover the antialiased fringe. Rendering writes device pixels whose
	/// centers lie outside the geometry, while the non-AA damage clip only replays pixels whose centers lie
	/// inside; without the slack that fringe is written once and never replayed, and accumulates as a stale
	/// edge. Two root pixels plus an outward snap is at least the one device pixel the fringe can reach at any
	/// rasterization scale a display reports, so the present path can clip to the region as it stands.
	/// </summary>
	private static Rect OutsetForAntialiasing(Rect rect)
	{
		rect = Inflate(rect, 2, 2);
		return new Rect(
			Math.Floor(rect.X),
			Math.Floor(rect.Y),
			Math.Ceiling(rect.Right) - Math.Floor(rect.X),
			Math.Ceiling(rect.Bottom) - Math.Floor(rect.Y));
	}

	internal virtual bool TryGetLocalContentBounds(out Rect localBounds)
	{
		localBounds = default;

		// What this visual paints itself, in local coordinates: nothing for non-painting visuals (containers),
		// its Size when it paints within Size, otherwise it can't be bounded here and we fall back to the clip.
		Rect ownContent;
		if (!CanPaint())
		{
			ownContent = default;
		}
		else if (PaintsWithinOwnSize)
		{
			ownContent = new Rect(0, 0, Math.Max(0f, Size.X), Math.Max(0f, Size.Y));
		}
		else
		{
			return false;
		}

		if (ShadowState is not null)
		{
			return TryGetShadowSilhouetteBounds(ownContent, out localBounds);
		}

		localBounds = ownContent;
		return true;
	}

	private protected bool TryGetShadowSilhouetteBounds(Rect ownLocalBounds, out Rect localBounds)
	{
		localBounds = default;

		var casterMatrix = TotalMatrix.ToMatrix3x2();
		var silhouetteInRoot = ownLocalBounds.Transform(casterMatrix);
		if (!TryAccumulateDescendantContentBoundsInRoot(ref silhouetteInRoot))
		{
			return false;
		}

		if (IsRectEmpty(silhouetteInRoot))
		{
			localBounds = default;
			return true;
		}

		Rect silhouetteLocal;
		if (Matrix3x2.Invert(casterMatrix, out var inverse))
		{
			silhouetteLocal = silhouetteInRoot.Transform(inverse);
		}
		else
		{
			silhouetteLocal = ownLocalBounds;
		}

		localBounds = ExpandForShadow(silhouetteLocal);
		return true;
	}

	/// <summary>
	/// The bounds of everything this visual and its subtree paint, in root coordinates. False when some
	/// part of it can't be bounded analytically, in which case the caller has to fall back to its clip.
	/// </summary>
	internal bool TryGetSubtreeContentBoundsInRoot(out Rect boundsInRoot)
	{
		boundsInRoot = default;

		if (!TryGetLocalContentBounds(out var own))
		{
			return false;
		}

		if (!IsRectEmpty(own))
		{
			boundsInRoot = own.Transform(TotalMatrix.ToMatrix3x2());
		}

		// A shadow's silhouette already covers the descendants, see TryGetShadowSilhouetteBounds.
		return ShadowState is not null || TryAccumulateDescendantContentBoundsInRoot(ref boundsInRoot);
	}

	private bool TryAccumulateDescendantContentBoundsInRoot(ref Rect acc)
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

			if (!IsRectEmpty(childLocal))
			{
				var rect = childLocal.Transform(child.TotalMatrix.ToMatrix3x2());
				acc = IsRectEmpty(acc) ? rect : Union(acc, rect);
			}

			if (child.ShadowState is null && !child.TryAccumulateDescendantContentBoundsInRoot(ref acc))
			{
				return false;
			}
		}

		return true;
	}

	private Rect ExpandForShadow(Rect content)
	{
		if (ShadowState is not { } shadow)
		{
			return content;
		}

		var shadowRect = OffsetRect(content, shadow.Dx, shadow.Dy);
		shadowRect = Inflate(shadowRect, shadow.SigmaX * 3, shadow.SigmaY * 3);
		return Union(content, shadowRect);
	}

	// --- Rect helpers (Windows.Foundation.Rect, kept in LTRB-safe form) ---

	private protected static bool IsRectEmpty(Rect r) => r.Width <= 0 || r.Height <= 0;

	private static bool RectEquals(Rect a, Rect b)
		=> a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;

	private static Rect Inflate(Rect r, double dx, double dy)
		=> new(r.X - dx, r.Y - dy, Math.Max(0, r.Width + 2 * dx), Math.Max(0, r.Height + 2 * dy));

	private static Rect OffsetRect(Rect r, double dx, double dy)
		=> new(r.X + dx, r.Y + dy, r.Width, r.Height);

	private static Rect Union(Rect a, Rect b)
	{
		if (IsRectEmpty(a))
		{
			return b;
		}

		if (IsRectEmpty(b))
		{
			return a;
		}

		var left = Math.Min(a.Left, b.Left);
		var top = Math.Min(a.Top, b.Top);
		var right = Math.Max(a.Right, b.Right);
		var bottom = Math.Max(a.Bottom, b.Bottom);
		return new Rect(left, top, right - left, bottom - top);
	}

	private protected static Rect Intersect(Rect a, Rect b)
	{
		var left = Math.Max(a.Left, b.Left);
		var top = Math.Max(a.Top, b.Top);
		var right = Math.Min(a.Right, b.Right);
		var bottom = Math.Min(a.Bottom, b.Bottom);
		return right <= left || bottom <= top ? default : new Rect(left, top, right - left, bottom - top);
	}
}
