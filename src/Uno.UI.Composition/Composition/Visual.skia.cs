#nullable enable
//#define TRACE_COMPOSITION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.UI.Composition.Interactions;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Composition;
using Uno.UI.Composition.Composition;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class Visual : global::Microsoft.UI.Composition.CompositionObject
{
	private CompositionClip? _clip;
	private Vector2 _anchorPoint = Vector2.Zero; // Backing for scroll offsets
	private int _zIndex;

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
			Compositor.InvalidateRender();
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

		if (ignoreLocation)
		{
			surface.Canvas.Save();
			surface.Canvas.Translate(-(Offset.X + AnchorPoint.X), -(Offset.Y + AnchorPoint.Y));
		}

		using var session = BeginDrawing(surface, DrawingFilters.Default);
		Render(in session);

		if (ignoreLocation)
		{
			surface.Canvas.Restore();
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

	private protected DrawingSession BeginDrawing(in DrawingSession parentSession)
		=> BeginDrawing(parentSession.Surface, parentSession.Filters);

	private protected DrawingSession BeginDrawing(SKSurface surface, in DrawingFilters filters)
	{
		if (ShadowState is { } shadow)
		{
			surface.Canvas.SaveLayer(shadow.Paint);
		}
		else
		{
			surface.Canvas.Save();
		}

		// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
		surface.Canvas.Translate(Offset.X + AnchorPoint.X, Offset.Y + AnchorPoint.Y);

		// Applied rending transformation matrix (i.e. change coordinates system to the "rendering" one)
		if (this.GetTransform() is { IsIdentity: false } transform)
		{
			var skTransform = transform.ToSKMatrix();
			surface.Canvas.Concat(ref skTransform);
		}

		// Apply the clipping defined on the element
		// (Only the Clip property, clipping applied by parent for layout constraints reason it's managed by the ShapeVisual through the ViewBox)
		// Note: The Clip is applied after the transformation matrix, so it's also transformed.
		Clip?.Apply(surface);

		var session = new DrawingSession(surface, in filters);

		DrawingSession.PushOpacity(ref session, Opacity);

		return session;
	}
}
