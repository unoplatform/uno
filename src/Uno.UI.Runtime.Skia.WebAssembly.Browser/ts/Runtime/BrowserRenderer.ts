namespace Uno.UI.Runtime.Skia {
	export class BrowserRenderer {
		managedHandle: number;
		canvas: HTMLCanvasElement;
		requestRender: () => void;
		static anyGL: any;
		glCtx: any;
		ctx2D: CanvasRenderingContext2D;
		pixelBuffer: number;
		clampedArray: Uint8ClampedArray;

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
			var rect = document.documentElement.getBoundingClientRect();
			var width = rect.width;
			var height = rect.height;
			var w = width * scale
			var h = height * scale;

			if (this.canvas.width !== w)
				this.canvas.width = w;
			if (this.canvas.height !== h)
				this.canvas.height = h;

			// We request to repaint on the next frame. Without this, the first frame after resizing the window will be
			// blank and will cause a flickering effect when you drag the window's border to resize.
			// See also https://github.com/unoplatform/uno-private/issues/902.
			BrowserRenderer.invalidate(this);
		}

		static invalidate(instance: BrowserRenderer) {
			window.requestAnimationFrame(() => {
				(<any>window).GL.makeContextCurrent(instance.glCtx);
				instance.requestRender();
			});
		}

		public static createContextStatic(instance: BrowserRenderer, canvasOrCanvasId: any) {
			return instance.createContext(canvasOrCanvasId);
		}

		private createContext(canvasOrCanvasId: string) {
			if (!canvasOrCanvasId)
				throw 'No <canvas> element or ID was provided';

			this.canvas = <HTMLCanvasElement>document.getElementById(canvasOrCanvasId);

			if (!this.canvas)
				throw `No <canvas> with id ${canvasOrCanvasId} was found`;

			try {
				this.glCtx = BrowserRenderer.createWebGLContext(this.canvas);
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
				this.setCanvasSize();
				window.addEventListener("resize", x => this.setCanvasSize());
				return {
					success: true,
					fbo: currentGLctx.getParameter(currentGLctx.FRAMEBUFFER_BINDING),
					stencil: currentGLctx.getParameter(currentGLctx.STENCIL_BITS),
					sample: 0, // TODO: currentGLctx.getParameter(GLctx.SAMPLES)
					depth: currentGLctx.getParameter(currentGLctx.DEPTH_BITS),
				};
			} catch (e) {
				this.ctx2D = this.canvas.getContext("2d");
				return {
					success: false,
					error: e.toString()
				}
			}
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

		public static resizePixelBuffer(instance: BrowserRenderer, width: number, height: number): number {
			if (instance.pixelBuffer !== 0) {
				Module._free(instance.pixelBuffer);
			}
			instance.canvas.width = width;
			instance.canvas.height = height;
			instance.pixelBuffer = Module._malloc(width * height * 4);
			instance.clampedArray = new Uint8ClampedArray(Module.HEAPU8.buffer, instance.pixelBuffer, width * height * 4);
			return instance.pixelBuffer;
		}

		public static blitSoftware(instance: BrowserRenderer, width: number, height: number) {
			const imageData = new ImageData(
				instance.clampedArray,
				width,
				height
			);
			instance.ctx2D.putImageData(imageData, 0, 0);
		}
	}
}
