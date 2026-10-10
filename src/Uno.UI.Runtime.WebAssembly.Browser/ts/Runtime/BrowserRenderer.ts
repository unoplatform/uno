namespace Uno.UI.Runtime {
	export class BrowserRenderer {
		private readonly managedHandle: number;
		private readonly canvas: HTMLCanvasElement;
		private readonly requestRender: () => void;

		constructor(managedHandle: number, canvas: HTMLCanvasElement) {
			this.canvas = canvas;
			this.managedHandle = managedHandle;
			const skiaSharpExports = WebAssemblyWindowWrapper.getAssemblyExports();
			const renderFrame = () => skiaSharpExports.Uno.UI.Runtime.BrowserRenderer.RenderFrame(this.managedHandle);
			this.requestRender = new URLSearchParams(window.location.search).has("unofps")
				? BrowserRenderer.withFrameRateBadge(renderFrame)
				: renderFrame;

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

			// Once handed to the worker, the page canvas's size belongs to the worker's copy.
			const workerClock = WorkerFrameClock.instance;
			if (workerClock) {
				workerClock.setSize(w, h);
			} else {
				if (this.canvas.width !== w)
					this.canvas.width = w;
				if (this.canvas.height !== h)
					this.canvas.height = h;
			}

			// We request to repaint on the next frame. Without this, the first frame after resizing the window will be
			// blank and will cause a flickering effect when you drag the window's border to resize.
			// See also https://github.com/unoplatform/uno-private/issues/902.
			BrowserRenderer.invalidate(this);
		}

		// ?unofps shows the render rate while frames run back to back, to compare frame clocks on a device.
		private static withFrameRateBadge(renderFrame: () => void): () => void {
			const badge = document.createElement("div");
			badge.setAttribute("aria-hidden", "true");
			badge.style.cssText = "position:fixed;right:4px;bottom:4px;z-index:2147483647;pointer-events:none;"
				+ "font:12px monospace;padding:2px 6px;border-radius:4px;background:rgba(0,0,0,.7);color:#0f0";
			document.body.appendChild(badge);

			const clock = () => WorkerFrameClock.instance ? "worker" : "page";
			let windowStart = performance.now(), frames = 0, last = 0;
			badge.textContent = `${clock()} clock`;
			setInterval(() => {
				const now = performance.now();
				// An idle gap would read as a low rate: show the last busy window instead.
				if (frames > 10) {
					last = Math.round(frames * 1000 / (now - windowStart));
				}
				badge.textContent = `${clock()} clock: ${last} fps`;
				windowStart = now;
				frames = 0;
			}, 500);

			return () => {
				frames++;
				renderFrame();
			};
		}

		static invalidate(instance: BrowserRenderer) {
			const workerClock = WorkerFrameClock.instance;
			if (workerClock) {
				workerClock.requestFrame(instance.requestRender);
				return;
			}

			window.requestAnimationFrame(() => {
				instance.requestRender();
			});
		}
	}
}
