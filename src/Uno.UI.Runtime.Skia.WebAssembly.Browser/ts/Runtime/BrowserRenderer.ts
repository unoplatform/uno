namespace Uno.UI.Runtime.Skia {
	export class BrowserRenderer {
		private readonly managedHandle: number;
		private readonly canvas: HTMLCanvasElement;
		private readonly requestRender: () => void;

		constructor(managedHandle: number, canvas: HTMLCanvasElement) {
			this.canvas = canvas;
			this.managedHandle = managedHandle;
			const skiaSharpExports = WebAssemblyWindowWrapper.getAssemblyExports();
			this.requestRender = () => skiaSharpExports.Uno.UI.Runtime.Skia.BrowserRenderer.RenderFrame(this.managedHandle);

			this.setCanvasSize();
			window.addEventListener("resize", x => this.setCanvasSize());
		}

		public static createInstance(managedHandle: number, canvasId: string) {
			if (!canvasId)
				throw 'No <canvas> element or ID was provided';

			const canvas = <HTMLCanvasElement>document.getElementById(canvasId);
			if (!canvas)
				throw `No <canvas> with id ${canvasId} was found`;

			return new BrowserRenderer(managedHandle, canvas);
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
				instance.requestRender();
			});
		}
	}
}
