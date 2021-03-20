namespace Uno.Storage.Streams {
	export class NativeFileWriteStream {

		private static _streamMap: Map<string, NativeFileWriteStream> = new Map<string, NativeFileWriteStream>();

		private _stream: FileSystemWritableFileStream;
		private _buffer: Uint8Array;

		private constructor(stream: FileSystemWritableFileStream) {
			this._stream = stream;
		}

		public static async openAsync(streamId: string, fileId: string): Promise<string> {
			const handle = <FileSystemFileHandle>NativeStorageItem.getHandle(fileId);
			if (await NativeFileWriteStream.verifyPermissionAsync(handle)) {
				const writableStream = await handle.createWritable({ keepExistingData: true });
				const fileSize = (await handle.getFile()).size;
				const stream = new NativeFileWriteStream(writableStream);
				NativeFileWriteStream._streamMap.set(streamId, stream);
				return fileSize.toString();
			} else {
				return "PermissionNotGranted";
			}
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

			if (!instance._buffer || instance._buffer.length < count) {
				instance._buffer = new Uint8Array(count);
			}

			var clampedArray = new Uint8Array(count);
			for (var i = 0; i < count; i++) {
				clampedArray[i] = Module.HEAPU8[dataArrayPointer + i + offset];
			}

			await instance._stream.write({
				type: 'write',
				data: clampedArray.subarray(0, count).buffer,
				position: position
			})
			return "";
		}

		public static async closeAsync(streamId: string): Promise<string> {
			var instance = NativeFileWriteStream._streamMap.get(streamId);
			await instance._stream.close();
			NativeFileWriteStream._streamMap.delete(streamId);
			return "";
		}

		public static async truncateAsync(streamId: string, length: number): Promise<string> {
			var instance = NativeFileWriteStream._streamMap.get(streamId);
			await instance._stream.truncate(length);
			return "";
		}
	}
}
