#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The neutral WebGPU device face for the <see cref="GraphicsContextKind.WebGpu"/> kind — the opaque WebGPU
/// handles (instance / adapter / device / queue) plus the host's chosen colour format and MSAA sample count, all
/// as <c>nint</c>/<c>uint</c> so no GPU-library type crosses the seam. The seam names no specific WebGPU
/// implementation, so host and backend can agree on whichever one they use (e.g. wgpu-native or Dawn). Mirrors
/// <see cref="IVulkanDeviceContext"/> / <see cref="IMetalDeviceContext"/>: the host graphics context implements
/// it, and the matched WebGPU render backend — Uno's or a third party's — adopts it to build its engine. There is
/// no privileged internal path.
/// </summary>
public interface IWebGpuDeviceContext : IGraphicsContext
{
	nint Instance { get; }
	nint Adapter { get; }
	nint Device { get; }
	nint Queue { get; }

	/// <summary>The WebGPU colour-format enum value the offscreen/resolve targets and the backend's pipelines must
	/// use, chosen by the host at device creation. 0 means the backend's default.</summary>
	uint ColorFormat { get; }

	/// <summary>The MSAA sample count the host picked (baked into the backend's pipelines). 0 means the backend's default.</summary>
	uint SampleCount { get; }
}
