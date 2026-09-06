namespace Uno.UI.Runtime.Skia {
	// Shared helper for creating emscripten-managed WebGL contexts (window.GL) with a
	// WebGL 1.0 fallback. Used by both WebGlBrowserRenderer (the onscreen Skia render
	// surface) and WasmNativeOpenGLWrapper (GLCanvasElement's offscreen contexts).
	export class EmscriptenWebGL {
		public static assertGL(): any {
			const anyGL = (<any>window).GL;
			if (!anyGL || typeof anyGL.createContext !== "function") {
				throw "Emscripten GL module (window.GL) is unavailable";
			}
			return anyGL;
		}

		// attributeOverrides adjusts the defaults per call site (e.g. antialias for the
		// onscreen Skia surface, preserveDrawingBuffer for GLCanvasElement readbacks).
		public static createContext(canvas: HTMLCanvasElement, attributeOverrides: object = {}): number {
			const anyGL = EmscriptenWebGL.assertGL();
			const contextAttributes = {
				alpha: 1,
				depth: 1,
				stencil: 8,
				antialias: 0,
				premultipliedAlpha: 1,
				preserveDrawingBuffer: 0,
				preferLowPowerToHighPerformance: 0,
				failIfMajorPerformanceCaveat: 0,
				majorVersion: 2,
				minorVersion: 0,
				enableExtensionsByDefault: 1,
				explicitSwapControl: 0,
				renderViaOffscreenBackBuffer: 0,
				...attributeOverrides,
			};

			let ctx = anyGL.createContext(canvas, contextAttributes);
			if (!ctx && contextAttributes.majorVersion > 1) {
				console.warn("EmscriptenWebGL: falling back to WebGL 1.0");
				contextAttributes.majorVersion = 1;
				contextAttributes.minorVersion = 0;
				ctx = anyGL.createContext(canvas, contextAttributes);
			}
			return ctx;
		}
	}
}
