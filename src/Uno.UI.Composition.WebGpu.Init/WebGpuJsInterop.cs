#nullable enable

using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// Browser WebGPU device bring-up done in JavaScript (see ts/Runtime/WebGpuInit.ts). The adapter/device are
/// requested via navigator.gpu and awaited as a <see cref="Task"/>, then the JS GPUDevice is imported into
/// emdawnwebgpu's C handle table — avoiding the in-WASM wgpuInstanceProcessEvents pump, which hangs when driven
/// from a managed call stack on the browser.
/// </summary>
internal static partial class WebGpuJsInterop
{
	/// <summary>Creates a WebGPU device in JS and imports it into the given wgpu instance. Returns the imported
	/// WGPUDevice pointer, or 0 on failure.</summary>
	[JSImport("globalThis.Uno.UI.Runtime.Skia.WebGpuInit.createImportedDevice")]
	public static partial Task<int> CreateImportedDeviceAsync(int instancePtr);

	/// <summary>Maps a readback buffer (by its wgpu handle ptr) off the event loop and inspects it as RGBA8
	/// (rows padded to <paramref name="bytesPerRow"/>). Returns the non-transparent pixel count, or -1 on failure,
	/// and stashes a PNG of the frame on window.__unoLastFramePng.</summary>
	[JSImport("globalThis.Uno.UI.Runtime.Skia.WebGpuInit.mapReadStats")]
	public static partial Task<int> MapReadStatsAsync(int bufferPtr, int width, int height, int bytesPerRow);

	/// <summary>Maps a readback buffer (by its wgpu handle ptr) off the event loop and returns its first
	/// <paramref name="byteLen"/> bytes as base64 (marshals cleanly as a string). Backs
	/// WebGpuDrawingFactory.SnapshotAsync (RenderTargetBitmap) on WASM.</summary>
	[JSImport("globalThis.Uno.UI.Runtime.Skia.WebGpuInit.mapReadBase64")]
	public static partial Task<string> MapReadBase64Async(int bufferPtr, int byteLen);
}
