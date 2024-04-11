#nullable enable
//#define TRACE_COMPOSITION

using System.Diagnostics;
using System.Numerics;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Composition;
using Uno.UI.Composition.Composition;

namespace Microsoft.UI.Composition;

public partial class Visual : global::Microsoft.UI.Composition.CompositionObject
{
	private CompositionClip? _clip;
	private Vector2 _anchorPoint = Vector2.Zero; // Backing for scroll offsets
	private int _zIndex;
	private bool _matrixDirty = true;
	private Matrix4x4 _totalMatrix = Matrix4x4.Identity;

	/// <returns>true if wasn't dirty</returns>
	internal virtual bool SetMatrixDirty() => _matrixDirty = true;

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
	internal virtual void Render(in DrawingSession parentSession)
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
		Draw(in session);
	}

	/// <summary>
	/// Draws the content of this visual.
	/// </summary>
	/// <param name="session">The drawing session to use.</param>
	internal virtual void Draw(in DrawingSession session)
	{
	}

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

		// Apply the clipping defined on the element
		// (Only the Clip property, clipping applied by parent for layout constraints reason it's managed by the ShapeVisual through the ViewBox)
		// Note: The Clip is applied after the transformation matrix, so it's also transformed.
		Clip?.Apply(canvas, this);

		var session = new DrawingSession(surface, canvas, in filters, in initialTransform);

		DrawingSession.PushOpacity(ref session, Opacity);

		return session;
	}
}
