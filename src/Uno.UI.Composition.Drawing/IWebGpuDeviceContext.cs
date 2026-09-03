#nullable enable

using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The neutral WebGPU device face for the <see cref="GraphicsContextKind.WebGpu"/> kind — the opaque WebGPU
/// handles (instance / adapter / device / queue) plus the host's chosen colour format and MSAA sample count, all
/// as <c>nint</c>/<c>uint</c> so no GPU-library type crosses the seam. The seam names no specific WebGPU
/// implementation, so host and backend can agree on whichever one they use (e.g. wgpu-native or Dawn).
/// </summary>
public interface IWebGpuDeviceContext : IGraphicsContext
{
	nint Instance { get; }
	nint Adapter { get; }
	nint Device { get; }
	nint Queue { get; }

	/// <summary>
	/// Browser only (null on every native target): the live JavaScript <c>GPUDevice</c> object. On WASM the device
	/// is created via <c>navigator.gpu</c> in JS, so the honest handle across the seam is the JS object — a backend
	/// converts it to whatever it needs (Uno's emdawn backend imports it to a wgpu pointer; a direct-JS backend uses
	/// it as-is), rather than the contract presupposing a native pointer / emdawn import.
	/// </summary>
	JSObject? JsDevice => null;

	/// <summary>The WebGPU colour-format enum value the backend's pipelines must use, chosen by the host. 0 means the backend's default.</summary>
	uint ColorFormat { get; }

	/// <summary>The MSAA sample count the host picked. 0 means the backend's default.</summary>
	uint SampleCount { get; }
}
