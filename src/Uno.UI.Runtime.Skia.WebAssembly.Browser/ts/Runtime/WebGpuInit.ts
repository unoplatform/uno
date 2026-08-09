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

		// Maps a readback buffer (by its wgpu handle ptr) off the event loop and inspects it as RGBA8 (rows padded to
		// bytesPerRow). Returns the count of non-transparent pixels (alpha != 0), or -1 on failure, logs luminance
		// min/max, and stashes a PNG data-URL of the frame on window.__unoLastFramePng so a headless driver can save
		// an actual screenshot — the only way to observe WebGPU output where SwiftShader can't composite the canvas.
		public static async mapReadStats(bufferPtr: number, w: number, h: number, bytesPerRow: number): Promise<number> {
			try {
				const module = (window as any).Module;
				const buffer = module && typeof module.unoWebGpuJsObject === "function"
					? module.unoWebGpuJsObject(bufferPtr) : null;
				if (!buffer) {
					console.error("WebGpuInit.mapReadStats: no JS buffer for ptr=" + bufferPtr);
					return -1;
				}
				await buffer.mapAsync(1 /* GPUMapMode.READ */);
				const src = new Uint8Array(buffer.getMappedRange());
				const rgba = new Uint8ClampedArray(w * h * 4);
				let opaque = 0, lumMin = 255, lumMax = 0;
				for (let y = 0; y < h; y++) {
					const row = y * bytesPerRow;
					for (let x = 0; x < w; x++) {
						const s = row + x * 4, d = (y * w + x) * 4;
						const r = src[s], g = src[s + 1], b = src[s + 2], a = src[s + 3];
						rgba[d] = r; rgba[d + 1] = g; rgba[d + 2] = b; rgba[d + 3] = a;
						if (a !== 0) { opaque++; }
						const lum = (r + g + b) / 3 | 0;
						if (lum < lumMin) { lumMin = lum; }
						if (lum > lumMax) { lumMax = lum; }
					}
				}
				buffer.unmap();
				try {
					const canvas = document.createElement("canvas");
					canvas.width = w; canvas.height = h;
					canvas.getContext("2d")!.putImageData(new ImageData(rgba, w, h), 0, 0);
					(window as any).__unoLastFramePng = canvas.toDataURL("image/png");
				} catch (e) { /* PNG capture is best-effort */ }
				console.log("WebGpuInit.mapReadStats: opaque=" + opaque + " lumMin=" + lumMin + " lumMax=" + lumMax);
				return opaque;
			} catch (e) {
				console.error("WebGpuInit.mapReadStats: failed: " + e);
				return -1;
			}
		}

		// Maps a readback buffer (by wgpu handle ptr) off the event loop and returns its first byteLen bytes.
		// Used by SnapshotAsync (RenderTargetBitmap) — the only way to complete a GPU->CPU map on WASM's single
		// JS thread. (Marshalled as a number[]; adequate for occasional RTB, could be a heap copy if it gets hot.)
		public static async mapReadBytes(bufferPtr: number, byteLen: number): Promise<number[]> {
			const module = (window as any).Module;
			const buffer = module && typeof module.unoWebGpuJsObject === "function"
				? module.unoWebGpuJsObject(bufferPtr) : null;
			if (!buffer) {
				console.error("WebGpuInit.mapReadBytes: no JS buffer for ptr=" + bufferPtr);
				return [];
			}
			await buffer.mapAsync(1 /* GPUMapMode.READ */);
			const src = new Uint8Array(buffer.getMappedRange());
			const out = Array.from(src.subarray(0, byteLen));
			buffer.unmap();
			return out;
		}
	}
}
