
// eslint-disable-next-line @typescript-eslint/no-namespace
namespace Windows.Storage {

	export class AssetManager {
		public static async DownloadAssetsManifest(path: string): Promise<string> {
			const response = await fetch(path);
			return response.text();
		}

		public static async DownloadAsset(path: string): Promise<string> {
			const response = await fetch(path);
			const arrayBuffer = await response.blob().then(b => <ArrayBuffer>(<any>b).arrayBuffer());
			const size = arrayBuffer.byteLength;
			const responseArray = new Uint8ClampedArray(arrayBuffer);

			const pData = Module._malloc(size);

			Module.HEAPU8.set(responseArray, pData);

			return `${pData};${size}`;
		}
	}
}
