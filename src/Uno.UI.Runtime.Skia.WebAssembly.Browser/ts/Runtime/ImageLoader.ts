namespace Uno.UI.Runtime.Skia {
	export class ImageLoader {
		public static async loadFromArray(array: Uint8Array): Promise<object> {
			return new Promise<object>((resolve) => {
				const image = new Blob([array]);
				const imageUrl = URL.createObjectURL(image);
				const canvas = document.createElement('canvas');
				const ctx = canvas.getContext('2d')

				const img = new Image();
				img.onload = () => {
					URL.revokeObjectURL(imageUrl);

					canvas.width = img.width;
					canvas.height = img.height;
					ctx.drawImage(img, 0, 0);

					const imageData = ctx.getImageData(
						0, 0,
						canvas.width,
						canvas.height
					);

					canvas.width = 0;
					canvas.height = 0;

					resolve({
						error: null,
						bytes: Array.from<number>(imageData.data),
						width: img.width,
						height: img.height
					}); // Return the RGBA buffer
				};

				img.onerror = e => resolve({
					error: e.toString()
				});

				img.src = imageUrl;
			});
		}
	}
}
