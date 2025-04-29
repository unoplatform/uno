namespace Uno.UI.Runtime.Skia {
	export class BrowserRenderer {
		managedHandle: number;
		canvas: any;
		requestRender: any;
		static anyGL: any;
		glCtx: any;
		continousRender: boolean;
		queued: boolean;

		constructor(managedHandle: number) {
			this.managedHandle = managedHandle;
			this.canvas = undefined;
			this.requestRender = undefined;
			BrowserRenderer.anyGL = (<any>window).GL;
			this.buildImports();
		}

		async buildImports() {
			let anyModule = <any>window.Module;

			if (anyModule.getAssemblyExports !== undefined) {
				const skiaSharpExports = await anyModule.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser");

				this.requestRender = () => skiaSharpExports.Uno.UI.Runtime.Skia.BrowserRenderer.RenderFrame(this.managedHandle);
			}
		}

		public static createInstance(managedHandle: number) {
			return new BrowserRenderer(managedHandle);
		}

		private setCanvasSize() {
			var scale = window.devicePixelRatio || 1;
			var width = document.documentElement.clientWidth;
			var height = document.documentElement.clientHeight
			var w = width * scale
			var h = height * scale;

			if (this.canvas.width !== w)
				this.canvas.width = w;
			if (this.canvas.height !== h)
				this.canvas.height = h;

			this.canvas.style.width = `${width}px`;
			this.canvas.style.height = `${height}px`;

			// We request to repaint on the next frame. Without this, the first frame after resizing the window will be
			// blank and will cause a flickering effect when you drag the window's border to resize.
			// See also https://github.com/unoplatform/uno-private/issues/902.
			BrowserRenderer.invalidate(this);
		}

		static setContinousRender(instance: BrowserRenderer, enabled: boolean) {
			instance.continousRender = enabled;

			if (enabled) {
				BrowserRenderer.invalidate(instance);
			}
		}

		static invalidate(instance: BrowserRenderer) {

			const render = () => {
				// Allow for another queuing to happen in callees of `requestRender`
				instance.queued = false;

				if (instance.requestRender) {
					// make current for this canvas instance
					(<any>window).GL.makeContextCurrent(instance.glCtx);

					instance.requestRender();

					if (
						// If we're in continuous render mode, we need to requeue
						instance.continousRender

						// unless there's already another queueued render
						&& !instance.queued) {
						window.requestAnimationFrame(() => {
							instance.queued = true;
							render();
						});
					}
				}
			};

			if (!instance.queued) {
				instance.queued = true;

				// add the draw to the next frame
				window.requestAnimationFrame(() => render());
			}
		}

		public static createContextStatic(instance: BrowserRenderer, canvasOrCanvasId: any) {
			return instance.createContext(canvasOrCanvasId);
		}

		private createContext(canvasOrCanvasId: string) {
			if (!canvasOrCanvasId)
				throw 'No <canvas> element or ID was provided';

			var canvas = document.getElementById(canvasOrCanvasId);

			if (!canvas)
				throw `No <canvas> with id ${canvasOrCanvasId} was found`;

			this.glCtx = BrowserRenderer.createWebGLContext(canvas);
			if (!this.glCtx || this.glCtx < 0)
				throw `Failed to create WebGL context: err ${this.glCtx}`;

			// make current
			BrowserRenderer.anyGL.makeContextCurrent(this.glCtx);

			// Starting from .NET 7 the GLctx is defined in an inaccessible scope
			// when the current GL context changes. We need to pick it up from the
			// GL.currentContext instead.
			let currentGLctx = BrowserRenderer.anyGL.currentContext && BrowserRenderer.anyGL.currentContext.GLctx;

			if (!currentGLctx)
				throw `Failed to get current WebGL context`;

			// read values
			this.canvas = canvas;
			this.setCanvasSize();			
			window.addEventListener("resize", x => this.setCanvasSize());
			return {
				ctx: this.glCtx,
				fbo: currentGLctx.getParameter(currentGLctx.FRAMEBUFFER_BINDING),
				stencil: currentGLctx.getParameter(currentGLctx.STENCIL_BITS),
				sample: 0, // TODO: currentGLctx.getParameter(GLctx.SAMPLES)
				depth: currentGLctx.getParameter(currentGLctx.DEPTH_BITS),
			};

		}

		static createWebGLContext(canvas: any) {
			var contextAttributes = {
				alpha: 1,
				depth: 1,
				stencil: 8,
				antialias: 1,
				premultipliedAlpha: 1,
				preserveDrawingBuffer: 0,
				preferLowPowerToHighPerformance: 0,
				failIfMajorPerformanceCaveat: 0,
				majorVersion: 2,
				minorVersion: 0,
				enableExtensionsByDefault: 1,
				explicitSwapControl: 0,
				renderViaOffscreenBackBuffer: 0,
			};

			var ctx = BrowserRenderer.anyGL.createContext(canvas, contextAttributes);
			if (!ctx && contextAttributes.majorVersion > 1) {
				console.warn('Falling back to WebGL 1.0');
				contextAttributes.majorVersion = 1;
				contextAttributes.minorVersion = 0;
				ctx = BrowserRenderer.anyGL.createContext(canvas, contextAttributes);
			}

			return ctx;
		}
	}
}
