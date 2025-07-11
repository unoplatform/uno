using System;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using SkiaSharp;
using Uno.Foundation.Extensibility;
using Uno.UI.Graphics;

namespace Uno.WinUI.Graphics2DSK;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw directly on the window using SkiaSharp.
/// </summary>
/// <remarks>This is only available on skia-based targets.</remarks>
public abstract class SKCanvasElement : FrameworkElement
{
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

	protected SKCanvasElement()
	{
		if (!ApiExtensibility.IsRegistered<SKCanvasVisualBaseFactory>())
		{
			throw new PlatformNotSupportedException($"This platform does not support {nameof(SKCanvasElement)}");
		}
	}

	/// <summary>
	/// Invalidates the element and triggers a redraw.
	/// </summary>
	public void Invalidate() => _skCanvasVisual?.Invalidate();

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
