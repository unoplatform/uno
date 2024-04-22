#nullable enable
//#define TRACE_COMPOSITION

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Composition;
using Uno.UI.Composition.Composition;

namespace Microsoft.UI.Composition;

public partial class Visual : global::Microsoft.UI.Composition.CompositionObject
{
	// Since painting (and recording) is done on the UI thread, we need a single SKPictureRecorder per UI thread.
	// If we move to a UI-thread-per-window model, then we need multiple recorders.
	[ThreadStatic] private static SKPictureRecorder? _recorder;

	private CompositionClip? _clip;
	private RectangleClip? _cornerRadiusClip;
	private Vector2 _anchorPoint = Vector2.Zero; // Backing for scroll offsets
	private int _zIndex;
	private bool _matrixDirty = true;
	private Matrix4x4 _totalMatrix = Matrix4x4.Identity;
	private bool _requiresRepaint = true;
	private SKPicture? _picture;

	public object? Owner { get; set; }

	/// <returns>true if wasn't dirty</returns>
	internal virtual bool SetMatrixDirty()
	{
		var matrixDirty = _matrixDirty;
		_matrixDirty = true;
		return !matrixDirty;
	}

	internal void InvalidatePaint()
	{
		_picture?.Dispose();
		_picture = null;
		_requiresRepaint = true;
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
			if (_matrixDirty)
			{
				_matrixDirty = false;

				// Start out with the final matrix of the parent
				var matrix = Parent?.TotalMatrix ?? Matrix4x4.Identity;

				// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
				var totalOffset = this.GetTotalOffset();
				var offsetMatrix = new Matrix4x4(
					1, 0, 0, 0,
					0, 1, 0, 0,
					0, 0, 1, 0,
					totalOffset.X + AnchorPoint.X, totalOffset.Y + AnchorPoint.Y, 0, 1);
				matrix = offsetMatrix * matrix;

				// Apply the rending transformation matrix (i.e. change coordinates system to the "rendering" one)
				if (this.GetTransform() is { IsIdentity: false } transform)
				{
					matrix = transform * matrix;
				}

				_totalMatrix = matrix;

			}

			return _totalMatrix;
		}
	}

#if DEBUG
	internal string TotalMatrixString => $"{(_matrixDirty ? "-dirty-" : "")}{_totalMatrix}";
#endif

	public CompositionClip? Clip
	{
		get => _clip;
		set => SetProperty(ref _clip, value);
	}

	/// <summary>
	/// DO NOT USE: This a a temporary property to properly support the corner radius. 
	/// It should be removed by https://github.com/unoplatform/uno/issues/16294.
	/// This clipping should be applied only on Children elements (i.e. in the context of a ContainerVisual)
	/// </summary>
	internal RectangleClip? CornerRadiusClip
	{
		get => _cornerRadiusClip;
		set => SetProperty(ref _cornerRadiusClip, value);
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

	/// <summary>
	/// Render a visual as if it's the root visual.
	/// </summary>
	/// <param name="surface">The surface on which this visual should be rendered.</param>
	/// <param name="ignoreLocation">A boolean that indicates if the location of the root visual should be ignored (so it will be rendered at 0,0).</param>
	internal void RenderRootVisual(SKSurface surface, bool ignoreLocation = false)
	{
		if (this is { Opacity: 0 } or { IsVisible: false })
		{
			return;
		}

		var canvas = surface.Canvas;

		// Since we're acting as if this visual is a root visual, we undo the parent's TotalMatrix
		// so that when concatenated with this visual's TotalMatrix, the result is only the transforms
		// from this visual.
		// It's important to set the default to canvas.TotalMatrix not SKMatrix.Identity in case there's
		// an initial global transformation set (e.g. if the renderer sets scaling for dpi)
		var initialTransform = canvas.TotalMatrix.ToMatrix4x4();
		if (Parent?.TotalMatrix is { } parentTotalMatrix)
		{
			Matrix4x4.Invert(parentTotalMatrix, out var invertedParentTotalMatrix);
			initialTransform = invertedParentTotalMatrix;
		}

		if (ignoreLocation)
		{
			canvas.Save();
			var totalOffset = this.GetTotalOffset();
			var translation = Matrix4x4.Identity with { M41 = -(totalOffset.X + AnchorPoint.X), M42 = -(totalOffset.Y + AnchorPoint.Y) };
			initialTransform = translation * initialTransform;
		}

		using var session = BeginDrawing(surface, canvas, DrawingFilters.Default, initialTransform);
		Render(in session);

		if (ignoreLocation)
		{
			canvas.Restore();
		}
	}

	/// <summary>
	/// Position a sub visual on the canvas and draw its content.
	/// </summary>
	/// <param name="parentSession">The drawing session of the <see cref="Parent"/> visual.</param>
	private void Render(in DrawingSession parentSession)
	{
#if TRACE_COMPOSITION
		var indent = int.TryParse(Comment?.Split(new char[] { '-' }, 2, StringSplitOptions.TrimEntries).FirstOrDefault(), out var depth)
			? new string(' ', depth * 2)
			: string.Empty;
		global::System.Diagnostics.Debug.WriteLine($"{indent}{Comment} (Opacity:{parentSession.Filters.Opacity:F2}x{Opacity:F2} | IsVisible:{IsVisible})");
#endif

		if (this is { Opacity: 0 } or { IsVisible: false })
		{
			return;
		}

		using var session = BeginDrawing(in parentSession);
		if (_requiresRepaint)
		{
			_requiresRepaint = false;
			_recorder ??= new SKPictureRecorder();
			var recorderSession = session with { Canvas = _recorder.BeginRecording(new SKRect(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)) };
			// To debug what exactly gets repainted, replace the following line with `Draw(in session);`
			Draw(in recorderSession);
			_picture = _recorder.EndRecording();
		}
		session.Canvas.DrawPicture(_picture);

		// The CornerRadiusClip doesn't affect the visual itself, only its children
		CornerRadiusClip?.Apply(parentSession.Canvas, this);

		foreach (var child in GetChildrenInRenderOrder())
		{
			child.Render(in parentSession);
		}
	}

	/// <summary>
	/// Draws the content of this visual.
	/// </summary>
	/// <param name="session">The drawing session to use.</param>
	internal virtual void Draw(in DrawingSession session)
	{
	}

	/// <remarks>The canvas' TotalMatrix is assumed to already be set up to the local coordinates of the visual.</remarks>
	private protected virtual void ApplyClipping(in SKCanvas canvas)
	{
		// Apply the clipping defined on the element
		// (Only the Clip property, clipping applied by parent for layout constraints reason it's managed by the ShapeVisual through the ViewBox)
		// Note: The Clip is applied after the transformation matrix, so it's also transformed.
		Clip?.Apply(canvas, this);
	}

	private protected virtual IEnumerable<Visual> GetChildrenInRenderOrder() => Array.Empty<Visual>();
	internal IEnumerable<Visual> GetChildrenInRenderOrderTestingOnly() => GetChildrenInRenderOrder();

	/// <summary>
	/// Identifies whether a Visual can paint things. For example, ContainerVisuals don't
	/// paint on their own (even though they might contain other Visuals that do).
	/// This is a temporary optimization used with <see cref="_requiresRepaint"/> to reduce unnecessary
	/// SkPicture allocations. In the future, we should accurately set <see cref="_requiresRepaint"/> to
	/// only be true when we really have something to paint (and that painting needs to be updated).
	/// </summary>
	internal virtual bool CanPaint => false;

	private DrawingSession BeginDrawing(in DrawingSession parentSession)
		=> BeginDrawing(parentSession.Surface, parentSession.Canvas, parentSession.Filters, parentSession.RootTransform);

	private DrawingSession BeginDrawing(SKSurface surface, SKCanvas canvas, in DrawingFilters filters, in Matrix4x4 initialTransform)
	{
		if (ShadowState is { } shadow)
		{
			canvas.SaveLayer(shadow.Paint);
		}
		else
		{
			canvas.Save();
		}

		if (initialTransform.IsIdentity)
		{
			canvas.SetMatrix(TotalMatrix.ToSKMatrix());
		}
		else
		{
			canvas.SetMatrix((TotalMatrix * initialTransform).ToSKMatrix());
		}

		ApplyClipping(canvas);

		var session = new DrawingSession(surface, canvas, in filters, in initialTransform);

		DrawingSession.PushOpacity(ref session, Opacity);

		return session;
	}
}
