#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A WebGPU color target — the neutral seam type for the <see cref="GraphicsContextKind.WebGpu"/> kind, so the
/// framework routes a WebGpu frame to the WebGpu backend without naming the backend's concrete surface class.
/// Mirrors <see cref="IGLRenderTarget"/> / <see cref="IMetalRenderTarget"/> / <see cref="ISoftwareRenderTarget"/>.
/// The WebGPU backend recognizes its own concrete implementation behind this interface (its swapchain surface
/// with its resolve/depth views); no foreign type crosses the seam.
/// </summary>
public interface IWebGpuRenderTarget : IRenderTarget
{
}
