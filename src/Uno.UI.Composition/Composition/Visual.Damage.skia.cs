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

	// The local-space geometry this visual paints, captured by Paint when the picture is (re)recorded and
	// reused for the per-frame damage region instead of being rebuilt every frame. A moved-but-unchanged
	// visual keeps the cached path (its picture isn't re-recorded, so neither is this). Empty/false means the
	// visual paints nothing analytically describable, and damage falls back to its bounds.
	private SKPath? _ownContentPath;
	private bool _hasOwnContentPath;

	internal virtual float DamageRegionSamplingMargin => 0;

	// Caches the geometry a visual's Paint returned (local coordinates), so the per-frame damage region reuses
	// it instead of rebuilding it. Null means the visual paints nothing analytically describable.
	private void CacheOwnContentPath(SKPath? localContentPath)
	{
		if (localContentPath is null || localContentPath.IsEmpty)
		{
			_hasOwnContentPath = false;
			return;
		}

		_ownContentPath ??= new SKPath();
		_ownContentPath.Rewind();
		_ownContentPath.AddPath(localContentPath);
		_hasOwnContentPath = true;
	}

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
			clipPath.Rewind();
			clipPath.AddPath(clip);
			if (clipPath.IsEmpty)
			{
				return false;
			}

			var clipIsRect = clipPath.IsRect;
			var clipRect = clipPath.Bounds;

			if (ShadowState is null && DamageRegionSamplingMargin == 0 && _hasOwnContentPath)
			{
				contentPath.Rewind();
				contentPath.AddPath(_ownContentPath!);
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

	private static void OutsetForAntialiasing(SKPath path)
	{
		var band = _pathPool.Allocate();
		using var bandDisposable = new DisposableStruct<SKPath>(static p => _pathPool.Free(p), band);
		var result = _pathPool.Allocate();
		using var resultDisposable = new DisposableStruct<SKPath>(static p => _pathPool.Free(p), result);

		band.Rewind();
		result.Rewind();
		_outsetPaint.GetFillPath(path, band);
		path.Op(band, SKPathOp.Union, result);
		path.Rewind();
		path.AddPath(result);
	}

	internal virtual bool TryGetLocalContentBounds(out SKRect localBounds)
	{
		localBounds = default;

		if (ShadowState is not null)
		{
			// Seed the silhouette with the caster's OWN painted bounds, derived the same way as the
			// non-shadow case below (and as WalkShadowSilhouette bounds a visual's own contribution): empty
			// when it paints nothing, its Size when it paints within Size, otherwise we can't bound it and
			// fall back to the clip. TryGetShadowSilhouetteBounds then unions descendants and the shadow.
			SKRect ownContent;
			if (!CanPaint())
			{
				ownContent = SKRect.Empty;
			}
			else if (PaintsWithinOwnSize && Size is { X: > 0, Y: > 0 })
			{
				ownContent = new SKRect(0, 0, Size.X, Size.Y);
			}
			else
			{
				return false;
			}
			return TryGetShadowSilhouetteBounds(ownContent, out localBounds);
		}

		if (!CanPaint())
		{
			localBounds = SKRect.Empty;
			return true;
		}

		if (PaintsWithinOwnSize)
		{
			localBounds = new SKRect(0, 0, Math.Max(0f, Size.X), Math.Max(0f, Size.Y));
			return true;
		}

		return false;
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
