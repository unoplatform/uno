namespace Uno.UI.Runtime.Skia {
	export class SoftwareBrowserRenderer {
		private readonly canvas: HTMLCanvasElement;
		private readonly ctx2D: CanvasRenderingContext2D;
		private pixelBuffer: number = -1;
		private clampedArray: Uint8ClampedArray;

		constructor(canvas: HTMLCanvasElement) {
			this.canvas = canvas;
			this.ctx2D = this.canvas.getContext("2d");
			if (!this.ctx2D) {
				throw 'Unable to acquire 2D rendering context for the provided <canvas> element';
			}
		}

		public static tryCreateInstance(canvasId: any) {
			try {
				if (!canvasId)
					throw 'No <canvas> element or ID was provided';

				const canvas = <HTMLCanvasElement>document.getElementById(canvasId);
				if (!canvas)
					throw `No <canvas> with id ${canvasId} was found`;

				const instance = new SoftwareBrowserRenderer(canvas);
				return {
					success: true,
					instance: instance,
				};
			} catch (e) {
				return {
					success: false,
					error: e.toString()
				};
			}
		}

		public static resizePixelBuffer(instance: SoftwareBrowserRenderer, width: number, height: number): number {
			if (instance.pixelBuffer !== -1) {
				Module._free(instance.pixelBuffer);
			}
			instance.pixelBuffer = Module._malloc(width * height * 4);
			instance.clampedArray = new Uint8ClampedArray(Module.HEAPU8.buffer, instance.pixelBuffer, width * height * 4);
			return instance.pixelBuffer;
		}

		public static isPixelBufferValid(instance: SoftwareBrowserRenderer): boolean {
			// The clampedArray might suddenly becomes zero-length because the runtime
			// decided to resize the heap.
			return instance.clampedArray?.length > 0;
		}

		public static blitSoftware(instance: SoftwareBrowserRenderer, width: number, height: number) {
			const imageData = new ImageData(
				instance.clampedArray,
				width,
				height
			);
			instance.ctx2D.putImageData(imageData, 0, 0);
		}
	}
}
