namespace Uno.UI.Runtime {
	/**
	 * Chrome on Android holds main-thread frames (requestAnimationFrame and canvas commits) to 60 Hz on 120 Hz
	 * screens, lifting it only while a finger moves, so flings and animations run at 60. A dedicated worker that
	 * owns a canvas gets its own, unthrottled frame clock.
	 *
	 * So the page's canvas is handed to a worker as a 'bitmaprenderer', WebGL renders into a standalone
	 * OffscreenCanvas, each drawn frame is posted to the worker as an ImageBitmap and shown one per refresh,
	 * and the worker's refreshes drive the render loop instead of window.requestAnimationFrame.
	 *
	 * Same approach as https://github.com/aumb/flutter_shim. It relies on undocumented Chrome behavior: if
	 * workers get throttled too, this simply runs at 60 again.
	 */
	export class WorkerFrameClock {
		private static _instance: WorkerFrameClock | null = null;

		/** Debug counters, readable from the console (globalThis.Uno.UI.Runtime.WorkerFrameClock.status). */
		public static readonly status = { active: false, ticks: 0, frames: 0, skipped: 0, error: <string>null };

		private readonly worker: Worker;
		private readonly offscreen: OffscreenCanvas;
		private callback: (() => void) | null = null;
		private awake = true;
		private skippedLast = false;
		private period = 1000 / 60;
		private previousVsync = 0;
		private readonly intervals: number[] = [];

		private constructor(worker: Worker, offscreen: OffscreenCanvas) {
			this.worker = worker;
			this.offscreen = offscreen;
			this.worker.onmessage = e => this.onTick(e.data);
			this.worker.onerror = e => WorkerFrameClock.status.error = e.message || "worker failed";
		}

		public static get instance(): WorkerFrameClock | null {
			return WorkerFrameClock._instance;
		}

		public static isRequested(): boolean {
			const supported = typeof Worker === "function"
				&& typeof OffscreenCanvas === "function"
				&& "transferControlToOffscreen" in HTMLCanvasElement.prototype
				&& "transferToImageBitmap" in OffscreenCanvas.prototype;
			if (!supported) {
				return false;
			}

			// ?unoframeclock=worker|page forces it on or off, for A/B comparisons on the same device.
			const forced = new URLSearchParams(window.location.search).get("unoframeclock");
			if (forced) {
				return forced === "worker";
			}

			const ua = navigator.userAgent;
			return /Android/.test(ua) && /Chrome\//.test(ua);
		}

		/**
		 * Creates the offscreen render canvas. The page canvas is only handed to the worker by attach(), once a
		 * GL context exists on it, so a failed WebGL init still leaves the page canvas to the software renderer.
		 */
		public static createRenderCanvas(canvas: HTMLCanvasElement): OffscreenCanvas {
			return new OffscreenCanvas(Math.max(1, canvas.width), Math.max(1, canvas.height));
		}

		public static attach(canvas: HTMLCanvasElement, offscreen: OffscreenCanvas): boolean {
			try {
				const worker = new Worker(URL.createObjectURL(new Blob([`(${workerFrameClockMain})()`], { type: "text/javascript" })));
				const onscreen = canvas.transferControlToOffscreen();
				onscreen.width = offscreen.width;
				onscreen.height = offscreen.height;
				worker.postMessage({ type: "canvas", canvas: onscreen }, [onscreen]);

				WorkerFrameClock._instance = new WorkerFrameClock(worker, offscreen);
				WorkerFrameClock.status.active = true;
				return true;
			} catch (e) {
				WorkerFrameClock.status.error = String(e);
				console.warn(`WorkerFrameClock: unavailable, staying on the page frame clock (${e})`);
				return false;
			}
		}

		public setSize(width: number, height: number) {
			if (this.offscreen.width === width && this.offscreen.height === height) {
				return;
			}

			this.offscreen.width = width;
			this.offscreen.height = height;
			this.worker.postMessage({ type: "resize", width, height });
		}

		/** Runs the callback on the worker's next refresh (one pending callback, like a coalesced rAF). */
		public requestFrame(callback: () => void) {
			this.callback = callback;
			if (!this.awake) {
				this.awake = true;
				this.worker.postMessage({ type: "wake" });
			}
		}

		/** Hands the frame just drawn into the render canvas to the worker, to show on its next refresh. */
		public present() {
			const frame = this.offscreen.transferToImageBitmap();
			WorkerFrameClock.status.frames++;
			this.worker.postMessage({ type: "frame", frame }, [frame]);
		}

		private onTick(data: any) {
			if (data.error) {
				WorkerFrameClock.status.error = data.error;
				return;
			}

			WorkerFrameClock.status.ticks++;
			const vsync = data.vsync - performance.timeOrigin;
			this.measurePeriod(vsync);

			// A tick that waited behind main-thread work has a newer one right behind it: skip it so the backlog
			// drains instead of rendering twice in one refresh (never twice in a row).
			if (!this.skippedLast && performance.now() - (data.sent - performance.timeOrigin) > 1.5 * this.period) {
				this.skippedLast = true;
				WorkerFrameClock.status.skipped++;
				return;
			}
			this.skippedLast = false;

			const callback = this.callback;
			this.callback = null;
			if (callback) {
				try {
					callback();
				} catch (e) {
					console.error(e);
				}
			}

			if (!this.callback && this.awake) {
				this.awake = false;
				this.worker.postMessage({ type: "sleep" });
			}
		}

		private measurePeriod(vsync: number) {
			const interval = vsync - this.previousVsync;
			this.previousVsync = vsync;
			if (interval <= 3 || interval >= 40) {
				return;
			}

			this.intervals.push(interval);
			if (this.intervals.length > 32) {
				this.intervals.shift();
			}
			this.period = [...this.intervals].sort((a, b) => a - b)[this.intervals.length >> 1];
		}
	}

	// Runs in the worker, serialized with toString (a method's source isn't a valid expression, hence a function).
	// Owns the page canvas, shows queued frames one per refresh and reports each refresh to the page.
	function workerFrameClockMain() {
		const self = <any>globalThis;
		let context: ImageBitmapRenderingContext = null;
		let canvas: OffscreenCanvas = null;
		const queue: ImageBitmap[] = [];
		let running = false, wanted = true, idleTicks = 0;

		// WebKit-style engines without worker rAF still present, at a timer's pace.
		const raf: (cb: (time: number) => void) => void = typeof self.requestAnimationFrame === "function"
			? (cb) => self.requestAnimationFrame(cb)
			: (cb) => setTimeout(() => cb(performance.now()), 1000 / 60);

		const tick = (time: number) => {
			const frame = queue.shift();
			if (frame) {
				context.transferFromImageBitmap(frame);
			}

			self.postMessage({ vsync: performance.timeOrigin + time, sent: performance.timeOrigin + performance.now() });

			// Keep ticking a few refreshes past the last frame: a restart costs a refresh, and the page re-arms
			// asynchronously. An idle page costs nothing.
			idleTicks = frame ? 0 : idleTicks + 1;
			if (wanted || queue.length || idleTicks < 8) {
				raf(tick);
			} else {
				running = false;
			}
		};

		const start = () => {
			if (!running) {
				running = true;
				raf(tick);
			}
		};

		self.onmessage = ({ data }: MessageEvent) => {
			switch (data.type) {
				case "canvas":
					canvas = data.canvas;
					context = <ImageBitmapRenderingContext>canvas.getContext("bitmaprenderer");
					start();
					break;
				case "wake":
					wanted = true;
					start();
					break;
				case "sleep":
					wanted = false;
					break;
				case "frame":
					// Oldest first, one per refresh. At most two wait; beyond that, latency matters more.
					queue.push(data.frame);
					if (queue.length > 2) {
						queue.shift().close();
					}
					start();
					break;
				case "resize":
					// Queued frames were drawn at the old size.
					for (const stale of queue.splice(0)) {
						stale.close();
					}
					canvas.width = data.width;
					canvas.height = data.height;
					break;
			}
		};
	}
}
