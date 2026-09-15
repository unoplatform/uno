namespace Uno.UI.Runtime.Skia {
	export class RenderWorker {
		/**
		 * Transfers the canvas to a target worker thread as an OffscreenCanvas.
		 * Called from C# via JSImport on the main browser thread.
		 *
		 * @param canvasId The DOM canvas element ID.
		 * @param targetPthreadId The native pthread ID of the target worker thread.
		 */
		public static transferAndSetupGL(canvasId: string, targetPthreadId: number): void {
			const canvas = <HTMLCanvasElement>document.getElementById(canvasId);

			if (!canvas) {
				throw new Error(`No <canvas> with id '${canvasId}' was found`);
			}

			// Set the canvas drawing buffer size BEFORE transferring.
			// After transferControlToOffscreen(), the HTMLCanvasElement can no longer be resized.
			// The OffscreenCanvas inherits these dimensions at transfer time.
			var scale = window.devicePixelRatio || 1;

			var rect = document.documentElement.getBoundingClientRect();

			canvas.width = rect.width * scale;
			canvas.height = rect.height * scale;

			const offscreen = canvas.transferControlToOffscreen();

			const pthreads = (<any>Module).PThread?.pthreads;
			const pthreadEntry = pthreads[targetPthreadId];

			if (!pthreadEntry) {
				throw new Error(`No pthread found with ID ${targetPthreadId}`);
			}

			const worker: Worker = pthreadEntry.worker || pthreadEntry;

			// Transfer the OffscreenCanvas to the target worker
			worker.postMessage({ type: 'uno-setup-gl', canvas: offscreen }, [offscreen]);
		}
	}
}
