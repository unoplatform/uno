#nullable enable

using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A WebGPU color target for the <see cref="GraphicsContextKind.WebGpu"/> kind. It carries only the
/// <b>resolve</b> color attachment (a single-sample color the host owns, presents from, and can read back); the
/// backend allocates its own MSAA color + depth/stencil to match and resolves into <see cref="ColorView"/>. The
/// handle is an opaque WebGPU object passed as <c>nint</c>, so it works with whichever WebGPU implementation the
/// host and backend agree on (e.g. wgpu-native or Dawn).
/// </summary>
public interface IWebGpuRenderTarget : IRenderTarget
{
	/// <summary>The single-sample resolve destination the backend resolves the frame into — an opaque WebGPU
	/// texture-view handle (the host owns the underlying texture, so it never crosses the seam).</summary>
	nint ColorView { get; }

	/// <summary>
	/// Browser only (null on every native target): the resolve destination as a live JavaScript
	/// <c>GPUTextureView</c>. Symmetric with <see cref="IWebGpuDeviceContext.JsDevice"/> — on WASM the honest handle
	/// across the seam is the JS object; a backend converts it as it needs (Uno's emdawn backend imports it to a
	/// wgpu view; a direct-JS backend renders into it directly), rather than the contract handing a native pointer.
	/// </summary>
	JSObject? JsColorView => null;
}
