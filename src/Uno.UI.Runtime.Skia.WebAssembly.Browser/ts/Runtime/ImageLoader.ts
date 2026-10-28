namespace Uno.UI.Runtime.Skia {
	export class ImageLoader {
		static workerScriptUrl = URL.createObjectURL(new Blob([`
const offscreenCanvas = new OffscreenCanvas(0, 0);
const context = offscreenCanvas.getContext("2d", { willReadFrequently: true });
const readPixels = async (array) => {
	const imageBitmap = await createImageBitmap(new Blob([array]), { premultiplyAlpha: "premultiply" });
	try {
		offscreenCanvas.width = imageBitmap.width;
		offscreenCanvas.height = imageBitmap.height;
		context.drawImage(imageBitmap, 0, 0);
		return context.getImageData(0, 0, imageBitmap.width, imageBitmap.height);
	} finally {
		// Workers live as long as the app: without this, each one keeps the last image's pixels in its
		// canvas (until the next decode resizes it) and in the bitmap (until the worker's own GC).
		imageBitmap.close();
		offscreenCanvas.width = 0;
		offscreenCanvas.height = 0;
	}
};
onmessage = async (e) => {
	const [array, seqNo] = e.data;
	try {
		const imageData = await readPixels(array);

		// Due to a bug in Skia on WASM, we need the pixels to be
		// alpha-premultiplied because using SKAlphaType.Unpremul is not working
		// correctly (see also https://github.com/unoplatform/uno/issues/20727),
		// so we multiply the RGB values by the alpha by hand instead.
		// This is somehow a LOT faster than doing it with webgl in a fragment shader
		const buffer = imageData.data;
		for (let i = 0; i < buffer.byteLength; i += 4) {
			const a = buffer[i + 3];
			buffer[i] = (buffer[i] * a) / 255;
			buffer[i + 1] = (buffer[i + 1] * a) / 255;
			buffer[i + 2] = (buffer[i + 2] * a) / 255;
		}
		postMessage({
			seqNo: seqNo,
			response: {
				error: null,
				bytes: new Uint8Array(imageData.data.buffer), // does not copy
				width: imageData.width,
				height: imageData.height
			}
		},
		[imageData.data.buffer]);
	} catch (e) {
		postMessage({seqNo: seqNo, response: { error: e.toString() }});
	}
};`], { type: 'application/javascript' }));
		static availableWorkers: Worker[] = Array.from({ length: navigator.hardwareConcurrency }).map((_, i) => {
			const worker = new Worker(ImageLoader.workerScriptUrl);
			const listener = (ev: MessageEvent) => {
				const promiseResolver = ImageLoader.pendingPromiseResolvers.get(ev.data.seqNo);
				ImageLoader.pendingPromiseResolvers.delete(ev.data.seqNo);
				promiseResolver(ev.data.response);
				if (ImageLoader.pendingJobs.length == 0) {
					ImageLoader.availableWorkers.push(worker);
				} else {
					const job = ImageLoader.pendingJobs.pop();
					(<any>window).setImmediate(() => job(worker));
				}
			}
			worker.addEventListener("message", listener);
			return worker;
		});
		static pendingJobs = new Array<(worker: Worker) => void>();
		static sequenceNumber = 0;
		static pendingPromiseResolvers = new Map<Number, (value: (PromiseLike<object> | object)) => void>();

		public static loadFromArray(array: Uint8Array): Promise<object> {
			const seqNo = ++ImageLoader.sequenceNumber;
			return new Promise<object>((resolve) => {
				ImageLoader.pendingPromiseResolvers.set(seqNo, resolve);
				if (ImageLoader.availableWorkers.length == 0) {
					ImageLoader.pendingJobs.push(worker => worker.postMessage([array, seqNo], [array.buffer]));
				} else {
					const worker = ImageLoader.availableWorkers.pop();
					worker.postMessage([array, seqNo], [array.buffer]);
				}
			});
		}
	}
}
