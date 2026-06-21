#nullable enable
//#define TRACE_COMPOSITION

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using Microsoft.CodeAnalysis.PooledObjects;
using SkiaSharp;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Helpers;
using Uno.UI.Composition;
using Uno.UI.Composition.Composition;

namespace Microsoft.UI.Composition;

public partial class Visual : global::Microsoft.UI.Composition.CompositionObject
{
	private static readonly ObjectPool<SKPath> _pathPool = new(() => new SKPath());
	private static readonly SKPath _spareRenderPath = new SKPath();

	private static readonly SKPath _spareShadowPath = new SKPath();

	// Scratch list used only inside the analytic-shadow silhouette walker. Each visit calls
	// TryAddShadowPaths into this list, drains it, and Clear()s before recursing into children, so a
	// single static instance is safe.
	private static readonly List<(SKPath path, float alpha)> _spareShadowContributions = new();

	private static readonly IPrivateSessionFactory _factory = new PaintingSession.SessionFactory();
	private static readonly List<Visual> s_emptyList = new List<Visual>();

	// Picture-collapsing folds a stable subtree into a cached picture that is no longer walked. It is
	// compatible with damage-region rendering: a subtree only collapses after it has been fully static for
	// several frames — any transform/content/structural change invalidates the cached picture (via
	// InvalidateParentChildrenPicture, which SetMatrixDirty/InvalidatePaint/child mutations all call), so a
	// collapsed subtree has no per-frame change and correctly contributes no damage. The one kind of content
	// that changes without those flags is a visual that repaints every frame (a backdrop/acrylic brush); it
	// invalidates its own ancestor chain each frame it paints (see PaintStep), so no ancestor collapses and it
	// keeps being walked — painted and damaged — every frame.
	internal static bool EnablePictureCollapsingOptimization { get; set; } = true;
	internal static int PictureCollapsingOptimizationFrameThreshold { get; set; } = 50;
	internal static int PictureCollapsingOptimizationVisualCountThreshold { get; set; } = 100;

	private bool _enablePictureCollapsingOptimization;
	private int _pictureCollapsingOptimizationFrameThreshold;
	private int _pictureCollapsingOptimizationVisualCountThreshold;

	private static SKPictureRecorder _recorder = new();

	private CompositionClip? _clip;
	private Vector2 _anchorPoint = Vector2.Zero; // Backing for scroll offsets
	private int _zIndex;
	private (Matrix4x4 matrix, bool isLocalMatrixIdentity) _totalMatrix = (Matrix4x4.Identity, true);
	private IntPtr _picture;
	private IntPtr _childrenPicture;
	private int _framesSinceSubtreeNotChanged;

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

	private VisualFlags _flags = VisualFlags.MatrixDirty | VisualFlags.PaintDirty | VisualFlags.ChildrenSKPictureInvalid;

	private const int SK_MaxS32FitsInFloat = 2147483520;
	// Skia uses SafeEdge = SK_MaxS32FitsInFloat / 2 - 1, but that causes clipping bounds issues in SKCanvasElement when used with LottieVisualSourceBase
	private const int SafeEdge = SK_MaxS32FitsInFloat / 4 - 1;
	// if we use float.Min/MaxValue, weird overflows happen and clipping breaks badly.
	// https://github.com/mono/skia/blob/927041a58f130e0dd0562ba86cb4170989ad39e9/src/core/SkRecorder.cpp#L79
	// https://github.com/mono/skia/blob/927041a58f130e0dd0562ba86cb4170989ad39e9/src/core/SkRectPriv.h#L38
	internal static SKRect InfiniteClipRect { get; } = new(-SafeEdge, -SafeEdge, SafeEdge, SafeEdge);

	internal bool IsNativeHostVisual => (_flags & VisualFlags.IsNativeHostVisualSet) != 0 ? (_flags & VisualFlags.IsNativeHostVisual) != 0 : (_flags & VisualFlags.IsNativeHostVisualInherited) != 0;

	/// <summary>A visual is a NativeHost visual if it's directly set by SetAsNativeHostVisual or is a child of a NativeHost visual</summary>
	/// <remarks>call with a null <paramref name="isNativeHostVisual"/> to unset.</remarks>
	internal void SetAsNativeHostVisual(bool? isNativeHostVisual) => SetAsNativeHostVisual(isNativeHostVisual, false);
	private void SetAsNativeHostVisual(bool? isNativeHostVisual, bool inherited)
	{
		Debug.Assert(!inherited || isNativeHostVisual is { }, "Only non-null values should be inherited.");
		var oldValue = IsNativeHostVisual;

		if (inherited)
		{
			_flags |= (isNativeHostVisual!.Value ? VisualFlags.IsNativeHostVisualInherited : 0);
		}
		else if (isNativeHostVisual is { })
		{
			_flags |= VisualFlags.IsNativeHostVisualSet;
			if (isNativeHostVisual.Value)
			{
				_flags |= VisualFlags.IsNativeHostVisual;
			}
			else
			{
				_flags &= ~VisualFlags.IsNativeHostVisual;
			}
		}
		else
		{
			_flags &= ~VisualFlags.IsNativeHostVisualSet;
		}

		var newValue = IsNativeHostVisual;
		if (oldValue != newValue)
		{
			foreach (var child in GetChildrenInRenderOrder())
			{
				child.SetAsNativeHostVisual(newValue, true);
			}
		}
	}

	partial void InitializePartial()
	{
		_enablePictureCollapsingOptimization = EnablePictureCollapsingOptimization;
		_pictureCollapsingOptimizationFrameThreshold = PictureCollapsingOptimizationFrameThreshold;
		_pictureCollapsingOptimizationVisualCountThreshold = PictureCollapsingOptimizationVisualCountThreshold;
	}

	/// <summary>
	/// Identifies whether a Visual can paint things. For example, ContainerVisuals don't
	/// paint on their own (even though they might contain other Visuals that do).
	/// This is a temporary optimization to reduce unnecessary SkPicture allocations.
	/// In the future, we should accurately set <see cref="_requiresRepaint"/> to
	/// only be true when we really have something to paint (and that painting needs to be updated).
	/// </summary>
	internal virtual bool CanPaint() => false;

	/// <summary>
	/// When true, this visual guarantees that everything <em>it itself</em> paints stays inside
	/// <c>(0, 0, Size.X, Size.Y)</c> in its local coordinates. This is a per-visual guarantee, not a
	/// statement about the subtree — descendants may still paint anywhere. The analytic drop-shadow
	/// walker uses it solely to decide whether <em>this visual's own</em> <c>TryAddShadowPaths</c> call
	/// can be skipped (when Size is inside the opaque silhouette), and does not propagate Size as a clip
	/// to children. Default <c>false</c>: a <see cref="Visual"/> is allowed to paint anywhere in WinUI
	/// semantics, so we don't assume the bounds constrain it. Subclasses that genuinely respect their
	/// Size opt in.
	/// </summary>
	internal virtual bool PaintsWithinOwnSize => false;

	// this is for effect brushes that apply an effect on an already-drawn area, so these need to be painted every frame.
	internal virtual bool RequiresRepaintOnEveryFrame => false;

	// How far (in local pixels) beyond its own bounds this visual's paint samples the surface (e.g. a backdrop
	// blur). Damage-region rendering expands the damage region by this margin so the sampled backdrop stays
	// fresh; otherwise the effect reads stale pixels where the surrounding content changed. 0 by default.
	internal virtual float DamageRegionSamplingMargin => 0;

	/// <returns>true if wasn't dirty</returns>
	internal virtual bool SetMatrixDirty()
	{
		var matrixDirty = (_flags & VisualFlags.MatrixDirty) != 0;
		_flags |= VisualFlags.MatrixDirty;
		InvalidateParentChildrenPicture(false);
		return !matrixDirty;
	}

	/// <summary>
	/// This is the final transformation matrix from the origin to this Visual.
	/// </summary>
#if DEBUG
	[DebuggerDisplay("{TotalMatrixString}")]
#endif
	internal Matrix4x4 TotalMatrix
	{
		get
		{
			// Due to the layout of the matrices and how they're multiplied, a scaling transform followed by a
			// translating transform will actually scale the translation. i.e.
			// MatrixThatTranslatesBy50 * MatrixThatScalesBy2 = MatrixThatScalesBy2ThenTranslatesBy100
			// This contradicts the traditional linear algebraic definitions, but works out in practice (e.g.
			// if the canvas is scaled very early, you want all the offsets to scale with it)
			// https://learn.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/transforms/matrix
			if ((_flags & VisualFlags.MatrixDirty) != 0)
			{
				_flags &= ~VisualFlags.MatrixDirty;

				var isLocalMatrixIdentity = true;

				// Start out with the final matrix of the parent
				var matrix = Parent?.TotalMatrix ?? Matrix4x4.Identity;

				// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
				var totalOffset = GetTotalOffset();
				var offsetMatrix = new Matrix4x4(
					1, 0, 0, 0,
					0, 1, 0, 0,
					0, 0, 1, 0,
					totalOffset.X + AnchorPoint.X, totalOffset.Y + AnchorPoint.Y, 0, 1);
				if (!offsetMatrix.IsIdentity)
				{
					isLocalMatrixIdentity = false;
					matrix = offsetMatrix * matrix;
				}

				// Apply the rending transformation matrix (i.e. change coordinates system to the "rendering" one)
				if (GetTransform() is { IsIdentity: false } transform)
				{
					isLocalMatrixIdentity = false;
					matrix = transform * matrix;
				}

				_totalMatrix = (matrix, isLocalMatrixIdentity);
			}

			return _totalMatrix.matrix;

			Matrix4x4 GetTransform()
			{
				var transform = TransformMatrix;

				var scale = Scale;
				if (scale != Vector3.One)
				{
					transform *= Matrix4x4.CreateScale(scale, CenterPoint);
				}

				var orientation = Orientation;
				if (orientation != Quaternion.Identity)
				{
					transform *= Matrix4x4.CreateFromQuaternion(orientation);
				}

				var rotation = RotationAngle;
				if (rotation is not 0)
				{
					transform *= Matrix4x4.CreateTranslation(-CenterPoint);
					transform *= Matrix4x4.CreateFromAxisAngle(RotationAxis, rotation);
					transform *= Matrix4x4.CreateTranslation(CenterPoint);
				}

				return transform;
			}
		}
	}

#if DEBUG
	internal string TotalMatrixString => $"{((_flags & VisualFlags.MatrixDirty) != 0 ? "-dirty-" : "")}{_totalMatrix}";
#endif

	/// <remarks>
	/// This should only be called from <see cref="Compositor.InvalidateRenderPartial"/>
	/// </remarks>
	internal void InvalidatePaint()
	{
		if (_picture != IntPtr.Zero)
		{
			UnoSkiaApi.sk_refcnt_safe_unref(_picture);
			_picture = IntPtr.Zero;
		}
		_flags |= VisualFlags.PaintDirty;
		InvalidateParentChildrenPicture(false);
	}

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

		// Scratch path (from the shared pool) to turn the rect contributions below into paths for unioning.
		var scratch = _pathPool.Allocate();
		using var scratchDisposable = new DisposableStruct<SKPath>(static path => _pathPool.Free(path), scratch);
		if (TryGetPaintDamageRegion(out var bounds, out var regionPath))
		{
			if (regionPath is not null)
			{
				damage.Union(regionPath);
				_pathPool.Free(regionPath);
			}
			else
			{
				damage.UnionRect(scratch, bounds);
			}

			// Erase the region this visual vacated — but only when it actually moved or resized. The bounding
			// rect is a safe (if loose) cover for a curved old region. Adding it on an in-place repaint (same
			// matrix and bounds) would be redundant AND would rectangularize an otherwise-rounded region, so it
			// is skipped: the new region already covers everything the unchanged old one did.
			if (_hasLastRenderBounds && (matrix != _lastRenderMatrix || bounds != _lastRenderBounds))
			{
				damage.UnionRect(scratch, _lastRenderBounds);
			}
			_lastRenderBounds = bounds;
			_lastRenderMatrix = matrix;
			_hasLastRenderBounds = true;
		}
		else if (_hasLastRenderBounds)
		{
			// The visual no longer has paintable bounds (e.g. collapsed to zero size); repaint where it was.
			damage.UnionRect(scratch, _lastRenderBounds);
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
				try
				{
					rectPath.Rewind();
					rectPath.AddRect(root);
					clipPath.Op(rectPath, SKPathOp.Intersect, clipPath);
				}
				finally
				{
					_pathPool.Free(rectPath);
				}

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
		var result = _pathPool.Allocate();
		try
		{
			band.Rewind();
			result.Rewind();
			// (path) ∪ (stroke band around its edge) = the path grown outward by `margin`.
			_outsetPaint.GetFillPath(path, band);
			path.Op(band, SKPathOp.Union, result);
			path.Rewind();
			path.AddPath(result);
		}
		finally
		{
			_pathPool.Free(band);
			_pathPool.Free(result);
		}
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
			// A drop shadow paints this visual's silhouette beyond its own size (offset + blur). Use Size as
			// the silhouette and expand for the shadow. If Size can't bound the silhouette, fall back to clip.
			if (Size is not { X: > 0, Y: > 0 })
			{
				return false;
			}
			localBounds = ExpandForShadow(new SKRect(0, 0, Size.X, Size.Y));
			return true;
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

	internal void InvalidateParentChildrenPicture(bool includeSelf)
	{
		var parent = includeSelf ? this : Parent;
		while (parent is not null && (parent._flags & VisualFlags.ChildrenSKPictureInvalid) == 0)
		{
			if (parent._childrenPicture != IntPtr.Zero)
			{
				UnoSkiaApi.sk_refcnt_safe_unref(parent._childrenPicture);
				parent._childrenPicture = IntPtr.Zero;
			}
			parent._flags |= VisualFlags.ChildrenSKPictureInvalid;
			parent = parent.Parent;
		}
	}

	public CompositionClip? Clip
	{
		get => _clip;
		set => SetProperty(ref _clip, value);
	}

	public Vector2 AnchorPoint
	{
		get => _anchorPoint;
		set
		{
			SetProperty(ref _anchorPoint, value);
		}
	}

	internal int ZIndex
	{
		get => _zIndex;
		set
		{
			if (_zIndex != value)
			{
				SetProperty(ref _zIndex, value);
				if (Parent is ContainerVisual containerVisual)
				{
					containerVisual.IsChildrenRenderOrderDirty = true;
				}
			}
		}
	}

	internal ShadowState? ShadowState { get; set; }


	partial void OnOffsetChanged(Vector3 value)
		=> VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged?.Invoke(this);

	partial void OnArrangeOffsetChanged(Vector3 value)
		=> VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged?.Invoke(this);

	partial void OnSizeChanged(Vector2 value)
		=> VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged?.Invoke(this);

	partial void OnIsVisibleChanged(bool value)
	{
		VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged?.Invoke(this);

		// When hidden, this visual (and its subtree) is no longer walked, so it can't contribute its
		// vacated region to the dirty area; damage what it last painted so the content underneath repaints.
		if (!value && CompositionTarget is { } target)
		{
			DamageLastRenderedRegion(target);
		}
	}

	// Damages the region this visual last painted (for containers, recursively for the subtree), so that
	// when it's removed from the tree or hidden — and therefore no longer walked — the content that was
	// underneath it is repainted instead of leaving stale pixels. Bounds are already in root coordinates.
	internal virtual void DamageLastRenderedRegion(ICompositionTarget target)
	{
		if (_hasLastRenderBounds)
		{
			target.AddDamage(_lastRenderBounds);
			_hasLastRenderBounds = false;
		}
	}

	/// <summary>
	/// Render a visual as if it's the root visual.
	/// </summary>
	/// <param name="canvas">The canvas on which this visual should be rendered.</param>
	/// <param name="offsetOverride">The offset (from the origin) to render the Visual at. If null, the offset properties on the Visual like <see cref="Offset"/> and <see cref="AnchorPoint"/> are used.</param>
	internal void RenderRootVisual(SKCanvas canvas, Vector2? offsetOverride, SKPath? damage = null)
	{
		if (this is { Opacity: 0 } or { IsVisible: false })
		{
			return;
		}

		// Since we're acting as if this visual is a root visual, we undo the parent's TotalMatrix
		// so that when concatenated with this visual's TotalMatrix, the result is only the transforms
		// from this visual.
		// It's important to set the default to canvas.TotalMatrix not SKMatrix.Identity in case there's
		// an initial global transformation set (e.g. if the renderer sets scaling for dpi or we're rendering from a VisualSurface)
		var initialTransform = canvas.TotalMatrix.ToMatrix4x4();
		if (Parent?.TotalMatrix is { } parentTotalMatrix)
		{
			Matrix4x4.Invert(parentTotalMatrix, out var invertedParentTotalMatrix);
			initialTransform = invertedParentTotalMatrix * initialTransform;
		}

		if (offsetOverride is { } offset)
		{
			var totalOffset = GetTotalOffset();
			var translation = Matrix4x4.Identity with { M41 = -(offset.X + totalOffset.X + AnchorPoint.X), M42 = -(offset.Y + totalOffset.Y + AnchorPoint.Y) };
			initialTransform = translation * initialTransform;
		}

		_factory.CreateInstance(this,
						  canvas,
						  ref initialTransform.IsIdentity ? ref Unsafe.NullRef<Matrix4x4>() : ref initialTransform,
						  opacity: 1.0f,
						  damage,
						  out var session);

		using (session)
		{
			// we set the matrix here similarly to CreateLocalMatrix in case the SetMatrix call there is
			// omitted.
			canvas.SetMatrix(initialTransform.IsIdentity ? TotalMatrix : TotalMatrix * initialTransform);
			Render(session);
		}
	}

	/// <summary>
	/// Position a sub visual on the canvas and draw its content.
	/// </summary>
	/// <param name="parentSession">The drawing session of the <see cref="Parent"/> visual.</param>
	private void Render(in PaintingSession parentSession, bool applyChildOptimization = true)
	{
#if TRACE_COMPOSITION
		var indent = int.TryParse(Comment?.Split(new char[] { '-' }, 2, StringSplitOptions.TrimEntries).FirstOrDefault(), out var depth)
			? new string(' ', depth * 2)
			: string.Empty;
		global::System.Diagnostics.Debug.WriteLine($"{indent}{Comment} (Opacity:{parentSession.Opacity:F2}x{Opacity:F2} | IsVisible:{IsVisible})");
#endif

		if (this is { Opacity: 0 } or { IsVisible: false })
		{
			return;
		}

		if ((_flags & VisualFlags.ChildrenSKPictureInvalid) == 0)
		{
			_framesSinceSubtreeNotChanged++;
			_subtreeChangedThisFrame = false;
		}
		else
		{
			_framesSinceSubtreeNotChanged = 0;
			_flags &= ~VisualFlags.ChildrenSKPictureInvalid;
			_subtreeChangedThisFrame = true;
		}

		CreateLocalSession(in parentSession, out var session);

		using (session)
		{
			var canvas = session.Canvas;

			var preClip = _spareRenderPath;

			preClip.Rewind();

			if (GetPrePaintingClipping(preClip))
			{
				canvas.ClipPath(preClip, antialias: true);
			}

			if (ShadowState is null || TryRenderAnalyticShadow(canvas, ShadowState))
			{
				PaintStep(this, session);
				PostPaintingClipStep(this, canvas);
				RenderChildrenStep(this, session, applyChildOptimization);
			}
			else
			{
				var recorder = new SKPictureRecorder();
				var recordingCanvas = recorder.BeginRecording(InfiniteClipRect);
				// child.Render will reapply the total transform matrix, so we need to invert ours.
				Matrix4x4.Invert(TotalMatrix, out var rootTransform);
				_factory.CreateInstance(this, recordingCanvas, ref rootTransform, session.Opacity, session.Damage, out var childSession);
				using (childSession)
				{
					PaintStep(this, childSession);
					PostPaintingClipStep(this, recordingCanvas);
					RenderChildrenStep(this, childSession, applyChildOptimization);
				}

				unsafe
				{
					var childrenPicture = UnoSkiaApi.sk_picture_recorder_end_recording(recorder.Handle);

					UnoSkiaApi.sk_canvas_draw_picture(canvas.Handle, childrenPicture, null, ShadowState.ShadowOnlyPaint.Handle);
					UnoSkiaApi.sk_canvas_draw_picture(canvas.Handle, childrenPicture, null, IntPtr.Zero);

					UnoSkiaApi.sk_refcnt_safe_unref(childrenPicture);
				}
			}
		}

		static void PaintStep(Visual visual, in PaintingSession session)
		{
			// Rendering shouldn't depend on matrix or clip adjustments happening in a visual's Paint. That should
			// be specific to that visual and should not affect the rendering of any other visual.
#if DEBUG
			var saveCount = session.Canvas.SaveCount;
#endif
			if (visual.RequiresRepaintOnEveryFrame)
			{
				// This visual repaints every frame without going through the dirty flags, so it would freeze
				// if an ancestor folded it into a collapsed picture (and its per-frame change wouldn't be
				// damaged). Invalidate the ancestor chain so none of them collapse — which keeps this visual
				// walked, painted and damaged every frame, and is what makes picture-collapsing compatible with
				// both damage-region rendering and live backdrop/acrylic content.
				visual.InvalidateParentChildrenPicture(includeSelf: false);
				// why bother with a recorder when it's going to get repainted next frame? just paint directly
				visual.ContributeDamageOnPaint(contentChanged: true, session.Damage);
				visual.Paint(session);
			}
			else
			{
				var contentChanged = (visual._flags & VisualFlags.PaintDirty) != 0;
				if (contentChanged)
				{
					visual._flags &= ~VisualFlags.PaintDirty;

					var recordingCanvas = _recorder.BeginRecording(InfiniteClipRect);
					_factory.CreateInstance(visual, recordingCanvas, ref session.RootTransform, session.Opacity, session.Damage, out var recorderSession);
					// To debug what exactly gets repainted, replace the following line with `Paint(in session);`
					visual.Paint(in recorderSession);

					var picture = UnoSkiaApi.sk_picture_recorder_end_recording(_recorder.Handle);

					if (visual._picture != IntPtr.Zero)
					{
						UnoSkiaApi.sk_refcnt_safe_unref(visual._picture);
					}

					visual._picture = picture;
				}

				if (visual._picture != IntPtr.Zero)
				{
					// Contribute damage on every draw (not only when the picture was re-recorded) so a visual
					// that merely moved — e.g. content scrolling under a clip — damages both the region it now
					// occupies and the one it vacated, even though its cached picture is reused.
					visual.ContributeDamageOnPaint(contentChanged, session.Damage);
					unsafe
					{
						UnoSkiaApi.sk_canvas_draw_picture(session.Canvas.Handle, visual._picture, null, IntPtr.Zero);
					}
				}
			}
#if DEBUG
			Debug.Assert(saveCount == session.Canvas.SaveCount);
#endif
		}

		static void PostPaintingClipStep(Visual visual, SKCanvas canvas)
		{
#if DEBUG
			canvas.Save();
			if (visual.GetPostPaintingClipping() is { } postClip)
			{
				canvas.ClipPath(postClip, antialias: true);
			}

			var nonOptimizedClip = (canvas.DeviceClipBounds, canvas.IsClipRect);
			canvas.Restore();
#endif
			visual.ApplyPostPaintingClipping(canvas);
#if DEBUG
			Debug.Assert(nonOptimizedClip.IsClipRect == canvas.IsClipRect && nonOptimizedClip.DeviceClipBounds == canvas.DeviceClipBounds);
#endif
		}

		static void RenderChildrenStep(Visual visual, PaintingSession session, bool applyChildOptimization)
		{
			if (visual._childrenPicture != IntPtr.Zero)
			{
				unsafe
				{
					UnoSkiaApi.sk_canvas_draw_picture(session.Canvas.Handle, visual._childrenPicture, null, IntPtr.Zero);
				}
			}
			else if (!visual._enablePictureCollapsingOptimization
					 || visual._framesSinceSubtreeNotChanged < visual._pictureCollapsingOptimizationFrameThreshold
					 || !applyChildOptimization
					 || visual.GetSubTreeVisualCount() < visual._pictureCollapsingOptimizationVisualCountThreshold)
			{
				// Walk children individually. A subtree is collapsed only after it has been fully static for
				// several frames, so the cached picture never freezes live content and a collapsed subtree
				// contributes no per-frame damage (see EnablePictureCollapsingOptimization).
				foreach (var child in visual.GetChildrenInRenderOrder())
				{
					child.Render(in session, applyChildOptimization);
				}
			}
			else
			{
				var recorder = new SKPictureRecorder();
				var recordingCanvas = recorder.BeginRecording(InfiniteClipRect);
				// child.Render will reapply the total transform matrix, so we need to invert ours.
				Matrix4x4.Invert(visual.TotalMatrix, out var rootTransform);
				_factory.CreateInstance(visual, recordingCanvas, ref rootTransform, session.Opacity, session.Damage, out var childSession);
				using (childSession)
				{
					foreach (var child in visual.GetChildrenInRenderOrder())
					{
						child.Render(in childSession, applyChildOptimization: false);
					}
				}

				var picture = IntPtr.Zero;

				unsafe
				{
					picture = UnoSkiaApi.sk_picture_recorder_end_recording(recorder.Handle);
					UnoSkiaApi.sk_canvas_draw_picture(session.Canvas.Handle, picture, null, IntPtr.Zero);
				}

				// The visual can be set on a ChildrenSKPictureInvalid path after the render has started.
				// In such case, we should not cache this picture. Not only it is outdated, it will also lead to a corrupted state,
				// where subtree rendering is skipped with the cached picture,
				// and its descendant can't invalidate the cached picture since they area already on a ChildrenSKPictureInvalid path.
				if ((visual._flags & VisualFlags.ChildrenSKPictureInvalid) == 0)
				{
					if (visual._childrenPicture != IntPtr.Zero)
					{
						UnoSkiaApi.sk_refcnt_safe_unref(visual._childrenPicture);
					}

					visual._childrenPicture = picture;
				}
				else
				{
					UnoSkiaApi.sk_refcnt_safe_unref(picture);
				}
			}
		}
	}

	internal void GetNativeViewPathAndZOrder(SKPath clipFromParent, SKPath clipPath, List<Visual> nativeVisualsInZOrder)
	{
		if (this is { Opacity: 0 } or { IsVisible: false } || clipFromParent.IsEmpty)
		{
			return;
		}

		var localClipCombinedByClipFromParent = _pathPool.Allocate();
		using var rentedArrayDisposable = new DisposableStruct<SKPath>(static path => _pathPool.Free(path), localClipCombinedByClipFromParent);
		localClipCombinedByClipFromParent.Rewind();

		if (GetPrePaintingClipping(_spareRenderPath))
		{
			localClipCombinedByClipFromParent.AddPath(_spareRenderPath);
		}
		else
		{
			localClipCombinedByClipFromParent.AddRect(new SKRect(0, 0, Size.X, Size.Y));
		}
		localClipCombinedByClipFromParent.Transform(TotalMatrix.ToSKMatrix(), localClipCombinedByClipFromParent);
		localClipCombinedByClipFromParent.Op(clipFromParent, SKPathOp.Intersect, localClipCombinedByClipFromParent);

		if (IsNativeHostVisual || CanPaint())
		{
			clipPath.Op(localClipCombinedByClipFromParent, IsNativeHostVisual ? SKPathOp.Union : SKPathOp.Difference, clipPath);
		}

		if (IsNativeHostVisual && !localClipCombinedByClipFromParent.IsEmpty)
		{
			nativeVisualsInZOrder.Add(this);
		}

		if (GetPostPaintingClipping() is { } postClip)
		{
			postClip.Transform(TotalMatrix.ToSKMatrix(), postClip);
			localClipCombinedByClipFromParent.Op(postClip, SKPathOp.Intersect, localClipCombinedByClipFromParent);
		}
		foreach (var child in GetChildrenInRenderOrder())
		{
			child.GetNativeViewPathAndZOrder(localClipCombinedByClipFromParent, clipPath, nativeVisualsInZOrder);
		}
	}

	internal void GetTotalClipPath(SKPath dst, bool skipPostPaintingClipping)
	{
		if (Parent is Visual parent)
		{
			parent.GetTotalClipPath(dst, false);
		}
		else
		{
			dst.Rewind();
			dst.AddRect(InfiniteClipRect);
		}

		var localPath = _pathPool.Allocate();
		using var localPathDisposable = new DisposableStruct<SKPath>(static path => _pathPool.Free(path), localPath);

		var totalMatrix = TotalMatrix.ToSKMatrix();
		if (GetPrePaintingClipping(localPath))
		{
			// The local clip is in local coordinates. We need to transform it to root coordinates.
			localPath.Transform(in totalMatrix);
			dst.Op(localPath, SKPathOp.Intersect, dst);
		}

		if (!skipPostPaintingClipping)
		{
			if (GetPostPaintingClipping() is { } postClip)
			{
				postClip.Transform(in totalMatrix);
				dst.Op(postClip, SKPathOp.Intersect, dst);
			}
		}
	}

	/// <summary>
	/// Returns the bounds, in root visual coordinates, of the effective clip applied to this visual's
	/// own content by its ancestors (e.g. a ScrollViewer's viewport clip) and its own <see cref="Clip"/>.
	/// Intersecting an element's bounds with this rect yields what's actually visible, which automation
	/// uses to detect elements clipped entirely out of view (e.g. scrolled outside a ScrollViewer).
	/// </summary>
	internal Rect GetTotalClipRectInRootCoordinates()
	{
		var clipPath = _pathPool.Allocate();
		using var clipPathDisposable = new DisposableStruct<SKPath>(static path => _pathPool.Free(path), clipPath);
		clipPath.Rewind();

		// skipPostPaintingClipping: true — a visual's own post-painting clip only affects its children,
		// not the visual itself. Ancestor post-painting clips are still applied via the parent recursion.
		GetTotalClipPath(clipPath, skipPostPaintingClipping: true);

		return clipPath.Bounds.ToRect();
	}

	/// <summary>
	/// Draws the content of this visual.
	/// </summary>
	/// <param name="session">The drawing session to use.</param>
	internal virtual void Paint(in PaintingSession session) { }

	private protected virtual bool TryAddShadowPaths(List<(SKPath path, float alpha)> output) => !CanPaint();

	private bool TryRenderAnalyticShadow(SKCanvas canvas, ShadowState shadow)
	{
		var rootMatrix = TotalMatrix.ToSKMatrix();
		if (!rootMatrix.TryInvert(out var inverseRoot))
		{
			return false;
		}

		using var accumulator = new ShadowPathAccumulator();
		if (!WalkShadowSilhouette(this, this, in inverseRoot, ancestorClipInRoot: null, 1f, accumulator))
		{
			return false;
		}

		var totalRegions = accumulator.Count;
		if (totalRegions == 0)
		{
			return true; // nothing to cast a shadow from; analytic path succeeded vacuously
		}

		var sigma = shadow.SigmaX;
		using var maskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sigma);
		var shadowSKColor = shadow.Color.ToSKColor();

		canvas.Save();
		canvas.Translate(shadow.Dx, shadow.Dy);

		var pathYScale = 1f;
		if (!shadow.SigmaX.Equals(shadow.SigmaY) && !shadow.SigmaX.Equals(0f))
		{
			// SKMaskFilter only supports a single sigma. To get anisotropic device-space blur (SigmaX, SigmaY)
			// we exploit respectCTM=true: the user-space sigma is multiplied by |CTM scale| per axis. So we
			// pick sigma = SigmaX, apply canvas.Scale(1, SigmaY/SigmaX), and pre-scale each region's path by
			// (1, SigmaX/SigmaY) to cancel the visual stretch — the geometry lands at original coordinates
			// while the blur becomes (SigmaX, SigmaY) in device pixels. When sigmas are equal (the common
			// SetElevation case) we skip the scaling entirely.
			var sy_over_sx = shadow.SigmaY / shadow.SigmaX;
			canvas.Scale(1f, sy_over_sx);
			pathYScale = 1f / sy_over_sx;
		}

		if (totalRegions > 1)
		{
			// Isolate accumulation so Plus blend sums region contributions without polluting the canvas
			// behind the shadow.
			canvas.SaveLayer();
			if (accumulator.OpaqueSilhouette is { } opaque)
			{
				DrawRegionShadow(canvas, opaque, 1f, shadowSKColor, maskFilter, useAdditive: true, pathYScale);
			}
			foreach (var (path, alpha) in accumulator.Regions)
			{
				DrawRegionShadow(canvas, path, alpha, shadowSKColor, maskFilter, useAdditive: true, pathYScale);
			}
			canvas.Restore();
		}
		else
		{
			// avoiding the SaveLayer was measured to be a significant perf win for the common case of a single region
			if (accumulator.OpaqueSilhouette is { } opaque)
			{
				DrawRegionShadow(canvas, opaque, 1f, shadowSKColor, maskFilter, useAdditive: false, pathYScale);
			}
			else
			{
				var (path, alpha) = accumulator.Regions[0];
				DrawRegionShadow(canvas, path, alpha, shadowSKColor, maskFilter, useAdditive: false, pathYScale);
			}
		}

		canvas.Restore();
		return true;

		static void DrawRegionShadow(SKCanvas canvas, SKPath path, float alpha, SKColor shadowColor, SKMaskFilter? maskFilter, bool useAdditive, float pathYScale)
		{
			using var paint = new SKPaint
			{
				IsAntialias = true,
				Color = shadowColor.WithAlpha((byte)(shadowColor.Alpha * alpha)),
				MaskFilter = maskFilter,
				BlendMode = useAdditive ? SKBlendMode.Plus : SKBlendMode.SrcOver,
			};

			if (pathYScale.Equals(1f))
			{
				canvas.DrawPath(path, paint);
			}
			else
			{
				// Cancel the canvas Y-scale on the geometry so the shape lands at its original
				// position; the canvas scale only affects the mask blur's per-axis sigma.
				var scratch = _spareShadowPath;
				scratch.Rewind();
				path.Transform(SKMatrix.CreateScale(1f, pathYScale), scratch);
				canvas.DrawPath(scratch, paint);
			}
		}
	}

	private static bool WalkShadowSilhouette(
		Visual visual,
		Visual shadowRoot,
		in SKMatrix inverseRootMatrix,
		SKPath? ancestorClipInRoot,
		float opacityChain,
		ShadowPathAccumulator accumulator)
	{
		var scratch = _spareShadowContributions;
		if (visual.Opacity == 0f || !visual.IsVisible)
		{
			return true;
		}
		// A self-shadowed descendant renders its own drop shadow; including its silhouette in the ancestor
		// would double-cast.
		if (visual != shadowRoot && visual.ShadowState is not null)
		{
			return true;
		}

		var visualMatrix = visual.TotalMatrix.ToSKMatrix();
		var toRoot = SKMatrix.Concat(inverseRootMatrix, visualMatrix);

		var clipPath = _pathPool.Allocate();
		using var clipPathDisposable = new DisposableStruct<SKPath>(static p => _pathPool.Free(p), clipPath);
		clipPath.Rewind();

		var hasClip = TryPopulateEffectiveClipInRoot(visual, in toRoot, clipPath);

		// Intersect with the accumulated ancestor clip. After this block hasClip is unconditionally true.
		if (ancestorClipInRoot is not null)
		{
			if (hasClip)
			{
				clipPath.Op(ancestorClipInRoot, SKPathOp.Intersect, clipPath);
			}
			else
			{
				clipPath.AddPath(ancestorClipInRoot);
			}
			hasClip = true;
		}

		// Skip optimization (scoped to THIS visual, not the subtree): if the visual's own painting is
		// guaranteed to land inside the opaque silhouette, we can skip its TryAddShadowPaths call.
		// PaintsWithinOwnSize lets Size act as an upper bound on its painting (intersected with the
		// effective clip). When that's not available we fall back to the effective clip itself, which is
		// a sound upper bound when present. Either way, this only short-circuits THIS visual's
		// contribution — children are still walked, because the Size bound is per-visual, not per-subtree.
		var canSkipOwnContribution = false;
		if (visual is { PaintsWithinOwnSize: true, Size: { X: > 0, Y: > 0 } size })
		{
			var sizeCandidate = _spareShadowPath;
			sizeCandidate.Rewind();
			sizeCandidate.AddRect(new SKRect(0, 0, size.X, size.Y));
			sizeCandidate.Transform(toRoot);
			if (hasClip)
			{
				sizeCandidate.Op(clipPath, SKPathOp.Intersect, sizeCandidate);
			}
			canSkipOwnContribution = accumulator.IsFullyCovered(sizeCandidate);
		}
		else if (hasClip)
		{
			canSkipOwnContribution = accumulator.IsFullyCovered(clipPath);
		}

		var combinedOpacity = opacityChain * visual.Opacity;

		if (!canSkipOwnContribution)
		{
			// scratch is always empty on entry — the previous visit clears it before recursing.
			if (!visual.TryAddShadowPaths(scratch))
			{
				return false;
			}

			foreach (var (path, alpha) in scratch)
			{
				var transformed = _spareShadowPath;
				transformed.Rewind();
				path.Transform(toRoot, transformed);

				if (hasClip)
				{
					if (transformed.Op(clipPath, SKPathOp.Intersect, transformed) && !transformed.IsEmpty)
					{
						accumulator.Add(transformed, alpha * combinedOpacity);
					}
				}
				else
				{
					accumulator.Add(transformed, alpha * combinedOpacity);
				}

				path.Dispose();
			}
			scratch.Clear();
		}

		// Apply the post-painting clip to derive the clip for children. We can mutate clipPath in place —
		// its previous value (the visual's own clip) is no longer needed past this point.
		var postClipLocal = visual.GetPostPaintingClipping();
		if (postClipLocal is not null)
		{
			var postClipInRoot = _spareShadowPath;
			postClipInRoot.Rewind();
			postClipLocal.Transform(toRoot, postClipInRoot);

			if (hasClip)
			{
				clipPath.Op(postClipInRoot, SKPathOp.Intersect, clipPath);
			}
			else
			{
				clipPath.AddPath(postClipInRoot);
			}
			hasClip = true;
		}

		SKPath? childClipInRoot = hasClip ? clipPath : null;

		foreach (var child in visual.GetChildrenInRenderOrder())
		{
			if (!WalkShadowSilhouette(child, shadowRoot, in inverseRootMatrix, childClipInRoot, combinedOpacity, accumulator))
			{
				return false;
			}
		}

		return true;
	}

	private static bool TryPopulateEffectiveClipInRoot(Visual visual, in SKMatrix toRoot, SKPath dst)
	{
		var preClipLocal = _spareShadowPath;
		preClipLocal.Rewind();
		if (visual.GetPrePaintingClipping(preClipLocal))
		{
			preClipLocal.Transform(toRoot, dst);
			return true;
		}
		return false;
	}

	private Vector3 GetTotalOffset()
	{
		var total = new Vector3(
			Offset.X + ArrangeOffset.X,
			Offset.Y + ArrangeOffset.Y,
			Offset.Z + ArrangeOffset.Z
		);

		if (IsTranslationEnabled && Properties.TryGetVector3("Translation", out var translation) == CompositionGetValueStatus.Succeeded)
		{
			// WARNING: DO NOT change this to plain "return Offset + translation;"
			// as this results in very wrong values on Android when debugger is not attached.
			// https://github.com/dotnet/runtime/issues/114094
			return new Vector3(total.X + translation.X, total.Y + translation.Y, total.Z + translation.Z);
		}

		return total;
	}

	internal virtual bool GetPrePaintingClipping(SKPath dst)
	{
		// Apply the clipping defined on the element
		// (Only the Clip property, clipping applied by parent for layout constraints reason it's managed by the ContainerVisual through the LayoutClip)
		// Note: The Clip is applied after the transformation matrix, so it's also transformed.
		if (Clip is not null)
		{
			dst.Reset();
			dst.AddPath(Clip?.GetClipPath(this));
			return true;
		}
		return false;
	}

	/// <summary>This clipping won't affect the visual itself, but its children.</summary>
	private protected virtual SKPath? GetPostPaintingClipping() => null;
	/// <summary>This can be overriden if some Visuals can apply the clipping more optimally than generating a path
	/// and then applying the clip. Specifically, if the clipping is a simple rectangle, creating an SKPath with the
	/// rectangle might be a lot more overhead than just calling SKCanvas.ClipRect, specifically on WASM.</summary>
	private protected virtual void ApplyPostPaintingClipping(SKCanvas canvas)
	{
		if (GetPostPaintingClipping() is { } postClip)
		{
			canvas.ClipPath(postClip, antialias: true);
		}
	}

	/// <remarks>You should NOT mutate the list returned by this method.</remarks>
	// NOTE: Returning List<Visual> so that enumerating doesn't cause boxing.
	// This has the side effect of having to return an empty list here.
	// The caller then shouldn't mutate the list, otherwise, things will go wrong badly.
	// An alternative is to return null and check for null on the call sites.
	private protected virtual List<Visual> GetChildrenInRenderOrder() => s_emptyList;

	/// <remarks>You should NOT mutate the list returned by this method.</remarks>
	internal List<Visual> GetChildrenInRenderOrderTestingOnly() => GetChildrenInRenderOrder();

	internal virtual int GetSubTreeVisualCount() => 1;

	/// <summary>
	/// Creates a new <see cref="PaintingSession"/> set up with the local coordinates and opacity.
	/// </summary>
	private unsafe void CreateLocalSession(in PaintingSession parentSession, out PaintingSession session)
	{
		var canvas = parentSession.Canvas;

		ref var rootTransform = ref parentSession.RootTransform;

		var opacity = Opacity == 1.0f ? parentSession.Opacity : parentSession.Opacity * Opacity;

		_factory.CreateInstance(this, canvas, ref rootTransform, opacity, parentSession.Damage, out session);

		if ((_flags & VisualFlags.MatrixDirty) != 0 || !_totalMatrix.isLocalMatrixIdentity)
		{
			Matrix4x4 totalMatrix;

			if (Unsafe.IsNullRef(ref rootTransform))
			{
				totalMatrix = TotalMatrix;
			}
			else
			{
				totalMatrix = TotalMatrix * rootTransform;
			}

			if (!_totalMatrix.isLocalMatrixIdentity)
			{
				// this avoids the matrix copying in canvas.SetMatrix()
				UnoSkiaApi.sk_canvas_set_matrix(canvas.Handle, (SKMatrix44*)&totalMatrix);
			}
		}
#if DEBUG
		else
		{
			Debug.Assert(Unsafe.IsNullRef(ref rootTransform)
				? canvas.TotalMatrix == TotalMatrix.ToSKMatrix()
				// Due to the limit precision of doubles, instead of comparing the two matrices directly we compare the Frobenius norm of their difference to zero
				: CompositionMathHelpers.IsCloseRealZero((canvas.TotalMatrix.ToMatrix4x4() - TotalMatrix * rootTransform).ToSKMatrix().Values.Sum(i => i * i), 1e-5f));
		}
#endif
	}

	internal void PrintSubtree(StringBuilder sb, int indent = 0)
	{
		var indentation = new string(' ', indent * 2);
		sb.Append(indentation);
		sb.Append('[');
		sb.Append(Comment);
		sb.Append("]: ");
		sb.Append("Subtree count: [");
		sb.Append(GetSubTreeVisualCount());
		sb.Append("], flags: [");
		sb.Append(_flags);
		sb.Append("], _totalMatrix: [");
		sb.Append(_totalMatrix.matrix);
		sb.Append(']');
		sb.Append("], _framesSinceSubtreeNotChanged: [");
		sb.Append(_framesSinceSubtreeNotChanged);
		sb.Append(']');
		sb.AppendLine();
		foreach (var child in GetChildrenInRenderOrder())
		{
			child.PrintSubtree(sb, indent + 1);
		}
	}

	[Flags]
	internal enum VisualFlags : byte
	{
		IsNativeHostVisualSet = 1, // Is the IsNativeHostVisual bit valid?
		IsNativeHostVisual = 2,
		IsNativeHostVisualInherited = 4,
		MatrixDirty = 8,
		PaintDirty = 16,
		ChildrenSKPictureInvalid = 32, // some child in the subtree of this visual is dirty.
	}
}
