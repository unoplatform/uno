using System;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using SkiaSharp;

#if CROSSRUNTIME
using Uno.Foundation.Extensibility;
using Uno.UI.Graphics;
#endif

namespace Uno.WinUI.Graphics2DSK;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw directly on the window using SkiaSharp.
/// </summary>
/// <remarks>
/// On skia-based targets the drawing happens through the Uno composition pipeline.
/// On WinUI (WinAppSDK) the drawing happens through <see cref="SkiaSharp.Views.Windows.SKSwapChainPanel"/>.
/// </remarks>
public abstract partial class SKCanvasElement
#if WINAPPSDK
	: SkiaSharp.Views.Windows.SKSwapChainPanel
#else
	: FrameworkElement
#endif
{
#if CROSSRUNTIME
	private SKCanvasVisualBase? _skCanvasVisual;

	private protected override ContainerVisual CreateElementVisual()
	{
		if (ApiExtensibility.CreateInstance<SKCanvasVisualBaseFactory>(this, out var factory))
		{
			return _skCanvasVisual = factory.CreateInstance((o, size) => RenderOverride((SKCanvas)o, size), Compositor.GetSharedCompositor());
		}
		else
		{
			throw new InvalidOperationException($"Failed to create an instance of {nameof(SKCanvasVisualBase)}");
		}
	}

	internal override bool IsViewHit() => true;
#endif

	protected SKCanvasElement()
	{
		if (!IsSupportedOnCurrentPlatform())
		{
			throw new PlatformNotSupportedException($"This platform does not support {nameof(SKCanvasElement)}. For more information: https://aka.platform.uno/skcanvaselement");
		}

#if WINAPPSDK
		PaintSurface += OnPaintSurface;
#endif
	}

#if CROSSRUNTIME
	public static bool IsSupportedOnCurrentPlatform() => ApiExtensibility.IsRegistered<SKCanvasVisualBaseFactory>();
#elif WINAPPSDK
	public static bool IsSupportedOnCurrentPlatform() => true;
#else
	public static bool IsSupportedOnCurrentPlatform() => false;
#endif

#if CROSSRUNTIME
	/// <summary>
	/// Invalidates the element and triggers a redraw.
	/// </summary>
	public void Invalidate() => _skCanvasVisual?.Invalidate();
#elif !WINAPPSDK
	/// <summary>
	/// Invalidates the element and triggers a redraw.
	/// </summary>
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
	public new void Invalidate() { }
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required
#endif

#if WINAPPSDK
	// Mirrors the `_rendering` guard in Uno's Win32WindowWrapper.Rendering.cs:
	// if user code blocks inside RenderOverride (e.g. Task.Wait or Monitor.Enter),
	// a COM/WndProc message-pump turn could re-enter PaintSurface and corrupt
	// the canvas state.
	private bool _isRendering;

	private void OnPaintSurface(object? sender, SkiaSharp.Views.Windows.SKPaintGLSurfaceEventArgs e)
	{
		if (_isRendering)
		{
			return;
		}

		_isRendering = true;
		try
		{
			var canvas = e.Surface.Canvas;
			var area = new Size(ActualWidth, ActualHeight);

			// We save and restore the canvas state ourselves so that the inheritor doesn't accidentally forget to.
			canvas.Save();
			// clipping here guarantees that drawing doesn't get outside the intended area
			canvas.ClipRect(new SKRect(0, 0, (float)area.Width, (float)area.Height), antialias: true);
			try
			{
				RenderOverride(canvas, area);
			}
			finally
			{
				canvas.Restore();
			}
		}
		finally
		{
			_isRendering = false;
		}
	}
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
