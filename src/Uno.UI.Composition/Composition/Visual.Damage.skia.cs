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

	// The local-space geometry this visual paints, returned by Paint when the picture is (re)recorded and
	// reused for the per-frame damage region instead of being rebuilt every frame. A moved-but-unchanged
	// visual keeps it (its picture isn't re-recorded, so neither is this). Null means the visual paints
	// nothing analytically describable, and damage falls back to its bounds.
	private protected IGeometry? _ownContentPath;

	internal virtual float DamageRegionSamplingMargin => 0;

	// Above this many path segments, skip the geometry-shaped damage region and use the bounding box instead:
	// the boolean Combine to build/clip the tight region costs more than the extra pixels it would save.
	private const int DamageTightPathMaxSegments = 64;

	// Neutral analog of bc's stroke-4px-round outset used to grow a content path to cover antialiasing bleed.
	private static readonly StrokeStyle _outsetStroke = new()
	{
		Thickness = 4f,
		StartCap = StrokeCap.Round,
		EndCap = StrokeCap.Round,
		LineJoin = StrokeJoin.Round,
		MiterLimit = 4f,
	};

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

	internal void ContributeDamageOnPaint(bool contentChanged, DamageRegion? damage)
	{
		if (damage is null)
		{
			return;
		}

		var matrix = TotalMatrix;
		var moved = !_hasLastRenderBounds || matrix != _lastRenderMatrix;

		var shadowSilhouetteChanged = ShadowState is not null && _subtreeChangedThisFrame;

		if (!contentChanged && !moved && !shadowSilhouetteChanged)
		{
			return;
		}

		// The clip in effect for this visual's own content, in root coordinates (its ancestors' clips intersected
		// with its own pre-painting clip). Computed here per damaged visual rather than threaded through the whole
		// render walk; only changed/moved visuals reach this point, so the re-walk stays cheap in practice.
		using var clip = GetTotalClipPath(skipPostPaintingClipping: true);

		if (TryGetPaintDamageRegion(clip, out var bounds, out var regionPath))
		{
			if (regionPath is not null)
			{
				damage.Union(regionPath);
				regionPath.Dispose();
			}
			else
			{
				damage.UnionRect(bounds);
			}

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

	private bool TryGetPaintDamageRegion(IGeometry clip, out Rect bounds, out IGeometry? regionPath)
	{
		bounds = default;
		regionPath = null;

		if (clip.IsEmpty)
		{
			return false;
		}

		var clipRect = clip.Bounds;

		// The tight (geometry-shaped) damage region is only worth its cost for simple shapes. For a complex path
		// its boolean Combine (outset + clip) is expensive — pathologically so on a managed geometry engine, where
		// it flattens curves and runs an O(n^2) boolean — while adding little over the bounding-box fallback below.
		if (ShadowState is null && DamageRegionSamplingMargin == 0
			&& _ownContentPath is { IsEmpty: false } ownContent
			&& ownContent.SegmentCount <= DamageTightPathMaxSegments)
		{
			using var inRoot = ownContent.Transform(TotalMatrix.ToMatrix3x2());
			using var outset = OutsetForAntialiasing(inRoot);
			var clipped = outset.Combine(clip, GeometryCombineMode.Intersect);
			if (clipped.IsEmpty)
			{
				clipped.Dispose();
				return false;
			}

			bounds = clipped.Bounds;
			regionPath = clipped;
			return true;
		}

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

			var root = local.Transform(TotalMatrix.ToMatrix3x2());
			root = Inflate(root, 2, 2);
			root = new Rect(
				Math.Floor(root.X),
				Math.Floor(root.Y),
				Math.Ceiling(root.Right) - Math.Floor(root.X),
				Math.Ceiling(root.Bottom) - Math.Floor(root.Y));

			var clipped = Intersect(root, clipRect);
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

	private static IGeometry OutsetForAntialiasing(IGeometry path)
	{
		using var band = path.GetStrokeFillGeometry(_outsetStroke);
		return path.Combine(band, GeometryCombineMode.Union);
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

	private static bool IsRectEmpty(Rect r) => r.Width <= 0 || r.Height <= 0;

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

	private static Rect Intersect(Rect a, Rect b)
	{
		var left = Math.Max(a.Left, b.Left);
		var top = Math.Max(a.Top, b.Top);
		var right = Math.Min(a.Right, b.Right);
		var bottom = Math.Min(a.Bottom, b.Bottom);
		return right <= left || bottom <= top ? default : new Rect(left, top, right - left, bottom - top);
	}
}
