#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.Disposables;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class Visual
{
	private SKRect _lastRenderBounds;
	private Matrix4x4 _lastRenderMatrix;
	private bool _hasLastRenderBounds;

	private bool _subtreeChangedThisFrame;

	// The local-space geometry this visual paints, returned by Paint when the picture is (re)recorded and
	// reused for the per-frame damage region instead of being rebuilt every frame. A moved-but-unchanged
	// visual keeps it (its picture isn't re-recorded, so neither is this). Null means the visual paints
	// nothing analytically describable, and damage falls back to its bounds.
	private SKPath? _ownContentPath;

	internal virtual float DamageRegionSamplingMargin => 0;

	private void ContributeDamageOnPaint(bool contentChanged, SKPath? damage, SKPath clip)
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

		if (TryGetPaintDamageRegion(clip, out var bounds, out var regionPath))
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

	private bool TryGetPaintDamageRegion(SKPath clip, out SKRect bounds, out SKPath? regionPath)
	{
		bounds = default;
		regionPath = null;

		var clipPath = _pathPool.Allocate();
		var contentPath = _pathPool.Allocate();
		var keepClipPath = false;
		var keepContentPath = false;
		try
		{
			clip.Transform(SKMatrix.Identity, clipPath);
			if (clipPath.IsEmpty)
			{
				return false;
			}

			var clipIsRect = clipPath.IsRect;
			var clipRect = clipPath.Bounds;

			if (ShadowState is null && DamageRegionSamplingMargin == 0 && _ownContentPath is { IsEmpty: false } ownContent)
			{
				ownContent.Transform(SKMatrix.Identity, contentPath);
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

			if (TryGetLocalContentBounds(out var local))
			{
				if (local.IsEmpty)
				{
					return false;
				}

				var samplingMargin = DamageRegionSamplingMargin;
				if (samplingMargin > 0)
				{
					local.Inflate(samplingMargin, samplingMargin);
				}

				var root = TotalMatrix.ToSKMatrix().MapRect(local);
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

				using var rectPath = SkiaExtensions.CreateRectPath(root);
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

	private static readonly SKPaint _outsetPaint = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 4f, StrokeJoin = SKStrokeJoin.Round, StrokeCap = SKStrokeCap.Round };
	private static readonly SKPathBuilder _outsetBuilder = new();

	private static void OutsetForAntialiasing(SKPath path)
	{
		var result = _pathPool.Allocate();
		using var resultDisposable = new DisposableStruct<SKPath>(static p => _pathPool.Free(p), result);

		_outsetBuilder.Reset();
		_outsetPaint.GetFillPath(path, _outsetBuilder);
		using var band = _outsetBuilder.Detach();
		result.Reset();
		path.Op(band, SKPathOp.Union, result);
		result.Transform(SKMatrix.Identity, path);
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
