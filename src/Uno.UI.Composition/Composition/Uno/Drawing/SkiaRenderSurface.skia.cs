#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IRenderSurface"/> wrapping the host swapchain's <see cref="SKCanvas"/>.</summary>
internal sealed class SkiaRenderSurface(SKCanvas canvas) : IRenderSurface
{
	public SKCanvas Canvas => canvas;
}
