namespace Uno.UI.Runtime.Skia {
	export class ImageLoader {
		static canvas: HTMLCanvasElement = document.createElement('canvas');
		static ctx: CanvasRenderingContext2D = ImageLoader.canvas.getContext("2d");
		
		public static async loadFromArray(array: Uint8Array): Promise<object> {
			return new Promise<object>((resolve) => {
				const image = new Blob([array]);
				const imageUrl = URL.createObjectURL(image);

				const img = new Image();
				img.onload = () => {
					URL.revokeObjectURL(imageUrl);

					const bytes = ImageLoader.imageToBytes(img);

					resolve({
						error: null,
						bytes: bytes,
						width: img.width,
						height: img.height
					});
				};

				img.onerror = e => {
					URL.revokeObjectURL(imageUrl);
					resolve({error: e.toString()});
				};

				img.src = imageUrl;
			});
		}

		private static imageToBytes(img: HTMLImageElement): Array<number> {
			ImageLoader.canvas.width = img.width;
			ImageLoader.canvas.height = img.height;

			ImageLoader.canvas.width = img.width;
			ImageLoader.canvas.height = img.height;
			ImageLoader.ctx.clearRect(0, 0, img.width, img.height);
			ImageLoader.ctx.drawImage(img, 0, 0);

			const imageData = ImageLoader.ctx.getImageData(
				0, 0,
				ImageLoader.canvas.width,
				ImageLoader.canvas.height
			);

			ImageLoader.canvas.width = 0;
			ImageLoader.canvas.height = 0;

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
			
			return Array.from<number>(imageData.data);
		}
	}
}
