namespace Uno.UI.Runtime {
	export class WebGlBrowserRenderer {
		private readonly canvas: HTMLCanvasElement;
		private readonly anyGL: any;
		private readonly glCtx: any;
		private readonly usesWorkerClock: boolean;

		constructor(canvas: HTMLCanvasElement) {
			this.canvas = canvas;
			this.anyGL = EmscriptenWebGL.assertGL();

			if (WorkerFrameClock.isRequested()) {
				try {
					const renderCanvas = WorkerFrameClock.createRenderCanvas(canvas);
					const offscreenCtx = EmscriptenWebGL.createContext(<any>renderCanvas, { antialias: 1 });
					if (offscreenCtx > 0 && WorkerFrameClock.attach(canvas, renderCanvas)) {
						this.glCtx = offscreenCtx;
						this.usesWorkerClock = true;
						return;
					}
				} catch (e) {
					console.warn(`WebGlBrowserRenderer: no offscreen WebGL context, rendering on the page clock (${e})`);
				}
			}

			this.glCtx = EmscriptenWebGL.createContext(this.canvas, { antialias: 1 });
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
					fbo: currentGLctx.getParameter(currentGLctx.FRAMEBUFFER_BINDING),
					stencil: currentGLctx.getParameter(currentGLctx.STENCIL_BITS),
					sample: 0, // TODO: currentGLctx.getParameter(GLctx.SAMPLES)
					depth: currentGLctx.getParameter(currentGLctx.DEPTH_BITS),
					usesWorkerClock: instance.usesWorkerClock === true,
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

		public static present(instance: WebGlBrowserRenderer) {
			if (instance.usesWorkerClock) {
				WorkerFrameClock.instance.present();
			}
		}
	}
}
