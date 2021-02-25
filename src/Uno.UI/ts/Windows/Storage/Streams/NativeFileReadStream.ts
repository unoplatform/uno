namespace Uno.Storage.Streams {
	export class NativeFileReadStream {

		private static _streamMap: Map<string, NativeFileReadStream> = new Map<string, NativeFileReadStream>();

		private _file: File;

		private constructor(file: File) {
			this._file = file;
		}

		public static async openAsync(streamId: string, fileId: string): Promise<string> {
			const handle = <FileSystemFileHandle>NativeStorageItem.getHandle(fileId);
			const file = await handle.getFile();
			const fileSize = file.size;
			const stream = new NativeFileReadStream(file);
			NativeFileReadStream._streamMap.set(streamId, stream);
			return fileSize.toString();
		}

		public static async readAsync(streamId: string, targetArrayPointer: number, offset: number, count: number, position: number): Promise<string> {
			const instance = NativeFileReadStream._streamMap.get(streamId);

			//TODO: Reuse buffer somehow (slice?)
			var buffer = await instance._file.slice(position, position + count).arrayBuffer();
			var byteBuffer = new Uint8Array(buffer);
			for (var i = 0; i < count; i++) {
				Module.HEAPU8[targetArrayPointer + offset + i] = byteBuffer[i];
			}

			return byteBuffer.length.toString();
		}

		public static async closeAsync(streamId: string): Promise<string> {
			NativeFileReadStream._streamMap.delete(streamId);
			return "";
		}
	}
}
