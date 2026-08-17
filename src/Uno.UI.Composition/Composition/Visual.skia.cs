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
using Uno.Extensions;
using Uno.Helpers;
using Uno.UI.Composition;
using Uno.UI.Composition.Composition;
using Uno.UI.Composition.Drawing;


namespace Microsoft.UI.Composition;

public partial class Visual : global::Microsoft.UI.Composition.CompositionObject
{
	// Scratch list used only inside the analytic-shadow silhouette walker. Each visit calls
	// TryAddShadowPaths into this list, drains it, and Clear()s before recursing into children, so a
	// single static instance is safe.
	private static readonly List<(IGeometry path, float alpha)> _spareShadowContributions = new();

	private static readonly IPrivateSessionFactory _factory = new PaintingSession.SessionFactory();
	private static readonly List<Visual> s_emptyList = new List<Visual>();

	internal static bool EnablePictureCollapsingOptimization { get; set; } = true;
	internal static int PictureCollapsingOptimizationFrameThreshold { get; set; } = 50;
	internal static int PictureCollapsingOptimizationVisualCountThreshold { get; set; } = 100;

	/// <summary>Test seam: forces the command-list retained fallback even when the backend has native retention.</summary>
	internal static bool ForceFallbackRetainedRendering { get; set; }

	// Retention is always available: the registered backend's native recorder, or the neutral command-list
	// fallback (also forced by the ForceFallbackRetainedRendering test seam). Replay is on the IRenderRecord
	// (data.Replay(session)); a native recording only replays into its own backend's session, the command-list
	// fallback into any.
	private static ICommandRecorder CreateRecording()
		=> ForceFallbackRetainedRendering ? new CommandListRecorder() : DrawingFactory.Current.CreateRecording();

	private bool _enablePictureCollapsingOptimization;
	private int _pictureCollapsingOptimizationFrameThreshold;
	private int _pictureCollapsingOptimizationVisualCountThreshold;

	private CompositionClip? _clip;
	private Vector2 _anchorPoint = Vector2.Zero; // Backing for scroll offsets
	private int _zIndex;
	private (Matrix4x4 matrix, bool isLocalMatrixIdentity) _totalMatrix = (Matrix4x4.Identity, true);
	// Opaque per-visual retained state owned by the rendering backend (Skia: an SKPicture). _content is
	// this visual's own painted content; _childrenContent is a collapsed subtree cache.
	private IRenderRecord? _content;
	private IRenderRecord? _childrenContent;
	private int _framesSinceSubtreeNotChanged;

	private VisualFlags _flags = VisualFlags.MatrixDirty | VisualFlags.PaintDirty | VisualFlags.ChildrenSKPictureInvalid;

	private const int SK_MaxS32FitsInFloat = 2147483520;
	// Skia uses SafeEdge = SK_MaxS32FitsInFloat / 2 - 1, but that causes clipping bounds issues in SKCanvasElement when used with LottieVisualSourceBase
	private const int SafeEdge = SK_MaxS32FitsInFloat / 4 - 1;
	// if we use float.Min/MaxValue, weird overflows happen and clipping breaks badly.
	// https://github.com/mono/skia/blob/927041a58f130e0dd0562ba86cb4170989ad39e9/src/core/SkRecorder.cpp#L79
	// https://github.com/mono/skia/blob/927041a58f130e0dd0562ba86cb4170989ad39e9/src/core/SkRectPriv.h#L38
	internal static Rect InfiniteClipRect { get; } = new(-SafeEdge, -SafeEdge, SafeEdge * 2d, SafeEdge * 2d);

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
		_content?.Dispose();
		_content = null;
		_flags |= VisualFlags.PaintDirty;
		InvalidateParentChildrenPicture(false);
	}

	/// <summary>
	/// Discards this visual's and its whole subtree's cached recordings. Used when the active
	/// <see cref="Microsoft.UI.Xaml.Media.CompositionTarget"/> renderer changes (e.g. the WebGPU device is
	/// imported asynchronously on WebAssembly, replacing the default Skia renderer): recordings retained by the
	/// previous backend can't be replayed by the new one, so every visual must re-record under it.
	/// </summary>
	internal void InvalidatePaintRecursive()
	{
		InvalidatePaint();
		foreach (var child in GetChildrenInRenderOrder())
		{
			child.InvalidatePaintRecursive();
		}
	}

	internal void InvalidateParentChildrenPicture(bool includeSelf)
	{
		var parent = includeSelf ? this : Parent;
		while (parent is not null && (parent._flags & VisualFlags.ChildrenSKPictureInvalid) == 0)
		{
			parent._childrenContent?.Dispose();
			parent._childrenContent = null;
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
	internal void RenderRootVisual(IDrawingSession drawingSession, Vector2? offsetOverride, DamageRegion? damage = null)
	{
		if (this is { Opacity: 0 } or { IsVisible: false })
		{
			return;
		}

		// Since we're acting as if this visual is a root visual, we undo the parent's TotalMatrix
		// so that when concatenated with this visual's TotalMatrix, the result is only the transforms
		// from this visual.
		// It's important to set the default to the session's current transform, not identity, in case there's
		// an initial global transformation set (e.g. if the renderer sets scaling for dpi or we're rendering from a VisualSurface)
		var initialTransform = drawingSession.TotalMatrix;
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
						  drawingSession,
						  ref initialTransform.IsIdentity ? ref Unsafe.NullRef<Matrix4x4>() : ref initialTransform,
						  opacity: 1.0f,
						  damage,
						  out var session);

		using (session)
		{
			// we set the matrix here similarly to CreateLocalMatrix in case the SetMatrix call there is
			// omitted.
			drawingSession.SetMatrix(initialTransform.IsIdentity ? TotalMatrix : TotalMatrix * initialTransform);
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
			// A descendant changed this frame; a drop shadow cast by this visual must re-damage its silhouette.
			_subtreeChangedThisFrame = true;
		}

		CreateLocalSession(in parentSession, out var session);

		using (session)
		{
			ApplyPrePaintingClipping(session.Session);

			if (ShadowState is null || TryRenderAnalyticShadow(session.Session, ShadowState))
			{
				PaintStep(this, session);
				PostPaintingClipStep(this, in session);
				RenderChildrenStep(this, session, applyChildOptimization);
			}
			else
			{
				// Non-analytic fallback: record the subtree once, then replay it twice — first through a
				// drop-shadow-filtered layer (which composites the shadow), then directly (the content on top).
				var recording = CreateRecording();
				// child.Render will reapply the total transform matrix, so we need to invert ours.
				Matrix4x4.Invert(TotalMatrix, out var rootTransform);
				_factory.CreateInstance(this, recording, ref rootTransform, session.Opacity, session.Damage, out var childSession);
				IRenderRecord renderData;
				using (childSession)
				{
					PaintStep(this, childSession);
					PostPaintingClipStep(this, in childSession);
					RenderChildrenStep(this, childSession, applyChildOptimization);
					renderData = recording.Finish();
				}

				session.Session.SaveLayer(ShadowState.ShadowFilter);
				renderData.Replay(session.Session);
				session.Session.Restore();

				renderData.Replay(session.Session);
				renderData.Dispose();
			}
		}

		static void PaintStep(Visual visual, in PaintingSession session)
		{
			// Rendering shouldn't depend on matrix or clip adjustments happening in a visual's Paint. That should
			// be specific to that visual and should not affect the rendering of any other visual.
#if DEBUG
			var saveCount = session.Session.SaveCount;
#endif
			if (visual.RequiresRepaintOnEveryFrame)
			{
				// Repaint-every-frame content (e.g. an effect brush over already-drawn area): paint directly, uncached.
				visual.ContributeDamageOnPaint(contentChanged: true, session.Damage);
				visual._ownContentPath?.Dispose();
				visual._ownContentPath = visual.Paint(session);
			}
			else
			{
				var contentChanged = (visual._flags & VisualFlags.PaintDirty) != 0;
				if (contentChanged)
				{
					visual._flags &= ~VisualFlags.PaintDirty;

					var recording = CreateRecording();
					_factory.CreateInstance(visual, recording, ref session.RootTransform, session.Opacity, session.Damage, out var recorderSession);
					// To debug what exactly gets repainted, replace the following line with `Paint(in session);`
					visual._ownContentPath?.Dispose();
					visual._ownContentPath = visual.Paint(in recorderSession);

					visual._content?.Dispose();
					visual._content = recording.Finish();
				}

				// Contribute damage whether or not the content was re-recorded: a moved-but-unchanged visual keeps its
				// cached content and own-content path, but its new position still needs to be repainted (and its old one).
				visual.ContributeDamageOnPaint(contentChanged, session.Damage);

				if (visual._content is { } content)
				{
					content.Replay(session.Session);
				}
			}
#if DEBUG
			Debug.Assert(saveCount == session.Session.SaveCount);
#endif
		}

		static void PostPaintingClipStep(Visual visual, in PaintingSession session)
			=> visual.ApplyPostPaintingClipping(session.Session);

		static void RenderChildrenStep(Visual visual, PaintingSession session, bool applyChildOptimization)
		{
			if (visual._childrenContent is { } childrenContent)
			{
				childrenContent.Replay(session.Session);
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
				var recording = CreateRecording();
				// child.Render will reapply the total transform matrix, so we need to invert ours.
				Matrix4x4.Invert(visual.TotalMatrix, out var rootTransform);
				_factory.CreateInstance(visual, recording, ref rootTransform, session.Opacity, session.Damage, out var childSession);
				using (childSession)
				{
					foreach (var child in visual.GetChildrenInRenderOrder())
					{
						child.Render(in childSession, applyChildOptimization: false);
					}
				}

				var content = recording.Finish();
				content.Replay(session.Session);

				// The visual can be set on a ChildrenSKPictureInvalid path after the render has started.
				// In such case, we should not cache this content. Not only it is outdated, it will also lead to a corrupted state,
				// where subtree rendering is skipped with the cached content,
				// and its descendant can't invalidate the cached content since they are already on a ChildrenSKPictureInvalid path.
				if ((visual._flags & VisualFlags.ChildrenSKPictureInvalid) == 0)
				{
					visual._childrenContent?.Dispose();
					visual._childrenContent = content;
				}
				else
				{
					content.Dispose();
				}
			}
		}
	}

	internal IGeometry GetNativeViewPathAndZOrder(IGeometry clipFromParent, IGeometry clipPath, List<Visual> nativeVisualsInZOrder)
	{
		if (this is { Opacity: 0 } or { IsVisible: false } || clipFromParent.IsEmpty)
		{
			return clipPath;
		}

		var localMatrix = TotalMatrix.ToMatrix3x2();
		var localClip = (GetPrePaintingClipping() ?? GeometryFactory.Current.CreateRectangleGeometry(new Rect(0, 0, Size.X, Size.Y)))
			.Transform(localMatrix)
			.Combine(clipFromParent, GeometryCombineMode.Intersect);

		if (IsNativeHostVisual || CanPaint())
		{
			clipPath = clipPath.Combine(localClip, IsNativeHostVisual ? GeometryCombineMode.Union : GeometryCombineMode.Difference);
		}

		if (IsNativeHostVisual && !localClip.IsEmpty)
		{
			nativeVisualsInZOrder.Add(this);
		}

		var childClip = localClip;
		if (GetPostPaintingClipping() is { } postClip)
		{
			childClip = childClip.Combine(postClip.Transform(localMatrix), GeometryCombineMode.Intersect);
		}

		foreach (var child in GetChildrenInRenderOrder())
		{
			clipPath = child.GetNativeViewPathAndZOrder(childClip, clipPath, nativeVisualsInZOrder);
		}

		return clipPath;
	}

	internal IGeometry GetTotalClipPath(bool skipPostPaintingClipping)
	{
		// Root: seed with the unclipped (infinite) region; ancestor clips are intersected into it.
		var dst = Parent is Visual parent
			? parent.GetTotalClipPath(false)
			: GeometryFactory.Current.CreateRectangleGeometry(InfiniteClipRect);

		var totalMatrix = TotalMatrix.ToMatrix3x2();
		if (GetPrePaintingClipping() is { } pre)
		{
			// The local clip is in local coordinates. We need to transform it to root coordinates.
			dst = dst.Combine(pre.Transform(totalMatrix), GeometryCombineMode.Intersect);
		}

		if (!skipPostPaintingClipping && GetPostPaintingClipping() is { } postClip)
		{
			dst = dst.Combine(postClip.Transform(totalMatrix), GeometryCombineMode.Intersect);
		}

		return dst;
	}

	/// <summary>
	/// Returns the bounds, in root visual coordinates, of the effective clip applied to this visual's
	/// own content by its ancestors (e.g. a ScrollViewer's viewport clip) and its own <see cref="Clip"/>.
	/// Intersecting an element's bounds with this rect yields what's actually visible, which automation
	/// uses to detect elements clipped entirely out of view (e.g. scrolled outside a ScrollViewer).
	/// </summary>
	internal Rect GetTotalClipRectInRootCoordinates()
		// skipPostPaintingClipping: true — a visual's own post-painting clip only affects its children,
		// not the visual itself. Ancestor post-painting clips are still applied via the parent recursion.
		=> GetTotalClipPath(skipPostPaintingClipping: true).Bounds;

	/// <summary>
	/// Draws the content of this visual.
	/// </summary>
	/// <param name="session">The drawing session to use.</param>
	internal virtual IGeometry? Paint(in PaintingSession session) => null;

	private protected virtual bool TryAddShadowPaths(List<(IGeometry path, float alpha)> output) => !CanPaint();

	private bool TryRenderAnalyticShadow(IDrawingSession session, ShadowState shadow)
	{
		var rootMatrix = TotalMatrix.ToMatrix3x2();
		if (!Matrix3x2.Invert(rootMatrix, out var inverseRoot))
		{
			return false;
		}

		var accumulator = new ShadowPathAccumulator();
		if (!WalkShadowSilhouette(this, this, inverseRoot, ancestorClipInRoot: null, 1f, accumulator))
		{
			return false;
		}

		var totalRegions = accumulator.Count;
		if (totalRegions == 0)
		{
			return true; // nothing to cast a shadow from; analytic path succeeded vacuously
		}

		var shadowColor = shadow.Color;

		session.Save();
		session.Translate(shadow.Dx, shadow.Dy);

		if (totalRegions > 1)
		{
			// Isolate accumulation so the additive blend sums region contributions without polluting the
			// canvas behind the shadow.
			session.SaveLayer();
			if (accumulator.OpaqueSilhouette is { } opaque)
			{
				DrawRegionShadow(session, opaque, 1f, shadowColor, shadow.SigmaX, shadow.SigmaY, additive: true);
			}
			foreach (var (path, alpha) in accumulator.Regions)
			{
				DrawRegionShadow(session, path, alpha, shadowColor, shadow.SigmaX, shadow.SigmaY, additive: true);
			}
			session.Restore();
		}
		else
		{
			// avoiding the SaveLayer was measured to be a significant perf win for the common case of a single region
			if (accumulator.OpaqueSilhouette is { } opaque)
			{
				DrawRegionShadow(session, opaque, 1f, shadowColor, shadow.SigmaX, shadow.SigmaY, additive: false);
			}
			else
			{
				var (path, alpha) = accumulator.Regions[0];
				DrawRegionShadow(session, path, alpha, shadowColor, shadow.SigmaX, shadow.SigmaY, additive: false);
			}
		}

		session.Restore();
		return true;

		static void DrawRegionShadow(IDrawingSession session, IGeometry path, float alpha, global::Windows.UI.Color shadowColor, float sigmaX, float sigmaY, bool additive)
		{
			var color = global::Windows.UI.Color.FromArgb((byte)(shadowColor.A * alpha), shadowColor.R, shadowColor.G, shadowColor.B);
			session.DrawShadow(path, color, sigmaX, sigmaY, additive, antialias: true);
		}
	}

	private static bool WalkShadowSilhouette(
		Visual visual,
		Visual shadowRoot,
		Matrix3x2 inverseRootMatrix,
		IGeometry? ancestorClipInRoot,
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

		var toRoot = visual.TotalMatrix.ToMatrix3x2() * inverseRootMatrix;

		var effectiveClip = visual.GetPrePaintingClipping()?.Transform(toRoot);

		// Intersect with the accumulated ancestor clip.
		if (ancestorClipInRoot is not null)
		{
			effectiveClip = effectiveClip is not null
				? effectiveClip.Combine(ancestorClipInRoot, GeometryCombineMode.Intersect)
				: ancestorClipInRoot;
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
			var sizeCandidate = GeometryFactory.Current.CreateRectangleGeometry(new Rect(0, 0, size.X, size.Y)).Transform(toRoot);
			if (effectiveClip is not null)
			{
				sizeCandidate = sizeCandidate.Combine(effectiveClip, GeometryCombineMode.Intersect);
			}
			canSkipOwnContribution = accumulator.IsFullyCovered(sizeCandidate);
		}
		else if (effectiveClip is not null)
		{
			canSkipOwnContribution = accumulator.IsFullyCovered(effectiveClip);
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
				var transformed = path.Transform(toRoot);

				if (effectiveClip is not null)
				{
					transformed = transformed.Combine(effectiveClip, GeometryCombineMode.Intersect);
					if (!transformed.IsEmpty)
					{
						accumulator.Add(transformed, alpha * combinedOpacity);
					}
				}
				else
				{
					accumulator.Add(transformed, alpha * combinedOpacity);
				}
			}
			scratch.Clear();
		}

		// Apply the post-painting clip to derive the clip for children.
		var childClipInRoot = effectiveClip;
		var postClipLocal = visual.GetPostPaintingClipping();
		if (postClipLocal is not null)
		{
			var postClipInRoot = postClipLocal.Transform(toRoot);
			childClipInRoot = childClipInRoot is not null
				? childClipInRoot.Combine(postClipInRoot, GeometryCombineMode.Intersect)
				: postClipInRoot;
		}

		foreach (var child in visual.GetChildrenInRenderOrder())
		{
			if (!WalkShadowSilhouette(child, shadowRoot, inverseRootMatrix, childClipInRoot, combinedOpacity, accumulator))
			{
				return false;
			}
		}

		return true;
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

	/// <summary>
	/// The pre-painting clip (this visual's <see cref="Clip"/>) in local coordinates, or <c>null</c> when
	/// this visual defines no such clip.
	/// </summary>
	// Note: The Clip is applied after the transformation matrix. A non-null Clip whose GetClipPath yields
	// no path still clips everything out (empty geometry) — matching the previous SKPath behaviour.
	internal virtual IGeometry? GetPrePaintingClipping()
		=> Clip is null
			? null
			: Clip.GetClipPath(this) ?? GeometryFactory.Current.CreateRectangleGeometry(new Rect(0, 0, 0, 0));

	/// <summary>Applies this visual's pre-painting clipping (its <see cref="Clip"/> and any layout/corner clip) to the drawing session.</summary>
	internal virtual void ApplyPrePaintingClipping(IDrawingSession session) => Clip?.ApplyClip(this, session);

	/// <summary>This clipping won't affect the visual itself, but its children.</summary>
	private protected virtual IGeometry? GetPostPaintingClipping() => null;
	/// <summary>Applies the post-painting (children) clipping to the drawing session. Overridable so simple
	/// rect/round-rect clips can use the session's ClipRect/ClipRoundRect fast paths instead of a full path.</summary>
	private protected virtual void ApplyPostPaintingClipping(IDrawingSession session)
	{
		if (GetPostPaintingClipping() is { } postClip)
		{
			session.ClipPath(postClip, antialias: true);
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
	private void CreateLocalSession(in PaintingSession parentSession, out PaintingSession session)
	{
		ref var rootTransform = ref parentSession.RootTransform;

		var opacity = Opacity == 1.0f ? parentSession.Opacity : parentSession.Opacity * Opacity;

		_factory.CreateInstance(this, parentSession.Session, ref rootTransform, opacity, parentSession.Damage, out session);

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
				session.Session.SetMatrix(totalMatrix);
			}
		}
#if DEBUG
		else
		{
			var actual = parentSession.Session.TotalMatrix;
			var expected = Unsafe.IsNullRef(ref rootTransform) ? TotalMatrix : TotalMatrix * rootTransform;
			var diff = actual - expected;
			// Due to the limited precision of doubles, instead of comparing the two matrices directly we compare the Frobenius norm of their difference to zero
			var frobeniusSquared =
				diff.M11 * diff.M11 + diff.M12 * diff.M12 + diff.M13 * diff.M13 + diff.M14 * diff.M14 +
				diff.M21 * diff.M21 + diff.M22 * diff.M22 + diff.M23 * diff.M23 + diff.M24 * diff.M24 +
				diff.M31 * diff.M31 + diff.M32 * diff.M32 + diff.M33 * diff.M33 + diff.M34 * diff.M34 +
				diff.M41 * diff.M41 + diff.M42 * diff.M42 + diff.M43 * diff.M43 + diff.M44 * diff.M44;
			Debug.Assert(Unsafe.IsNullRef(ref rootTransform)
				? actual == TotalMatrix
				: CompositionMathHelpers.IsCloseRealZero(frobeniusSquared, 1e-5f));
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
