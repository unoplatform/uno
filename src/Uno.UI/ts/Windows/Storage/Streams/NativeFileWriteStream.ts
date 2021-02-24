namespace Uno.Storage.Streams {
	export class NativeFileWriteStream {

		private static _streamMap: Map<string, NativeFileWriteStream> = new Map<string, NativeFileWriteStream>();

		private _stream: FileSystemWritableFileStream;

		private constructor(stream: FileSystemWritableFileStream) {
			this._stream = stream;
		}

		public static async openStreamAsync(streamId: string, fileId: string): Promise<string> {
			const handle = <FileSystemFileHandle>NativeStorageItem.getHandle(fileId);
			const writableStream = await handle.createWritable();
			const stream = new NativeFileWriteStream(writableStream);
			NativeFileWriteStream._streamMap.set(streamId, stream);
			return "";
		}

		public static async writeAsync(streamId: string, dataArrayPointer: number, position: number, length: number) {
			const instance = NativeFileWriteStream._streamMap.get(streamId);
			instance._stream.write({
				type: 'write',
				data: 0,
				position: position
			})
		}

		public static async closeStream(streamId: string): Promise<string> {
			var instance = NativeFileWriteStream._streamMap.get(streamId);
			await instance._stream.close();
			NativeFileWriteStream._streamMap.delete(streamId);
			return "";
		}
	}
}
