#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A WebGPU color target for the <see cref="GraphicsContextKind.WebGpu"/> kind — the neutral seam a WebGPU render
/// backend renders the frame into, exactly as <see cref="IGLRenderTarget"/>/<see cref="IVulkanRenderTarget"/>/
/// <see cref="ISoftwareRenderTarget"/> serve Skia. It carries only the <b>resolve</b> color attachment (a
/// single-sample color the host owns, presents from, and can read back); the backend allocates its own MSAA
/// color + depth/stencil to match and resolves into <see cref="ColorView"/> — the same "backend brings its own
/// depth/stencil" contract the other render targets follow. The handle is an opaque WebGPU object passed as
/// <c>nint</c> — this seam names no specific WebGPU implementation, so it works with whichever one the host and
/// backend agree on (e.g. wgpu-native or Dawn); a third-party WebGPU backend consumes it identically to Uno's own.
/// </summary>
public interface IWebGpuRenderTarget : IRenderTarget
{
	/// <summary>The single-sample resolve destination the backend resolves the frame into — an opaque WebGPU
	/// texture-view handle (a render-pass resolve target is a view; the host owns the underlying texture and
	/// presents/reads back from it, so the texture itself never crosses the seam).</summary>
	nint ColorView { get; }
}
