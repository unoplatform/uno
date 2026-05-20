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

	private static readonly IPrivateSessionFactory _factory = new PaintingSession.SessionFactory();
	private static readonly List<Visual> s_emptyList = new List<Visual>();

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

	// Cached blurred shadow output. When a Visual has a ShadowState we render
	// the subtree picture once through the shadow image filter and snapshot
	// the result here; subsequent renders draw the cached image (via 9-slice to
	// fit the current Visual size) instead of running the blur each frame.
	private SKImage? _shadowOnlyImage;
	private int _shadowOnlyImageBlurMargin;
	private Vector2 _shadowOnlyImageSize;        // Cached content size (bucketed up to ShadowCacheGrowStride).
	private ShadowState? _shadowOnlyImageStateKey;

	// Coarse stride applied to the cached content size so that small drag/scroll-
	// induced size changes don't force a rebuild. With a 128 px stride a Visual
	// that grows 410→450→500→550→600 px hits buckets {512, 640} = 2 rebuilds,
	// instead of one per pixel.
	private const int ShadowCacheGrowStride = 128;

	// Refuse cache allocations beyond this dimension in either axis; fall back
	// to the unbatched per-frame path. Protects against pathological Visual
	// sizes producing multi-megabyte surfaces.
	private const int MaxShadowCacheDim = 4096;

	// Bounded pool of reusable SKSurfaces keyed by exact (width, height). Avoids
	// per-build pixel-buffer allocations when the same cache size is rebuilt.
	private readonly struct PooledShadowSurface
	{
		public PooledShadowSurface(SKSurface surface, int width, int height)
		{
			Surface = surface;
			Width = width;
			Height = height;
		}
		public SKSurface Surface { get; }
		public int Width { get; }
		public int Height { get; }
	}
	private static readonly List<PooledShadowSurface> s_shadowSurfacePool = new();
	private const int MaxPooledShadowSurfaces = 24;

	private static SKSurface? RentShadowSurface(int width, int height)
	{
		lock (s_shadowSurfacePool)
		{
			for (var i = 0; i < s_shadowSurfacePool.Count; i++)
			{
				var entry = s_shadowSurfacePool[i];
				if (entry.Width == width && entry.Height == height)
				{
					s_shadowSurfacePool.RemoveAt(i);
					return entry.Surface;
				}
			}
		}

		try
		{
			return SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
		}
		catch
		{
			return null;
		}
	}

	private static void ReturnShadowSurface(SKSurface surface, int width, int height)
	{
		lock (s_shadowSurfacePool)
		{
			if (s_shadowSurfacePool.Count < MaxPooledShadowSurfaces)
			{
				s_shadowSurfacePool.Add(new PooledShadowSurface(surface, width, height));
				return;
			}
		}
		surface.Dispose();
	}

	internal void DisposeShadowCache()
	{
		if (_shadowOnlyImage is not null)
		{
			_shadowOnlyImage.Dispose();
			_shadowOnlyImage = null;
			_shadowOnlyImageStateKey = null;
		}
	}

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

	// this is for effect brushes that apply an effect on an already-drawn area, so these need to be painted every frame.
	internal virtual bool RequiresRepaintOnEveryFrame => false;

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
		=> VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged?.Invoke(this);

	/// <summary>
	/// Render a visual as if it's the root visual.
	/// </summary>
	/// <param name="canvas">The canvas on which this visual should be rendered.</param>
	/// <param name="offsetOverride">The offset (from the origin) to render the Visual at. If null, the offset properties on the Visual like <see cref="Offset"/> and <see cref="AnchorPoint"/> are used.</param>
	internal void RenderRootVisual(SKCanvas canvas, Vector2? offsetOverride)
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
		}
		else
		{
			_framesSinceSubtreeNotChanged = 0;
			_flags &= ~VisualFlags.ChildrenSKPictureInvalid;
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

			if (ShadowState is null)
			{
				PaintStep(this, session);
				PostPaintingClipStep(this, canvas);
				RenderChildrenStep(this, session, applyChildOptimization);
			}
			else
			{
				RenderWithShadowCache(in session, canvas, applyChildOptimization);
			}
		}
	}

	private static void PaintStep(Visual visual, in PaintingSession session)
	{
		// Rendering shouldn't depend on matrix or clip adjustments happening in a visual's Paint. That should
		// be specific to that visual and should not affect the rendering of any other visual.
#if DEBUG
		var saveCount = session.Canvas.SaveCount;
#endif
		if (visual.RequiresRepaintOnEveryFrame)
		{
			// why bother with a recorder when it's going to get repainted next frame? just paint directly
			visual.Paint(session);
		}
		else
		{
			if ((visual._flags & VisualFlags.PaintDirty) != 0)
			{
				visual._flags &= ~VisualFlags.PaintDirty;

				var recordingCanvas = _recorder.BeginRecording(InfiniteClipRect);
				_factory.CreateInstance(visual, recordingCanvas, ref session.RootTransform, session.Opacity, out var recorderSession);
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

	private static void PostPaintingClipStep(Visual visual, SKCanvas canvas)
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

	private static void RenderChildrenStep(Visual visual, PaintingSession session, bool applyChildOptimization)
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
			_factory.CreateInstance(visual, recordingCanvas, ref rootTransform, session.Opacity, out var childSession);
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

	// Cached-shadow render path. Renders the subtree picture once through the
	// blur image filter into a pooled SKSurface, snapshots the result as an
	// SKImage (keyed on the ShadowState reference and the bucketed Visual
	// size), and reuses that image on subsequent renders. Size changes within
	// the cached dimensions are handled via 9-slice scaling instead of
	// rebuilding; growth past the cached size rebuilds at the next stride
	// bucket. Mirrors WinUI's own SpriteVisual+NineGrid optimization for
	// ThemeShadow.
	private void RenderWithShadowCache(in PaintingSession session, SKCanvas canvas, bool applyChildOptimization)
	{
		var shadowState = ShadowState!;
		var sigma = Math.Max(shadowState.SigmaX, shadowState.SigmaY);
		var blurMargin = (int)Math.Ceiling(sigma * 3
			+ Math.Max(Math.Abs(shadowState.Dx), Math.Abs(shadowState.Dy))
			+ 4);

		var actualW = Math.Max(1, (int)Math.Ceiling(Size.X));
		var actualH = Math.Max(1, (int)Math.Ceiling(Size.Y));

		// 9-slice needs at least 2*blurMargin in each axis (corners) plus a
		// small middle stretch region to be well-defined.
		var minCachedContentW = 2 * blurMargin + 8;
		var minCachedContentH = 2 * blurMargin + 8;

		var cachedContentW = (int)_shadowOnlyImageSize.X;
		var cachedContentH = (int)_shadowOnlyImageSize.Y;

		var cacheValid =
			_shadowOnlyImage is not null
			&& ReferenceEquals(_shadowOnlyImageStateKey, shadowState)
			&& _shadowOnlyImageBlurMargin == blurMargin
			&& cachedContentW >= actualW
			&& cachedContentH >= actualH;

		if (!cacheValid)
		{
			DisposeShadowCache();

			static int RoundUpToStride(int v) => ((v + ShadowCacheGrowStride - 1) / ShadowCacheGrowStride) * ShadowCacheGrowStride;

			var cacheW = Math.Max(RoundUpToStride(actualW), minCachedContentW);
			var cacheH = Math.Max(RoundUpToStride(actualH), minCachedContentH);
			var imgW = cacheW + 2 * blurMargin;
			var imgH = cacheH + 2 * blurMargin;

			if (imgW > MaxShadowCacheDim || imgH > MaxShadowCacheDim)
			{
				// Too large to cache reasonably; fall back to per-frame blur draw.
				RenderShadowUncached(in session, canvas, applyChildOptimization);
				return;
			}

			using var recorder = new SKPictureRecorder();
			var recordingCanvas = recorder.BeginRecording(InfiniteClipRect);
			// child.Render will reapply the total transform matrix, so we need
			// to invert ours.
			Matrix4x4.Invert(TotalMatrix, out var rootTransform);
			_factory.CreateInstance(this, recordingCanvas, ref rootTransform, session.Opacity, out var childSession);
			using (childSession)
			{
				PaintStep(this, childSession);
				PostPaintingClipStep(this, recordingCanvas);
				RenderChildrenStep(this, childSession, applyChildOptimization);
			}
			using var picture = recorder.EndRecording();

			var surface = RentShadowSurface(imgW, imgH);
			if (surface is null)
			{
				// Surface allocation failed; fall back to per-frame blur draw on
				// the just-recorded picture.
				canvas.DrawPicture(picture, shadowState.ShadowOnlyPaint);
				canvas.DrawPicture(picture);
				return;
			}

			var surfaceCanvas = surface.Canvas;
			surfaceCanvas.ResetMatrix();
			surfaceCanvas.Clear(SKColors.Transparent);
			surfaceCanvas.Translate(blurMargin, blurMargin);
			// Single blur image-filter pass per cache entry — the work we are
			// trying to avoid running every frame in the prior implementation.
			surfaceCanvas.DrawPicture(picture, shadowState.ShadowOnlyPaint);

			_shadowOnlyImage = surface.Snapshot();
			// Skia's snapshot is copy-on-write; the next time the surface is
			// drawn into, the image's pixels are detached, so it's safe to
			// return the surface to the pool now.
			ReturnShadowSurface(surface, imgW, imgH);

			_shadowOnlyImageBlurMargin = blurMargin;
			_shadowOnlyImageSize = new Vector2(cacheW, cacheH);
			_shadowOnlyImageStateKey = shadowState;
			cachedContentW = cacheW;
			cachedContentH = cacheH;

			// Freshly built — draw cached shadow + the just-recorded content
			// picture (avoids re-rendering the subtree for the content layer).
			DrawShadowNineSlice(canvas, _shadowOnlyImage, blurMargin, cachedContentW, cachedContentH, actualW, actualH);
			canvas.DrawPicture(picture);
		}
		else
		{
			DrawShadowNineSlice(canvas, _shadowOnlyImage!, _shadowOnlyImageBlurMargin, cachedContentW, cachedContentH, actualW, actualH);

			// Render content directly on the outer canvas (no recording wrapper).
			PaintStep(this, session);
			PostPaintingClipStep(this, canvas);
			RenderChildrenStep(this, session, applyChildOptimization);
		}
	}

	// Pre-cache fallback: record subtree and apply the blur image filter at
	// draw time. Used when the cached image bounds would exceed
	// MaxShadowCacheDim (rare).
	private void RenderShadowUncached(in PaintingSession session, SKCanvas canvas, bool applyChildOptimization)
	{
		using var recorder = new SKPictureRecorder();
		var recordingCanvas = recorder.BeginRecording(InfiniteClipRect);
		Matrix4x4.Invert(TotalMatrix, out var rootTransform);
		_factory.CreateInstance(this, recordingCanvas, ref rootTransform, session.Opacity, out var childSession);
		using (childSession)
		{
			PaintStep(this, childSession);
			PostPaintingClipStep(this, recordingCanvas);
			RenderChildrenStep(this, childSession, applyChildOptimization);
		}
		using var picture = recorder.EndRecording();

		canvas.DrawPicture(picture, ShadowState!.ShadowOnlyPaint);
		canvas.DrawPicture(picture);
	}

	// Draws the cached blurred shadow image into the outer canvas using
	// 9-slice scaling: the four corner regions stay 1:1 (preserving blur
	// shape), the four edges stretch along one axis, and the middle
	// stretches both ways. The cached image is sized
	// (cachedContentW + 2*blurMargin) × (cachedContentH + 2*blurMargin),
	// laid out so the visual's local origin maps to pixel
	// (blurMargin, blurMargin).
	private static void DrawShadowNineSlice(
		SKCanvas canvas,
		SKImage image,
		int blurMargin,
		int cachedContentW,
		int cachedContentH,
		int actualW,
		int actualH)
	{
		var srcLeft = 0;
		var srcInnerLeft = blurMargin;
		var srcInnerRight = blurMargin + cachedContentW;
		var srcRight = (2 * blurMargin) + cachedContentW;

		var srcTop = 0;
		var srcInnerTop = blurMargin;
		var srcInnerBottom = blurMargin + cachedContentH;
		var srcBottom = (2 * blurMargin) + cachedContentH;

		var dstLeft = -blurMargin;
		var dstInnerLeft = 0;
		var dstInnerRight = actualW;
		var dstRight = actualW + blurMargin;

		var dstTop = -blurMargin;
		var dstInnerTop = 0;
		var dstInnerBottom = actualH;
		var dstBottom = actualH + blurMargin;

		DrawShadowPatch(canvas, image, srcLeft, srcTop, srcInnerLeft, srcInnerTop, dstLeft, dstTop, dstInnerLeft, dstInnerTop);
		DrawShadowPatch(canvas, image, srcInnerLeft, srcTop, srcInnerRight, srcInnerTop, dstInnerLeft, dstTop, dstInnerRight, dstInnerTop);
		DrawShadowPatch(canvas, image, srcInnerRight, srcTop, srcRight, srcInnerTop, dstInnerRight, dstTop, dstRight, dstInnerTop);

		DrawShadowPatch(canvas, image, srcLeft, srcInnerTop, srcInnerLeft, srcInnerBottom, dstLeft, dstInnerTop, dstInnerLeft, dstInnerBottom);
		DrawShadowPatch(canvas, image, srcInnerLeft, srcInnerTop, srcInnerRight, srcInnerBottom, dstInnerLeft, dstInnerTop, dstInnerRight, dstInnerBottom);
		DrawShadowPatch(canvas, image, srcInnerRight, srcInnerTop, srcRight, srcInnerBottom, dstInnerRight, dstInnerTop, dstRight, dstInnerBottom);

		DrawShadowPatch(canvas, image, srcLeft, srcInnerBottom, srcInnerLeft, srcBottom, dstLeft, dstInnerBottom, dstInnerLeft, dstBottom);
		DrawShadowPatch(canvas, image, srcInnerLeft, srcInnerBottom, srcInnerRight, srcBottom, dstInnerLeft, dstInnerBottom, dstInnerRight, dstBottom);
		DrawShadowPatch(canvas, image, srcInnerRight, srcInnerBottom, srcRight, srcBottom, dstInnerRight, dstInnerBottom, dstRight, dstBottom);
	}

	private static void DrawShadowPatch(
		SKCanvas canvas,
		SKImage image,
		int sl, int st, int sr, int sb,
		int dl, int dt, int dr, int db)
	{
		if (sl >= sr || st >= sb || dl >= dr || dt >= db)
		{
			// Zero/negative-area patch (e.g. visual smaller than 2*blurMargin); skip.
			return;
		}
		var src = new SKRect(sl, st, sr, sb);
		var dst = new SKRect(dl, dt, dr, db);
		canvas.DrawImage(image, src, dst);
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
	/// Draws the content of this visual.
	/// </summary>
	/// <param name="session">The drawing session to use.</param>
	internal virtual void Paint(in PaintingSession session) { }

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

		_factory.CreateInstance(this, canvas, ref rootTransform, opacity, out session);

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
