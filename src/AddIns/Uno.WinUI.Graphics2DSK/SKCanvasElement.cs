using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Uno.WinUI.Graphics2DSK;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw directly using SkiaSharp.
/// </summary>
/// <remarks>
/// On a SkiaSharp backend the drawing goes zero-copy straight into the window's frame <see cref="SKCanvas"/>.
/// On any other backend (e.g. WebGPU) it falls back to a self-contained Skia-on-GL island (read back and
/// composited, at the cost of a copy). Skia-based targets only.
/// </remarks>
public abstract partial class SKCanvasElement : Grid
{
#if CROSSRUNTIME
	private SKCanvasVisual? _canvasVisual;
	// Typed as FrameworkElement (not SkiaGLCanvasElement) so holding the field never type-loads the optional
	// Graphics3DGL add-in. Concrete-island references are isolated in NoInlining methods guarded by a presence check.
	private FrameworkElement? _island;
	private bool _islandRequested;

	private protected override ContainerVisual CreateElementVisual()
		=> _canvasVisual = new SKCanvasVisual(this, Compositor.GetSharedCompositor());

	internal override bool IsViewHit() => true;
#endif

	protected SKCanvasElement()
	{
		if (!IsSupportedOnCurrentPlatform())
		{
			throw new PlatformNotSupportedException($"This platform does not support {nameof(SKCanvasElement)}. For more information: https://aka.platform.uno/skcanvaselement");
		}
	}

#if CROSSRUNTIME
	public static bool IsSupportedOnCurrentPlatform() => true;

	// Called from the paint when the active backend exposes no SKCanvas: bring up the GL island once, off the paint,
	// so it composites on the next frame. Only touched when the optional Graphics3DGL add-in is present.
	internal void EnsureIslandFallback()
	{
		if (_islandRequested)
		{
			return;
		}
		_islandRequested = true;

		if (!IsGLCanvasElementAvailable())
		{
			// Graphics3DGL isn't referenced — no GL fallback, and don't touch SkiaGLCanvasElement (keeps its base
			// assembly unloaded). Log so a blank element on a non-Skia backend is diagnosable.
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"{nameof(SKCanvasElement)} cannot draw on the active backend (it exposes no SKCanvas) and the Uno.WinUI.Graphics3DGL add-in — needed for the GL fallback — is not referenced; this element will not render. Reference Uno.WinUI.Graphics3DGL, or use a Skia backend.");
			}

			return;
		}

		DispatcherQueue.TryEnqueue(CreateIsland);
	}

	private static bool IsGLCanvasElementAvailable()
		=> Type.GetType("Uno.WinUI.Graphics3DGL.GLCanvasElement, Uno.WinUI.Graphics3DGL") is not null;

	// Isolated so the SkiaGLCanvasElement token is JIT-resolved only once Graphics3DGL is confirmed present.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void CreateIsland()
	{
		if (_island is null)
		{
			_island = new SkiaGLCanvasElement(this);
			Children.Add(_island);
			// The fallback was reached from a paint that already returned; re-invalidate so the visual repaints now
			// that the island is a child (otherwise the island — and thus the drawing — never composites).
			_canvasVisual?.Invalidate();
		}
	}
#else
	public static bool IsSupportedOnCurrentPlatform() => false;
#endif

	/// <summary>
	/// Invalidates the element and triggers a redraw.
	/// </summary>
#if CROSSRUNTIME
	public void Invalidate()
	{
		_canvasVisual?.Invalidate();
		if (_island is not null)
		{
			InvalidateIsland();
		}
	}

	// Isolated: the SkiaGLCanvasElement cast is JIT-resolved only when an island exists (i.e. Graphics3DGL is present).
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void InvalidateIsland() => ((SkiaGLCanvasElement)_island!).Invalidate();

	internal void InvokeRenderOverride(SKCanvas canvas, Size area) => RenderOverride(canvas, area);
#else
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
	public new void Invalidate() { }
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required
#endif

	/// <summary>
	/// The SkiaSharp drawing logic goes here.
	/// </summary>
	/// <param name="canvas">The SKCanvas that should be drawn on.</param>
	/// <param name="area">The dimensions of the clipping area.</param>
	/// <remarks>
	/// When called, the <paramref name="canvas"/> is already set up such that the origin (0,0) is at the top-left of the clipping area.
	/// Drawing outside this area (i.e. outside the (0, 0, area.Width, area.Height) rectangle) will be clipped out.
	/// </remarks>
	protected abstract void RenderOverride(SKCanvas canvas, Size area);
}
