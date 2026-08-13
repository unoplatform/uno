#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-backed <see cref="IRenderTarget"/> wrapping the host swapchain's <see cref="SKCanvas"/>. The
/// canvas is owned by the host, so <see cref="Dispose"/> is a no-op. (Once per-kind context providers own an
/// offscreen texture + dirty-rect blit, this becomes the color view they hand over instead of the raw canvas.)
/// </summary>
public sealed class SkiaRenderTarget(SKCanvas canvas) : IRenderTarget
{
	internal SKCanvas Canvas => canvas;

	public int Width => canvas.DeviceClipBounds.Width;

	public int Height => canvas.DeviceClipBounds.Height;

	public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Bgra8888;

	public void Dispose() { }
}
