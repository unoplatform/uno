#nullable enable
//#define TRACE_COMPOSITION

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
	internal SKMatrix TotalMatrix
	{
		get
		{
			if (_matrixDirty)
			{
				_matrixDirty = false;

				var matrix = Parent?.TotalMatrix ?? SKMatrix.Identity;

				// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
				var totalOffset = this.GetTotalOffset();
				matrix.TransX += totalOffset.X + AnchorPoint.X;
				matrix.TransY += totalOffset.Y + AnchorPoint.Y;

				// Applied rending transformation matrix (i.e. change coordinates system to the "rendering" one)
				if (this.GetTransform() is { IsIdentity: false } transform)
				{
					matrix = matrix.PreConcat(transform.ToSKMatrix());
				}

				_totalMatrix = matrix;

			}

			return _totalMatrix;
		}
	}
	internal string? TotalMatrixString => string.Join(", ", TotalMatrix.Values);

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
	/// Render a root visual.
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
		if (ignoreLocation)
		{
			canvas.Save();
			var totalOffset = this.GetTotalOffset();
			canvas.Translate(-(totalOffset.X + AnchorPoint.X), -(totalOffset.Y + AnchorPoint.Y));
		}

		using var session = BeginDrawing(surface, canvas, DrawingFilters.Default);
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
		=> BeginDrawing(parentSession.Surface, parentSession.Canvas, parentSession.Filters);

	private DrawingSession BeginDrawing(SKSurface surface, SKCanvas canvas, in DrawingFilters filters)
	{
		if (ShadowState is { } shadow)
		{
			canvas.SaveLayer(shadow.Paint);
		}
		else
		{
			canvas.Save();
		}

		canvas.SetMatrix(TotalMatrix);

		// Apply the clipping defined on the element
		// (Only the Clip property, clipping applied by parent for layout constraints reason it's managed by the ShapeVisual through the ViewBox)
		// Note: The Clip is applied after the transformation matrix, so it's also transformed.
		Clip?.Apply(canvas, this);

		var session = new DrawingSession(surface, canvas, in filters);

		DrawingSession.PushOpacity(ref session, Opacity);

		return session;
	}
}
