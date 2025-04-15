#nullable enable
//#define TRACE_COMPOSITION

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
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

	// Since painting (and recording) is done on the UI thread, we need a single SKPictureRecorder per UI thread.
	// If we move to a UI-thread-per-window model, then we need multiple recorders.
	[ThreadStatic]
	private static SKPictureRecorder? _recorder;

	private CompositionClip? _clip;
	private Vector2 _anchorPoint = Vector2.Zero; // Backing for scroll offsets
	private int _zIndex;
	private Matrix4x4 _totalMatrix = Matrix4x4.Identity;
	private SKPicture? _picture;

	private VisualFlags _flags = VisualFlags.MatrixDirty | VisualFlags.PaintDirty;

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

				// Start out with the final matrix of the parent
				var matrix = Parent?.TotalMatrix ?? Matrix4x4.Identity;

				// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
				var totalOffset = GetTotalOffset();
				var offsetMatrix = new Matrix4x4(
					1, 0, 0, 0,
					0, 1, 0, 0,
					0, 0, 1, 0,
					totalOffset.X + AnchorPoint.X, totalOffset.Y + AnchorPoint.Y, 0, 1);
				matrix = offsetMatrix * matrix;

				// Apply the rending transformation matrix (i.e. change coordinates system to the "rendering" one)
				if (GetTransform() is { IsIdentity: false } transform)
				{
					matrix = transform * matrix;
				}

				_totalMatrix = matrix;

			}

			return _totalMatrix;

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
		_picture?.Dispose();
		_picture = null;
		_flags |= VisualFlags.PaintDirty;
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

		canvas.Save();

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
			Render(session);
		}

		canvas.Restore();
	}

	/// <summary>
	/// Position a sub visual on the canvas and draw its content.
	/// </summary>
	/// <param name="parentSession">The drawing session of the <see cref="Parent"/> visual.</param>
	private void Render(in PaintingSession parentSession)
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
				foreach (var child in GetChildrenInRenderOrder())
				{
					child.Render(in session);
				}
			}
			else
			{
				var recorder = new SKPictureRecorder();
				var recordingCanvas = recorder.BeginRecording(new SKRect(-999999, -999999, 999999, 999999));
				// child.Render will reapply the total transform matrix, so we need to invert ours.
				Matrix4x4.Invert(TotalMatrix, out var rootTransform);
				_factory.CreateInstance(this, recordingCanvas, ref rootTransform, session.Opacity, out var childSession);
				using (childSession)
				{
					PaintStep(this, childSession);
					PostPaintingClipStep(this, canvas);
					foreach (var child in GetChildrenInRenderOrder())
					{
						child.Render(in childSession);
					}
				}
				var childrenPicture = recorder.EndRecording();
				canvas.DrawPicture(childrenPicture, ShadowState.ShadowOnlyPaint);
				canvas.DrawPicture(childrenPicture);
			}
		}

		static void PaintStep(Visual visual, PaintingSession session)
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
					_recorder ??= new SKPictureRecorder();
					var recordingCanvas = _recorder.BeginRecording(new SKRect(-999999, -999999, 999999, 999999));
					_factory.CreateInstance(visual, recordingCanvas, ref session.RootTransform, session.Opacity, out var recorderSession);
					// To debug what exactly gets repainted, replace the following line with `Paint(in session);`
					visual.Paint(in recorderSession);
					visual._picture = _recorder.EndRecording();
				}

				session.Canvas.DrawPicture(visual._picture);
			}
#if DEBUG
			Debug.Assert(saveCount == session.Canvas.SaveCount);
#endif
		}

		static void PostPaintingClipStep(Visual visual, SKCanvas canvas)
		{
			if (visual.GetPostPaintingClipping() is { } postClip)
			{
				canvas.ClipPath(postClip, antialias: true);
			}
		}
	}

	internal void GetNativeViewPath(SKPath clipFromParent, SKPath outPath)
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
			outPath.Op(localClipCombinedByClipFromParent, IsNativeHostVisual ? SKPathOp.Union : SKPathOp.Difference, outPath);
		}

		if (GetPostPaintingClipping() is { } postClip)
		{
			postClip.Transform(TotalMatrix.ToSKMatrix(), postClip);
			localClipCombinedByClipFromParent.Op(postClip, SKPathOp.Intersect, localClipCombinedByClipFromParent);
		}
		foreach (var child in GetChildrenInRenderOrder())
		{
			child.GetNativeViewPath(localClipCombinedByClipFromParent, outPath);
		}
	}

	/// <summary>
	/// Draws the content of this visual.
	/// </summary>
	/// <param name="session">The drawing session to use.</param>
	internal virtual void Paint(in PaintingSession session) { }

	private Vector3 GetTotalOffset()
	{
		if (IsTranslationEnabled && Properties.TryGetVector3("Translation", out var translation) == CompositionGetValueStatus.Succeeded)
		{
			// WARNING: DO NOT change this to plain "return Offset + translation;"
			// as this results in very wrong values on Android when debugger is not attached.
			// https://github.com/dotnet/runtime/issues/114094
			return new Vector3(Offset.X + translation.X, Offset.Y + translation.Y, Offset.Z + translation.Z);
		}

		return Offset;
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

	/// <remarks>You should NOT mutate the list returned by this method.</remarks>
	// NOTE: Returning List<Visual> so that enumerating doesn't cause boxing.
	// This has the side effect of having to return an empty list here.
	// The caller then shouldn't mutate the list, otherwise, things will go wrong badly.
	// An alternative is to return null and check for null on the call sites.
	private protected virtual List<Visual> GetChildrenInRenderOrder() => s_emptyList;

	/// <remarks>You should NOT mutate the list returned by this method.</remarks>
	internal List<Visual> GetChildrenInRenderOrderTestingOnly() => GetChildrenInRenderOrder();

	/// <summary>
	/// Creates a new <see cref="PaintingSession"/> set up with the local coordinates and opacity.
	/// </summary>
	private unsafe void CreateLocalSession(in PaintingSession parentSession, out PaintingSession session)
	{
		var canvas = parentSession.Canvas;

		ref var rootTransform = ref parentSession.RootTransform;

		var opacity = Opacity == 1.0f ? parentSession.Opacity : parentSession.Opacity * Opacity;

		_factory.CreateInstance(this, canvas, ref rootTransform, opacity, out session);

		Matrix4x4 totalMatrix;

		if (Unsafe.IsNullRef(ref rootTransform))
		{
			totalMatrix = TotalMatrix;
		}
		else
		{
			totalMatrix = TotalMatrix * rootTransform;
		}

		// this avoids the matrix copying in canvas.SetMatrix()
		UnoSkiaApi.sk_canvas_set_matrix(canvas.Handle, (SKMatrix44*)&totalMatrix);
	}

	[Flags]
	internal enum VisualFlags : byte
	{
		IsNativeHostVisualSet = 1, // Is the IsNativeHostVisual bit valid?
		IsNativeHostVisual = 2,
		IsNativeHostVisualInherited = 4,
		MatrixDirty = 8,
		PaintDirty = 16,
	}
}
