namespace Uno.UI.Runtime {
	export class WebGlBrowserRenderer {
		private readonly canvas: HTMLCanvasElement;
		private readonly anyGL: any;
		private readonly glCtx: any;
		private lastWidth = -1;
		private lastHeight = -1;

		constructor(canvas: HTMLCanvasElement) {
			this.canvas = canvas;
			this.anyGL = EmscriptenWebGL.assertGL();

			// The preserved drawing buffer keeps the previous frame, so only the damaged regions are repainted. No MSAA:
			// Skia's coverage AA does not use it, and resolving it costs a full-canvas pass every frame. No depth either:
			// Skia only needs the stencil.
			this.glCtx = EmscriptenWebGL.createContext(this.canvas, { antialias: 0, depth: 0, stencil: 8, preserveDrawingBuffer: 1 });
			if (!this.glCtx || this.glCtx < 0)
				throw `Failed to create WebGL context: err ${this.glCtx}`;
		}

		public static tryCreateInstance(canvasId: any) {
			try {
				if (!canvasId)
					throw 'No <canvas> element or ID was provided';

				const canvas = <HTMLCanvasElement>document.getElementById(canvasId);
				if (!canvas)
					throw `No <canvas> with id ${canvasId} was found`;

				const instance = new WebGlBrowserRenderer(canvas);

				WebGlBrowserRenderer.makeCurrent(instance);

				// Starting from .NET 7 the GLctx is defined in an inaccessible scope
				// when the current GL context changes. We need to pick it up from the
				// GL.currentContext instead.
				let currentGLctx = instance.anyGL.currentContext && instance.anyGL.currentContext.GLctx;
				if (!currentGLctx)
					throw `Failed to get current WebGL context`;

				return {
					success: true,
					instance: instance,
					stencil: currentGLctx.getParameter(currentGLctx.STENCIL_BITS),
					depth: currentGLctx.getParameter(currentGLctx.DEPTH_BITS),
				};
			} catch (e) {
				return {
					success: false,
					error: e.toString()
				};
			}
		}

		public static makeCurrent(instance: WebGlBrowserRenderer) {
			instance.anyGL.makeContextCurrent(instance.glCtx);
		}

		/** False when the drawing buffer lost the previous frame: resizing the canvas or losing the context clears it. */
		public static isContentPreserved(instance: WebGlBrowserRenderer): boolean {
			const canvas = instance.canvas;
			const unchanged = canvas.width === instance.lastWidth && canvas.height === instance.lastHeight;
			instance.lastWidth = canvas.width;
			instance.lastHeight = canvas.height;

			const gl = instance.anyGL.getContext(instance.glCtx)?.GLctx;
			return unchanged && !!gl && !gl.isContextLost();
		}
	}
}
