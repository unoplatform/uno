namespace Uno.UI.Runtime.Skia {
	// Browser WebGPU device bring-up, SkiaSharp-Graphite-style: create the adapter+device in JavaScript
	// (navigator.gpu, awaited as a Task via [JSImport]) and graft the JS GPUDevice into emdawnwebgpu's C
	// handle table via Module.unoWebGpuImportDevice (installed by the emdawn __postset patch). This avoids the
	// in-WASM wgpuInstanceProcessEvents pump, which hangs when driven from a managed call stack on the browser.
	export class WebGpuInit {
		// Returns the imported WGPUDevice pointer (as an unsigned int), or 0 on failure. instancePtr is a real
		// wgpuCreateInstance handle created on the managed side; it becomes the imported device's EventSource parent.
		public static async createImportedDevice(instancePtr: number): Promise<number> {
			try {
				const gpu = (navigator as any).gpu;
				if (!gpu) {
					console.error("WebGpuInit: navigator.gpu is unavailable");
					return 0;
				}
				const adapter = await gpu.requestAdapter({ powerPreference: "high-performance" });
				if (!adapter) {
					console.error("WebGpuInit: navigator.gpu.requestAdapter returned null");
					return 0;
				}
				const device = await adapter.requestDevice();
				if (!device) {
					console.error("WebGpuInit: adapter.requestDevice returned null");
					return 0;
				}
				const module = (window as any).Module;
				if (!module || typeof module.unoWebGpuImportDevice !== "function") {
					console.error("WebGpuInit: Module.unoWebGpuImportDevice is missing (emdawn __postset patch not applied?)");
					return 0;
				}
				const devicePtr = module.unoWebGpuImportDevice(device, instancePtr) >>> 0;
				console.log("WebGpuInit: imported device ptr=" + devicePtr);
				return devicePtr;
			} catch (e) {
				console.error("WebGpuInit: device creation failed: " + e);
				return 0;
			}
		}
	}
}
