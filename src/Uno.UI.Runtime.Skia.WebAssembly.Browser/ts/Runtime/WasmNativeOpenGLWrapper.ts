namespace Uno.UI.Runtime.Skia {
	export class WasmNativeOpenGLWrapper {

		public static tryCreateInstance() {
			try {
				const anyGL = (<any>window).GL;
				if (!anyGL || typeof anyGL.createContext !== "function") {
					throw "Emscripten GL module (window.GL) is unavailable";
				}

				// Detached canvas - GLCanvasElement reads pixels back from an FBO into a WriteableBitmap,
				// so the WebGL drawing buffer never needs to be visible.
				const canvas = document.createElement("canvas");
				canvas.width = 1;
				canvas.height = 1;

				const contextAttributes = {
					alpha: 1,
					depth: 1,
					stencil: 8,
					antialias: 0,
					premultipliedAlpha: 1,
					preserveDrawingBuffer: 1,
					preferLowPowerToHighPerformance: 0,
					failIfMajorPerformanceCaveat: 0,
					majorVersion: 2,
					minorVersion: 0,
					enableExtensionsByDefault: 1,
					explicitSwapControl: 0,
					renderViaOffscreenBackBuffer: 0,
				};

				let glCtx = anyGL.createContext(canvas, contextAttributes);
				if (!glCtx && contextAttributes.majorVersion > 1) {
					console.warn("WasmNativeOpenGLWrapper: falling back to WebGL 1.0");
					contextAttributes.majorVersion = 1;
					contextAttributes.minorVersion = 0;
					glCtx = anyGL.createContext(canvas, contextAttributes);
				}

				if (!glCtx || glCtx < 0) {
					throw `Failed to create offscreen WebGL context (err=${glCtx})`;
				}

				return { success: true, glCtxHandle: glCtx };
			} catch (e) {
				return { success: false, error: e.toString() };
			}
		}

		public static makeCurrent(glCtxHandle: number): number {
			const anyGL = (<any>window).GL;
			const prev = (anyGL.currentContext && anyGL.currentContext.handle) || 0;
			anyGL.makeContextCurrent(glCtxHandle);
			return prev;
		}

		public static restoreContext(prevHandle: number): void {
			const anyGL = (<any>window).GL;
			anyGL.makeContextCurrent(prevHandle);
		}

		public static destroy(glCtxHandle: number): void {
			const anyGL = (<any>window).GL;
			if (anyGL.currentContext && anyGL.currentContext.handle === glCtxHandle) {
				anyGL.makeContextCurrent(0);
			}
			anyGL.deleteContext(glCtxHandle);
		}
	}
}
