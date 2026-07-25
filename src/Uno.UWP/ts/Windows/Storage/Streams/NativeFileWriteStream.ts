namespace Uno.Storage.Streams {
	export class NativeFileWriteStream {

		private static _streamMap: Map<string, NativeFileWriteStream> = new Map<string, NativeFileWriteStream>();

		private _stream: FileSystemWritableFileStream;

		private constructor(stream: FileSystemWritableFileStream) {
			this._stream = stream;
		}

		public static async openAsync(streamId: string, fileId: string): Promise<string> {
			const item = NativeStorageItem.getItem(fileId);
			if (item instanceof File) {
				return "PermissionNotGranted";
			}

			const handle = <FileSystemFileHandle>item;
			if (!await NativeFileWriteStream.verifyPermissionAsync(handle)) {
				return "PermissionNotGranted";
			}

			const writableStream = await handle.createWritable({ keepExistingData: true });
			const fileSize = (await handle.getFile()).size;
			const stream = new NativeFileWriteStream(writableStream);
			NativeFileWriteStream._streamMap.set(streamId, stream);

			return fileSize.toString();
		}

		private static async verifyPermissionAsync(fileHandle: FileSystemFileHandle) {
			const options: FileSystemHandlePermissionDescriptor = {};
			options.mode = "readwrite";

			// Check if permission was already granted. If so, return true.
			if ((await fileHandle.queryPermission(options)) === 'granted') {
				return true;
			}
			// Request permission. If the user grants permission, return true.
			if ((await fileHandle.requestPermission(options)) === 'granted') {
				return true;
			}

			// The user didn't grant permission, so return false.
			return false;
		}

		public static async writeAsync(streamId: string, dataArrayPointer: number, offset: number, count: number, position: number): Promise<string> {
			const instance = NativeFileWriteStream._streamMap.get(streamId);

			// Copy out of the WASM heap in a single operation. The copy must not alias
			// Module.HEAPU8 - the heap may grow (and detach the buffer) during the await below.
			const data = Module.HEAPU8.slice(dataArrayPointer + offset, dataArrayPointer + offset + count);

			await instance._stream.write({
				type: 'write',
				data: data.buffer,
				position: position
			})
			return "";
		}

		public static async closeAsync(streamId: string): Promise<string> {
			var instance = NativeFileWriteStream._streamMap.get(streamId);
			if (instance)
			{
				await instance._stream.close();
				NativeFileWriteStream._streamMap.delete(streamId);
			}
			return "";
		}

		public static async truncateAsync(streamId: string, length: number): Promise<string> {
			var instance = NativeFileWriteStream._streamMap.get(streamId);
			await instance._stream.truncate(length);
			return "";
		}
	}
}
