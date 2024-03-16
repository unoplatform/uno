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
	private SKMatrix _totalMatrix = SKMatrix.Identity;

	/// <returns>true if wasn't dirty</returns>
	protected internal virtual bool SetMatrixDirty() => _matrixDirty = true;

	/// <summary>
	/// This is the final transformation matrix from the origin for this Visual.
	/// </summary>
	[DebuggerDisplay("{TotalMatrixString}")]
	internal SKMatrix TotalMatrix
	{
		get
		{
			if (_matrixDirty)
			{
				_matrixDirty = false;

				// Start out with the final matrix of the parent
				var matrix = Parent?.TotalMatrix ?? SKMatrix.Identity;

				// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
				var totalOffset = this.GetTotalOffset();
				var offsetMatrix = SKMatrix.Identity with { TransX = totalOffset.X + AnchorPoint.X, TransY = totalOffset.Y + AnchorPoint.Y };
				matrix = matrix.PreConcat(offsetMatrix);

				// Apply the rending transformation matrix (i.e. change coordinates system to the "rendering" one)
				if (this.GetTransform() is { IsIdentity: false } transform)
				{
					matrix = matrix.PreConcat(transform.ToSKMatrix());
				}

				_totalMatrix = matrix;

			}

			return _totalMatrix;
		}
	}

#if DEBUG
	internal string TotalMatrixString => string.Join(", ", TotalMatrix.Values);
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
	/// <param name="ignoreLocation">A boolean which indicates if the location of the root visual should be ignored (so it will be rendered at 0,0).</param>
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
		SKMatrix initialTransform = Parent?.TotalMatrix.Invert() ?? SKMatrix.Identity;

		if (ignoreLocation)
		{
			canvas.Save();
			var totalOffset = this.GetTotalOffset();
			initialTransform = initialTransform.PreConcat(SKMatrix.Identity with { TransX = -(totalOffset.X + AnchorPoint.X), TransY = -(totalOffset.Y + AnchorPoint.Y) });
		}

		using var session = BeginDrawing(surface, canvas, DrawingFilters.Default, initialTransform);
		Render(in session, initialTransform);

		if (ignoreLocation)
		{
			canvas.Restore();
		}
	}

	/// <summary>
	/// Position a sub visual on the canvas and draw its content.
	/// </summary>
	/// <param name="parentSession">The drawing session of the <see cref="Parent"/> visual.</param>
	/// <param name="initialTransform">An auxiliary transform matrix that the <see cref="TotalMatrix"/> should be applied on top of.</param>
	internal virtual void Render(in DrawingSession parentSession, in SKMatrix initialTransform)
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

		using var session = BeginDrawing(in parentSession, initialTransform);
		Draw(in session, initialTransform);
	}

	/// <summary>
	/// Draws the content of this visual.
	/// </summary>
	/// <param name="session">The drawing session to use.</param>
	/// <param name="initialTransform">An auxiliary transform matrix that the <see cref="TotalMatrix"/> should be applied on top of.</param>
	internal virtual void Draw(in DrawingSession session, in SKMatrix initialTransform)
	{
	}

	private DrawingSession BeginDrawing(in DrawingSession parentSession, in SKMatrix initialTransform)
		=> BeginDrawing(parentSession.Surface, parentSession.Canvas, parentSession.Filters, initialTransform);

	private DrawingSession BeginDrawing(SKSurface surface, SKCanvas canvas, in DrawingFilters filters, in SKMatrix initialTransform)
	{
		if (ShadowState is { } shadow)
		{
			canvas.SaveLayer(shadow.Paint);
		}
		else
		{
			canvas.Save();
		}

		if (!initialTransform.IsIdentity)
		{
			canvas.SetMatrix(TotalMatrix.PostConcat(initialTransform));
		}
		else
		{
			canvas.SetMatrix(TotalMatrix);
		}

		// Apply the clipping defined on the element
		// (Only the Clip property, clipping applied by parent for layout constraints reason it's managed by the ShapeVisual through the ViewBox)
		// Note: The Clip is applied after the transformation matrix, so it's also transformed.
		Clip?.Apply(canvas, this);

		var session = new DrawingSession(surface, canvas, in filters);

		DrawingSession.PushOpacity(ref session, Opacity);

		return session;
	}
}
