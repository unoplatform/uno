namespace Uno.UI.Runtime.Skia {
	export class BrowserRenderer {
		static activeInstances = {};

		managedHandle: number;
		canvas: any;
		renderLoop: boolean;
		currentRequest: number;
		requestRender: any;
		static anyGL: any;
		glCtx: any;

		constructor(managedHandle: number) {
			this.managedHandle = managedHandle;
			this.canvas = undefined;
			this.renderLoop = false;
			this.currentRequest = 0;
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

		public requestAnimationFrame(renderLoop?: boolean) {
			// optionally update the render loop
			if (renderLoop !== undefined && this.renderLoop !== renderLoop) {
				this.setEnableRenderLoopInternal(renderLoop);
			}

			// skip because we have a render loop
			if (this.currentRequest !== 0)
				return;

			// add the draw to the next frame
			this.currentRequest = window.requestAnimationFrame(() => {

				if (this.requestRender) {
					// make current for this canvas instance
					(<any>window).GL.makeContextCurrent(this.glCtx);

					this.requestRender();
				}

				this.currentRequest = 0;

				// we may want to draw the next frame
				if (this.renderLoop)
					this.requestAnimationFrame();
			});
		}

		private setCanvasSize(width: number, height: number) {
			//if (!this.canvas)
			//	return;

			//var scale = window.devicePixelRatio || 1;
			//var w = this.canvas.clientWidth * scale
			//var h = this.canvas.clientHeight * scale;

			if (this.canvas.width !== width)
				this.canvas.width = width;
			if (this.canvas.height !== height)
				this.canvas.height = height;
		}

		static setCanvasSize(instance: BrowserRenderer, width: number, height: number) {
			instance.setCanvasSize(width, height);
		}

		static setEnableRenderLoop(instance: BrowserRenderer, enable: boolean) {
			instance.setEnableRenderLoopInternal(enable);
		}

		private setEnableRenderLoopInternal(enable: boolean) {
			this.renderLoop = enable;

			// either start the new frame or cancel the existing one
			if (enable) {
				this.requestAnimationFrame();
			} else if (this.currentRequest !== 0) {
				window.cancelAnimationFrame(this.currentRequest);
				this.currentRequest = 0;
			}
		}

		public static createContextStatic(instance: BrowserRenderer, canvasOrCanvasId:any) {
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
