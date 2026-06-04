namespace Uno.UI.Runtime.Skia {
	export class WasmNativeOpenGLWrapper {

		public static tryCreateInstance() {
			try {
				// Detached canvas - GLCanvasElement reads pixels back from an FBO into a WriteableBitmap,
				// so the WebGL drawing buffer never needs to be visible.
				const canvas = document.createElement("canvas");
				canvas.width = 1;
				canvas.height = 1;

				const glCtx = EmscriptenWebGL.createContext(canvas, { preserveDrawingBuffer: 1 });
				if (!glCtx || glCtx < 0) {
					throw `Failed to create offscreen WebGL context (err=${glCtx})`;
				}

				return { success: true, glCtxHandle: glCtx };
			} catch (e) {
				return { success: false, error: e.toString() };
			}
		}

		public static makeCurrent(glCtxHandle: number): number {
			const anyGL = EmscriptenWebGL.assertGL();
			const prev = (anyGL.currentContext && anyGL.currentContext.handle) || 0;
			anyGL.makeContextCurrent(glCtxHandle);
			return prev;
		}

		public static restoreContext(prevHandle: number): void {
			EmscriptenWebGL.assertGL().makeContextCurrent(prevHandle);
		}

		public static destroy(glCtxHandle: number): void {
			const anyGL = EmscriptenWebGL.assertGL();
			if (anyGL.currentContext && anyGL.currentContext.handle === glCtxHandle) {
				anyGL.makeContextCurrent(0);
			}
			anyGL.deleteContext(glCtxHandle);
		}
	}
}
