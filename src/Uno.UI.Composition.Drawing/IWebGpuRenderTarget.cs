#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A WebGPU color target for the <see cref="GraphicsContextKind.WebGpu"/> kind — the neutral seam a WebGPU render
/// backend renders the frame into, exactly as <see cref="IGLRenderTarget"/>/<see cref="IVulkanRenderTarget"/>/
/// <see cref="ISoftwareRenderTarget"/> serve Skia. It carries only the <b>resolve</b> color attachment (a
/// single-sample wgpu texture the host owns, presents from, and can read back); the backend allocates its own
/// MSAA color + depth/stencil to match and resolves into <see cref="ColorView"/> — the same "backend brings its
/// own depth/stencil" contract the other render targets follow. Raw wgpu handles cross the seam as
/// <c>nint</c> (a <c>WGPUTexture</c>/<c>WGPUTextureView</c>), so no host-owned concrete type does — a third-party
/// WebGPU backend consumes this identically to Uno's own.
/// </summary>
public interface IWebGpuRenderTarget : IRenderTarget
{
	/// <summary>The resolve color texture the frame ends up in (host-owned; presented/read back). <c>WGPUTexture</c>.</summary>
	nint ColorTexture { get; }

	/// <summary>A view of <see cref="ColorTexture"/> — the backend's single-sample resolve destination. <c>WGPUTextureView</c>.</summary>
	nint ColorView { get; }
}
