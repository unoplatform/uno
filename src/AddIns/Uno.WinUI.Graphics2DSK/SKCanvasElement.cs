using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Uno.UI.Composition.Drawing;
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
	// The last resort when the backend exposes no SKCanvas and the GL island is unavailable or failed: draw into a
	// raster SKSurface and hand its pixels to the active backend as a texture. Slower than either, never blank.
	private bool _software;
	private SKSurface? _softSurface;
	private int _softW, _softH;

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
			// Graphics3DGL isn't referenced — no GL island, and don't touch SkiaGLCanvasElement (keeps its base
			// assembly unloaded). Draw through the software surface instead.
			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().LogInformation($"{nameof(SKCanvasElement)}: the active backend exposes no SKCanvas and Uno.WinUI.Graphics3DGL is not referenced; drawing through a software surface.");
			}

			_software = true;
			return;
		}

		DispatcherQueue.TryEnqueue(CreateIsland);
	}

	/// <summary>The GL island could not get a usable context: drop it and draw through the software surface.</summary>
	internal void OnIslandUnavailable()
	{
		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().LogInformation($"{nameof(SKCanvasElement)}: the GL island is unavailable; drawing through a software surface.");
		}

		_software = true;
		// Reached from the island's own Loaded handler, so detach it once that has run.
		DispatcherQueue.TryEnqueue(() =>
		{
			if (_island is not null)
			{
				Children.Remove(_island);
				_island = null;
			}
			_canvasVisual?.Invalidate();
		});
	}

	internal bool UseSoftwareSurface => _software;

	// Renders through a raster SKSurface at device resolution and draws the pixels as a texture of the active
	// backend. The surface persists across frames and is recreated only when the device size changes.
	internal void PaintSoftware(IDrawingSession session, Vector2 size)
	{
		var m = session.TotalMatrix;
		var sx = MathF.Max(1e-3f, new Vector2(m.M11, m.M12).Length());
		var sy = MathF.Max(1e-3f, new Vector2(m.M21, m.M22).Length());
		var w = Math.Max(1, (int)MathF.Ceiling(size.X * sx));
		var h = Math.Max(1, (int)MathF.Ceiling(size.Y * sy));
		if (_softSurface is null || _softW != w || _softH != h)
		{
			_softSurface?.Dispose();
			_softSurface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul));
			_softW = w;
			_softH = h;
		}

		var canvas = _softSurface.Canvas;
		canvas.Clear(SKColors.Transparent);
		canvas.Save();
		canvas.Scale(sx, sy);
		canvas.ClipRect(new SKRect(0, 0, size.X, size.Y));
		RenderOverride(canvas, new Size(size.X, size.Y));
		canvas.Restore();
		canvas.Flush();

		using var pixmap = _softSurface.PeekPixels();
		// The recording takes its own reference to the texture on DrawImage; this one is the creator's.
		using var texture = session.Factory.CreateTexture(w, h, pixmap.GetPixelSpan());
		session.Save();
		session.Scale(1f / sx, 1f / sy);
		session.DrawImage(texture, 0f, 0f);
		session.Restore();
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
