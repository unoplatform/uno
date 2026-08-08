#nullable enable

using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Uno.UI.Runtime.Skia;

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
}
