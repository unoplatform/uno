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

		// Maps a readback buffer (identified by its wgpu handle ptr) off the event loop and inspects the first
		// byteLen bytes as RGBA8. Returns the count of non-transparent pixels (alpha != 0), or -1 on failure, and
		// logs luminance min/max. Used to verify the offscreen frame headless without needing the canvas to composite.
		public static async mapReadStats(bufferPtr: number, byteLen: number): Promise<number> {
			try {
				const module = (window as any).Module;
				const buffer = module && typeof module.unoWebGpuJsObject === "function"
					? module.unoWebGpuJsObject(bufferPtr) : null;
				if (!buffer) {
					console.error("WebGpuInit.mapReadStats: no JS buffer for ptr=" + bufferPtr);
					return -1;
				}
				await buffer.mapAsync(1 /* GPUMapMode.READ */);
				const data = new Uint8Array(buffer.getMappedRange());
				let opaque = 0, lumMin = 255, lumMax = 0;
				const n = Math.min(byteLen, data.length);
				for (let i = 0; i + 3 < n; i += 4) {
					if (data[i + 3] !== 0) { opaque++; }
					const lum = (data[i] + data[i + 1] + data[i + 2]) / 3 | 0;
					if (lum < lumMin) { lumMin = lum; }
					if (lum > lumMax) { lumMax = lum; }
				}
				buffer.unmap();
				console.log("WebGpuInit.mapReadStats: opaque=" + opaque + " lumMin=" + lumMin + " lumMax=" + lumMax);
				return opaque;
			} catch (e) {
				console.error("WebGpuInit.mapReadStats: failed: " + e);
				return -1;
			}
		}
	}
}
