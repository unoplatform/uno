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
			var streamReader: ReadableStreamDefaultReader;
			var readerNeedsRelease = true;
			try {
				const instance = NativeFileReadStream._streamMap.get(streamId);

				var totalRead = 0;
				var stream = await instance._file.slice(position, position + count).stream();
				streamReader = stream.getReader();

				var chunk = await streamReader.read();
				while (!chunk.done && chunk.value) {
					for (var i = 0; i < chunk.value.length; i++) {
						Module.HEAPU8[targetArrayPointer + offset + totalRead + i] = chunk.value[i];
					}
					totalRead += chunk.value.length;

					chunk = await streamReader.read();
				}

				// If this is the end of stream, it closed itself
				readerNeedsRelease = !chunk.done;

				return totalRead.toString();
			}
			finally {
				// Reader must be released only if the underlying stream has not already closed it.				
				// Otherwise the release operation sets a new Promise.reject as reader.closed which
				// raises silent but observable exception in Chromium-based browsers.
				if (streamReader && readerNeedsRelease) {

					// Silently handling TypeError exceptions on closed event as the releaseLock()
					// raises one in case of a successful close.
					streamReader.closed.catch(reason => {
						if (!(reason instanceof TypeError)) {
							throw reason;
						}
					});

					streamReader.cancel();
					streamReader.releaseLock();
				}
			}
		}

		public static close(streamId: string) {
			NativeFileReadStream._streamMap.delete(streamId);
		}
	}
}
